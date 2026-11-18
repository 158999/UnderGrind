[CmdletBinding()]
param(
    [ValidateSet("EditMode", "PlayMode", "All")][string]$Platform = "All",
    [string]$UnityEditorPath = $env:UNITY_EDITOR_PATH
)

$ErrorActionPreference = "Stop"
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../.."))
$results = Join-Path $root "TestResults"
$runId = Get-Date -Format "yyyyMMdd-HHmmss-fff"
$runDir = Join-Path $results "Unity-$runId"
New-Item -ItemType Directory -Path $runDir -Force | Out-Null

if ([string]::IsNullOrWhiteSpace($UnityEditorPath)) {
    $candidates = @(
        "$env:ProgramFiles/Unity/Hub/Editor/6000.6.7f1/Editor/Unity.exe",
        "$env:LOCALAPPDATA/Programs/Unity/Hub/Editor/6000.6.7f1/Editor/Unity.exe"
    )
    $UnityEditorPath = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}
if ([string]::IsNullOrWhiteSpace($UnityEditorPath) -or -not (Test-Path $UnityEditorPath)) {
    Write-Host "Unity Editor not found. Set UNITY_EDITOR_PATH or pass -UnityEditorPath."
    exit 2
}

$testAssemblies = @(Get-ChildItem (Join-Path $root "Assets") -Filter "*.asmdef" -File -Recurse |
    Where-Object { $_.DirectoryName -match "Tests" })
if ($testAssemblies.Count -eq 0) {
    Write-Host "No Unity test assemblies found. No tests were executed."
    exit 2
}

$platforms = if ($Platform -eq "All") { @("EditMode", "PlayMode") } else { @($Platform) }
$items = [System.Collections.Generic.List[object]]::new()
$exit = 0
foreach ($target in $platforms) {
    $xmlName = "$target.xml"
    $logName = "$target.log"
    $xmlPath = Join-Path $runDir $xmlName
    $logPath = Join-Path $runDir $logName
    $arguments = @(
        "-batchmode", "-nographics",
        "-projectPath", $root,
        "-runTests",
        "-testPlatform", $target,
        "-testResults", $xmlPath,
        "-logFile", $logPath
    )
    Write-Host "Running Unity $target tests..."
    & $UnityEditorPath @arguments
    $code = $LASTEXITCODE
    $counts = @{ total = 0; passed = 0; failed = 0; skipped = 0 }
    $validationErrors = [System.Collections.Generic.List[string]]::new()
    $hasValidResult = $false

    if (Test-Path $xmlPath) {
        try {
            [xml]$testXml = Get-Content $xmlPath -Raw
            $suite = $testXml.SelectSingleNode("//*[local-name()='test-run']")
            if ($suite) {
                $counts.total = [int]$suite.total
                $counts.passed = [int]$suite.passed
                $counts.failed = [int]$suite.failed
                $counts.skipped = [int]$suite.skipped
                $hasValidResult = $true
                if ($counts.total -le 0) {
                    $validationErrors.Add("Unity produced a test result with zero executed or discovered tests.")
                }
            }
            else {
                $validationErrors.Add("The XML result does not contain a Unity test-run node.")
            }
        }
        catch {
            $validationErrors.Add("The XML result could not be parsed: $($_.Exception.Message)")
        }
    }
    else {
        $validationErrors.Add("Unity did not produce the expected XML result file.")
    }

    if (Test-Path $logPath) {
        $unityLog = Get-Content $logPath -Raw
        if ($unityLog -match "Scripts have compiler errors\.|error CS\d+|multiple assembly definition files") {
            $validationErrors.Add("Unity reported script compilation errors or invalid assembly-definition layout.")
        }
    }
    else {
        $validationErrors.Add("Unity did not produce the expected log file.")
    }

    $resultValid = $hasValidResult -and $counts.total -gt 0 -and $validationErrors.Count -eq 0
    $items.Add([ordered]@{
        platform = $target
        exitCode = $code
        resultValid = $resultValid
        validationErrors = @($validationErrors)
        resultFile = $xmlName
        logFile = $logName
        totals = $counts
    })
    if ($code -ne 0 -or $counts.failed -gt 0 -or -not $resultValid) { $exit = 1 }
}

$totals = @{
    total = ($items | ForEach-Object { [int]$_.totals.total } | Measure-Object -Sum).Sum
    passed = ($items | ForEach-Object { [int]$_.totals.passed } | Measure-Object -Sum).Sum
    failed = ($items | ForEach-Object { [int]$_.totals.failed } | Measure-Object -Sum).Sum
    skipped = ($items | ForEach-Object { [int]$_.totals.skipped } | Measure-Object -Sum).Sum
}
$report = [ordered]@{
    runId = $runId
    finishedUtc = [DateTime]::UtcNow.ToString("O")
    status = if ($exit -eq 0) { "Passed" } else { "Failed" }
    totals = $totals
    platforms = @($items)
}
$report | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $results "unity-latest.json") -Encoding utf8
@(
    "UnderGrind Unity tests"
    "Status: $($report.status)"
    "Total: $($totals.total) | Passed: $($totals.passed) | Failed: $($totals.failed) | Skipped: $($totals.skipped)"
    "Run details: TestResults/Unity-$runId/"
) | Set-Content (Join-Path $results "unity-latest.txt") -Encoding utf8
Write-Host "Unity status: $($report.status)"
exit $exit

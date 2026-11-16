[CmdletBinding()]
param([ValidateSet("Debug", "Release")][string]$Configuration = "Release")

$ErrorActionPreference = "Stop"
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../.."))
$results = Join-Path $root "TestResults"
$runId = Get-Date -Format "yyyyMMdd-HHmmss-fff"
$runDir = Join-Path $results $runId
New-Item -ItemType Directory -Path $runDir -Force | Out-Null

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error ".NET SDK not found."
    exit 2
}

$started = [DateTime]::UtcNow
$diagnosticsProject = Join-Path $root "Tools/Diagnostics/UnderGrind.Diagnostics.Standalone.csproj"
$buildOutput = & dotnet build $diagnosticsProject --configuration $Configuration 2>&1
$buildCode = $LASTEXITCODE
$buildOutput | Out-File (Join-Path $runDir "diagnostics-build.log") -Encoding utf8
if ($buildCode -ne 0) {
    $report = [ordered]@{
        runId = $runId
        finishedUtc = [DateTime]::UtcNow.ToString("O")
        status = "BuildFailed"
        diagnosticsBuildExitCode = $buildCode
        message = "The shared diagnostics source failed to build outside Unity."
        totals = @{ total = 0; passed = 0; failed = 0; skipped = 0 }
        projects = @()
    }
    $report | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $results "latest.json") -Encoding utf8
    "Standalone diagnostics build failed. See TestResults/$runId/diagnostics-build.log" | Set-Content (Join-Path $results "latest.txt") -Encoding utf8
    Write-Host "Diagnostics build failed. No tests were executed."
    exit 1
}

$projects = @(Get-ChildItem $root -Filter "*.Standalone.Tests.csproj" -File -Recurse |
    Where-Object { $_.FullName -notmatch "(bin|obj|Library|Temp)" } |
    Sort-Object FullName)

if ($projects.Count -eq 0) {
    $report = [ordered]@{
        runId = $runId
        startedUtc = $started.ToString("O")
        finishedUtc = [DateTime]::UtcNow.ToString("O")
        status = "NotConfigured"
        diagnosticsBuild = "Passed"
        diagnosticsBuildLog = "diagnostics-build.log"
        message = "Shared diagnostics compiled successfully. No standalone test projects were found; no tests were executed."
        totals = @{ total = 0; passed = 0; failed = 0; skipped = 0 }
        projects = @()
    }
    $report | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $results "latest.json") -Encoding utf8
    @("Diagnostics build: PASS", "Standalone tests: NOT CONFIGURED", "No tests were executed.") | Set-Content (Join-Path $results "latest.txt") -Encoding utf8
    Write-Host "Diagnostics build passed. No standalone test projects found; no tests were executed."
    exit 2
}

$items = [System.Collections.Generic.List[object]]::new()
$exit = 0
foreach ($project in $projects) {
    $name = $project.BaseName -replace '[^A-Za-z0-9_.-]', '_'
    $trxName = "$name.trx"
    $logName = "$name.log"
    $output = & dotnet test $project.FullName --configuration $Configuration --logger "trx;LogFileName=$trxName" --results-directory $runDir 2>&1
    $code = $LASTEXITCODE
    $output | Out-File (Join-Path $runDir $logName) -Encoding utf8
    $counts = @{ total = 0; passed = 0; failed = 0; skipped = 0 }
    $failures = @()
    $trxPath = Join-Path $runDir $trxName
    if (Test-Path $trxPath) {
        [xml]$trx = Get-Content $trxPath -Raw
        $counter = $trx.SelectSingleNode("//*[local-name()='ResultSummary']/*[local-name()='Counters']")
        if ($counter) {
            $counts.total = [int]$counter.total
            $counts.passed = [int]$counter.passed
            $counts.failed = [int]$counter.failed
            $counts.skipped = [int]$counter.notExecuted
        }
        $failures = @($trx.SelectNodes("//*[local-name()='UnitTestResult'][@outcome='Failed']") | ForEach-Object {
            $errorInfo = $_.SelectSingleNode(".//*[local-name()='ErrorInfo']")
            $messageNode = if ($errorInfo) { $errorInfo.SelectSingleNode("./*[local-name()='Message']") } else { $null }
            $stackNode = if ($errorInfo) { $errorInfo.SelectSingleNode("./*[local-name()='StackTrace']") } else { $null }
            [ordered]@{
                test = $_.testName
                message = if ($messageNode) { $messageNode.InnerText } else { "Test failed; inspect the TRX result." }
                stackTrace = if ($stackNode) { $stackNode.InnerText } else { "" }
            }
        })
    }
    $items.Add([ordered]@{
        project = $project.FullName.Substring($root.Length + 1)
        exitCode = $code
        trx = $trxName
        log = $logName
        totals = $counts
        failures = $failures
    })
    if ($code -ne 0 -or $counts.failed -gt 0) { $exit = 1 }
    Write-Host "[$code] $($project.Name): $($counts.passed) passed, $($counts.failed) failed"
}

$finished = [DateTime]::UtcNow
$totals = @{
    total = ($items | ForEach-Object { [int]$_.totals.total } | Measure-Object -Sum).Sum
    passed = ($items | ForEach-Object { [int]$_.totals.passed } | Measure-Object -Sum).Sum
    failed = ($items | ForEach-Object { [int]$_.totals.failed } | Measure-Object -Sum).Sum
    skipped = ($items | ForEach-Object { [int]$_.totals.skipped } | Measure-Object -Sum).Sum
}
$report = [ordered]@{
    runId = $runId
    startedUtc = $started.ToString("O")
    finishedUtc = $finished.ToString("O")
    durationSeconds = [Math]::Round(($finished - $started).TotalSeconds, 3)
    diagnosticsBuild = "Passed"
    status = if ($exit -eq 0) { "Passed" } else { "Failed" }
    totals = $totals
    projects = @($items)
}
$report | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $results "latest.json") -Encoding utf8
@(
    "UnderGrind standalone tests"
    "Diagnostics build: PASS"
    "Status: $($report.status)"
    "Total: $($totals.total) | Passed: $($totals.passed) | Failed: $($totals.failed) | Skipped: $($totals.skipped)"
    "Duration: $($report.durationSeconds)s"
    "Run details: TestResults/$runId/"
) | Set-Content (Join-Path $results "latest.txt") -Encoding utf8
Write-Host "Status: $($report.status)"
Write-Host "Report: TestResults/latest.json"
exit $exit

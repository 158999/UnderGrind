[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")][string]$Configuration = "Release",
    [ValidateSet("EditMode", "PlayMode", "All")][string]$Platform = "All",
    [string]$UnityEditorPath = $env:UNITY_EDITOR_PATH
)

$ErrorActionPreference = "Stop"
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../.."))
. (Join-Path $PSScriptRoot "TestResults.Retention.ps1")
$runDir = Initialize-UnderGrindTestRun -ProjectRoot $root
$standaloneScript = Join-Path $PSScriptRoot "run-tests.ps1"
$unityScript = Join-Path $PSScriptRoot "run-unity-tests.ps1"

function Invoke-TestScript {
    param([string]$ScriptPath, [string[]]$ScriptArguments)
    $arguments = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $ScriptPath) + $ScriptArguments
    $argumentLine = [string]::Join(" ", @($arguments | ForEach-Object {
        if ($_ -match '\s') { '"' + $_ + '"' } else { $_ }
    }))
    $process = Start-Process -FilePath "powershell.exe" -ArgumentList $argumentLine -WorkingDirectory $root -Wait -PassThru -NoNewWindow
    return $process.ExitCode
}

Write-Host "Test run: TestResults/test"
$standaloneExit = Invoke-TestScript -ScriptPath $standaloneScript -ScriptArguments @(
    "-Configuration", $Configuration, "-ReuseCurrentRun"
)

$unityArguments = @("-Platform", $Platform, "-ReuseCurrentRun")
if (-not [string]::IsNullOrWhiteSpace($UnityEditorPath)) {
    $unityArguments += @("-UnityEditorPath", $UnityEditorPath)
}
$unityExit = Invoke-TestScript -ScriptPath $unityScript -ScriptArguments $unityArguments

$standaloneReportPath = Join-Path $runDir "latest.json"
$unityReportPath = Join-Path $runDir "unity-latest.json"
$standaloneReport = if (Test-Path -LiteralPath $standaloneReportPath) {
    Get-Content -LiteralPath $standaloneReportPath -Raw | ConvertFrom-Json
} else { $null }
$unityReport = if (Test-Path -LiteralPath $unityReportPath) {
    Get-Content -LiteralPath $unityReportPath -Raw | ConvertFrom-Json
} else { $null }

$status = if ($standaloneExit -eq 0 -and $unityExit -eq 0 -and
    $standaloneReport.status -eq "Passed" -and $unityReport.status -eq "Passed") { "Passed" } else { "Failed" }
$combined = [ordered]@{
    runId = Get-Content -LiteralPath (Join-Path $runDir "run-id.txt") -Raw
    finishedUtc = [DateTime]::UtcNow.ToString("O")
    status = $status
    standaloneExitCode = $standaloneExit
    unityExitCode = $unityExit
    standalone = $standaloneReport
    unity = $unityReport
}
$combined | ConvertTo-Json -Depth 15 | Set-Content (Join-Path $runDir "all-latest.json") -Encoding utf8
@(
    "UnderGrind complete test run"
    "Status: $status"
    "Standalone: $($standaloneReport.status) | Exit: $standaloneExit"
    "Unity: $($unityReport.status) | Exit: $unityExit"
    "Reports: TestResults/test/"
    "Previous execution: TestResults/test old/"
) | Set-Content (Join-Path $runDir "all-latest.txt") -Encoding utf8

Write-Host "Complete test status: $status"
exit $(if ($status -eq "Passed") { 0 } else { 1 })

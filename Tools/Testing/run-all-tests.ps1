[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")][string]$Configuration = "Release",
    [ValidateSet("EditMode", "PlayMode", "All")][string]$Platform = "All",
    [string]$UnityEditorPath = $env:UNITY_EDITOR_PATH,
    [ValidateRange(30, 1800)][int]$BuildTimeoutSeconds = 180,
    [ValidateRange(30, 1800)][int]$TestTimeoutSeconds = 180,
    [ValidateRange(60, 3600)][int]$UnityTimeoutSeconds = 600,
    [ValidateRange(60, 3600)][int]$OverallTimeoutSeconds = 1800
)

$ErrorActionPreference = "Stop"
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../.."))
. (Join-Path $PSScriptRoot "TestResults.Retention.ps1")
$runDir = Initialize-UnderGrindTestRun -ProjectRoot $root
$standaloneScript = Join-Path $PSScriptRoot "run-tests.ps1"
$unityScript = Join-Path $PSScriptRoot "run-unity-tests.ps1"
$script:overallDeadline = [DateTime]::UtcNow.AddSeconds($OverallTimeoutSeconds)

function Invoke-TestScript {
    param(
        [Parameter(Mandatory = $true)][string]$StageName,
        [Parameter(Mandatory = $true)][string]$ScriptPath,
        [Parameter(Mandatory = $true)][string[]]$ScriptArguments
    )

    $remainingSeconds = [int][Math]::Ceiling(($script:overallDeadline - [DateTime]::UtcNow).TotalSeconds)
    if ($remainingSeconds -le 0) {
        Write-Warning "Timeout globale raggiunto prima di avviare la fase $StageName."
        return 124
    }

    Write-Host "Avvio fase $StageName (tempo residuo: $remainingSeconds secondi)..."
    $arguments = @("-NoLogo", "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", $ScriptPath) + $ScriptArguments
    $argumentLine = [string]::Join(" ", @($arguments | ForEach-Object {
        if ([string]$_ -match '\s') { '"' + [string]$_ + '"' } else { [string]$_ }
    }))

    $process = $null
    $exitCode = 1
    try {
        # Wait for the PowerShell worker process only. Do not use Start-Process -Wait:
        # it can wait for descendants (including Unity helper processes) after the
        # test runner has already printed its final status. Process.ExitCode is read
        # from the same process instance that was started by this runner.
        $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
        $startInfo.FileName = "powershell.exe"
        $startInfo.Arguments = $argumentLine
        $startInfo.WorkingDirectory = $root
        $startInfo.UseShellExecute = $false
        $startInfo.CreateNoWindow = $false

        $process = [System.Diagnostics.Process]::new()
        $process.StartInfo = $startInfo
        if (-not $process.Start()) {
            throw "Impossibile avviare il worker PowerShell per la fase $StageName."
        }

        if (-not $process.WaitForExit($remainingSeconds * 1000)) {
            Write-Warning "Timeout globale durante la fase $StageName. Termino il processo e i suoi discendenti."
            try { & taskkill.exe /PID $process.Id /T /F 2>$null | Out-Null } catch { }
            try {
                if (-not $process.HasExited) { $process.Kill() }
                [void]$process.WaitForExit(10000)
            } catch { }
            return 124
        }

        $process.WaitForExit()
        $exitCode = [int]$process.ExitCode
    }
    catch {
        Write-Error "Errore durante la fase $StageName - $($_.Exception.Message)"
        return 1
    }
    finally {
        if ($null -ne $process) { $process.Dispose() }
    }

    Write-Host "Fase $StageName terminata: exit code $exitCode."
    return $exitCode
}

Write-Host "Test run: TestResults/test"
$standaloneExit = Invoke-TestScript -StageName "standalone" -ScriptPath $standaloneScript -ScriptArguments @(
    "-Configuration", $Configuration,
    "-BuildTimeoutSeconds", [string]$BuildTimeoutSeconds,
    "-TestTimeoutSeconds", [string]$TestTimeoutSeconds,
    "-ReuseCurrentRun"
)

$unityExit = 124
if ([DateTime]::UtcNow -lt $script:overallDeadline) {
    $unityArguments = @("-Platform", $Platform, "-ReuseCurrentRun")
    if (-not [string]::IsNullOrWhiteSpace($UnityEditorPath)) {
        $unityArguments += @("-UnityEditorPath", $UnityEditorPath)
    }
    $unityArguments += @("-TimeoutSeconds", [string]$UnityTimeoutSeconds)
    $unityExit = Invoke-TestScript -StageName "unity" -ScriptPath $unityScript -ScriptArguments $unityArguments
}
else {
    Write-Warning "Timeout globale raggiunto: fase Unity non avviata."
}

Write-Host "Raccolta dei report delle due fasi..."
$standaloneReportPath = Join-Path $runDir "latest.json"
$unityReportPath = Join-Path $runDir "unity-latest.json"
$standaloneSummaryPath = Join-Path $runDir "latest.txt"
$unitySummaryPath = Join-Path $runDir "unity-latest.txt"
$runIdPath = Join-Path $runDir "run-id.txt"

Write-Host "Lettura stato standalone..."
$standaloneSummary = if (Test-Path -LiteralPath $standaloneSummaryPath -PathType Leaf) {
    [IO.File]::ReadAllText($standaloneSummaryPath)
} else { "" }
$standaloneStatus = if ($standaloneSummary -match "(?m)^Status:\s*(Passed|Failed|NotConfigured)\s*$") {
    $Matches[1]
} else { "Missing" }

Write-Host "Lettura stato Unity..."
$unitySummary = if (Test-Path -LiteralPath $unitySummaryPath -PathType Leaf) {
    [IO.File]::ReadAllText($unitySummaryPath)
} else { "" }
$unityStatus = if ($unitySummary -match "(?m)^Status:\s*(Passed|Failed|NotConfigured)\s*$") {
    $Matches[1]
} else { "Missing" }

Write-Host "Lettura report JSON originali..."
$standaloneJson = if (Test-Path -LiteralPath $standaloneReportPath -PathType Leaf) {
    [IO.File]::ReadAllText($standaloneReportPath).Trim()
} else { "null" }
$unityJson = if (Test-Path -LiteralPath $unityReportPath -PathType Leaf) {
    [IO.File]::ReadAllText($unityReportPath).Trim()
} else { "null" }
$runId = if (Test-Path -LiteralPath $runIdPath -PathType Leaf) {
    [IO.File]::ReadAllText($runIdPath).Trim()
} else { [DateTime]::UtcNow.ToString("yyyyMMdd-HHmmss-fff") }

$status = if ($standaloneExit -eq 0 -and $unityExit -eq 0 -and
    $standaloneStatus -eq "Passed" -and $unityStatus -eq "Passed" -and
    $standaloneJson -ne "null" -and $unityJson -ne "null") { "Passed" } else { "Failed" }

Write-Host "Composizione report complessivo..."
$finishedUtc = [DateTime]::UtcNow.ToString("O")
$combinedJson = @"
{
  "runId": "$runId",
  "finishedUtc": "$finishedUtc",
  "timeoutSeconds": $OverallTimeoutSeconds,
  "status": "$status",
  "standaloneExitCode": $standaloneExit,
  "unityExitCode": $unityExit,
  "standalone": $standaloneJson,
  "unity": $unityJson
}
"@
[IO.File]::WriteAllText(
    (Join-Path $runDir "all-latest.json"),
    $combinedJson,
    [Text.UTF8Encoding]::new($false))

@(
    "UnderGrind complete test run"
    "Status: $status"
    "Standalone: $standaloneStatus | Exit: $standaloneExit"
    "Unity: $unityStatus | Exit: $unityExit"
    "Standalone logs: TestResults/test/*.log"
    "Unity logs and XML: TestResults/test/Unity/"
    "Reports: TestResults/test/"
    "Previous execution: TestResults/test old/"
) | Set-Content (Join-Path $runDir "all-latest.txt") -Encoding utf8

Write-Host "Complete test status: $status"
Write-Host "Report: TestResults/test/all-latest.json"
exit $(if ($status -eq "Passed") { 0 } else { 1 })

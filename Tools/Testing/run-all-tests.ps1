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
        # test runner has already printed its final status.
        $process = Start-Process -FilePath "powershell.exe" -ArgumentList $argumentLine -WorkingDirectory $root -PassThru -NoNewWindow
        if (-not $process.WaitForExit($remainingSeconds * 1000)) {
            Write-Warning "Timeout globale durante la fase $StageName. Termino il processo e i suoi discendenti."
            try { & taskkill.exe /PID $process.Id /T /F 2>$null | Out-Null } catch { }
            try {
                if (-not $process.HasExited) { $process.Kill() }
                [void]$process.WaitForExit(10000)
            } catch { }
            return 124
        }

        $process.Refresh()
        if ($process.HasExited) {
            $exitCode = [int]$process.ExitCode
        }
        else {
            Write-Warning "Il processo della fase $StageName non ha confermato la terminazione."
            return 124
        }
    }
    catch {
        Write-Error "Errore durante la fase $StageName - $($_.Exception.Message)"
        return 1
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
$standaloneReport = if (Test-Path -LiteralPath $standaloneReportPath) {
    Get-Content -LiteralPath $standaloneReportPath -Raw | ConvertFrom-Json
} else { $null }
$unityReport = if (Test-Path -LiteralPath $unityReportPath) {
    Get-Content -LiteralPath $unityReportPath -Raw | ConvertFrom-Json
} else { $null }

$status = if ($standaloneExit -eq 0 -and $unityExit -eq 0 -and $standaloneReport.status -eq "Passed" -and $unityReport.status -eq "Passed") { "Passed" } else { "Failed" }
$combined = [ordered]@{
    runId = Get-Content -LiteralPath (Join-Path $runDir "run-id.txt") -Raw
    finishedUtc = [DateTime]::UtcNow.ToString("O")
    timeoutSeconds = $OverallTimeoutSeconds
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
    "Standalone logs: TestResults/test/*.log"
    "Unity logs and XML: TestResults/test/Unity/"
    "Reports: TestResults/test/"
    "Previous execution: TestResults/test old/"
) | Set-Content (Join-Path $runDir "all-latest.txt") -Encoding utf8

Write-Host "Complete test status: $status"
Write-Host "Report: TestResults/test/all-latest.json"
exit $(if ($status -eq "Passed") { 0 } else { 1 })

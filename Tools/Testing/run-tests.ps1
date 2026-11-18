[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")][string]$Configuration = "Release",
    [ValidateRange(30, 1800)][int]$BuildTimeoutSeconds = 180,
    [ValidateRange(30, 1800)][int]$TestTimeoutSeconds = 180,
    [switch]$ReuseCurrentRun
)

$ErrorActionPreference = "Stop"
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../.."))
. (Join-Path $PSScriptRoot "TestResults.Retention.ps1")
$runDir = Initialize-UnderGrindTestRun -ProjectRoot $root -ReuseCurrentRun:$ReuseCurrentRun
$runId = Get-Date -Format "yyyyMMdd-HHmmss-fff"
$started = [DateTime]::UtcNow
$diagnosticsProject = Join-Path $root "Tools/Diagnostics/UnderGrind.Diagnostics.Standalone.csproj"
$items = [System.Collections.Generic.List[object]]::new()
$exit = 0
$errors = [System.Collections.Generic.List[string]]::new()
$diagnosticsBuild = "NotRun"

function Invoke-DotNetCaptured {
    param(
        [Parameter(Mandatory = $true)][string[]]$DotNetArguments,
        [Parameter(Mandatory = $true)][string]$OutputPath,
        [Parameter(Mandatory = $true)][int]$TimeoutSeconds
    )

    $dotnetCommand = Get-Command dotnet -ErrorAction Stop
    $stdoutPath = "$OutputPath.stdout.tmp"
    $stderrPath = "$OutputPath.stderr.tmp"
    $quotedArguments = @($DotNetArguments | ForEach-Object {
        '"' + [string]$_ + '"'
    })
    $argumentLine = [string]::Join(" ", $quotedArguments)
    $timedOut = $false
    $exitCode = 1
    $process = $null
    try {
        $process = Start-Process -FilePath $dotnetCommand.Source -ArgumentList $argumentLine -WorkingDirectory $root -PassThru -NoNewWindow -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
        if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
            $timedOut = $true
            try { & taskkill.exe /PID $process.Id /T /F 2>$null | Out-Null } catch { }
            try {
                if (-not $process.HasExited) { $process.Kill() }
                $process.WaitForExit()
            } catch { }
            $exitCode = 124
        }
        else {
            $exitCode = [int]$process.ExitCode
        }
    }
    catch {
        $stderrFailure = $_.Exception.Message
        $timedOut = $false
        $exitCode = 1
    }

    $stdout = if (Test-Path -LiteralPath $stdoutPath) { Get-Content -LiteralPath $stdoutPath -Raw } else { "" }
    $stderr = if (Test-Path -LiteralPath $stderrPath) { Get-Content -LiteralPath $stderrPath -Raw } else { "" }
    if ($stderrFailure) { $stderr += [Environment]::NewLine + $stderrFailure }
    if ($timedOut) { $stderr += [Environment]::NewLine + "TIMEOUT after $TimeoutSeconds seconds." }
    @("=== STDOUT ===", $stdout, "=== STDERR ===", $stderr) | Set-Content -LiteralPath $OutputPath -Encoding utf8
    Remove-Item -LiteralPath $stdoutPath, $stderrPath -Force -ErrorAction SilentlyContinue
    return [pscustomobject]@{
        ExitCode = $exitCode
        TimedOut = $timedOut
        Output = $stdout + [Environment]::NewLine + $stderr
    }
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    $errors.Add(".NET SDK non trovato.")
    $exit = 2
}
else {
    $buildResult = Invoke-DotNetCaptured -DotNetArguments @("build", $diagnosticsProject, "--configuration", $Configuration) -OutputPath (Join-Path $runDir "diagnostics-build.log") -TimeoutSeconds $BuildTimeoutSeconds
    $buildOutput = $buildResult.Output
    $buildCode = $buildResult.ExitCode
    $diagnosticsBuild = if ($buildCode -eq 0) { "Passed" } else { "Failed" }

    if ($buildCode -ne 0) {
        if ($buildResult.TimedOut) { $errors.Add("La compilazione diagnostica ha superato il timeout di $BuildTimeoutSeconds secondi.") }
        $errors.Add("La compilazione del progetto diagnostico condiviso è fallita.")
        $exit = 1
    }
    else {
        $projects = @(Get-ChildItem $root -Filter "*.Standalone.Tests.csproj" -File -Recurse |
            Where-Object { $_.FullName -notmatch "(bin|obj|Library|Temp)" } |
            Sort-Object FullName)

        if ($projects.Count -eq 0) {
            $errors.Add("Nessun progetto di test standalone trovato: nessun test è stato eseguito.")
            $exit = 2
        }
        else {
            foreach ($project in $projects) {
                $name = $project.BaseName -replace '[^A-Za-z0-9_.-]', '_'
                $trxName = "$name.trx"
                $logName = "$name.log"
                $testResult = Invoke-DotNetCaptured -DotNetArguments @("test", $project.FullName, "--configuration", $Configuration, "--logger", "trx;LogFileName=$trxName", "--results-directory", $runDir) -OutputPath (Join-Path $runDir $logName) -TimeoutSeconds $TestTimeoutSeconds
                $output = $testResult.Output
                $code = $testResult.ExitCode

                $counts = @{ total = 0; passed = 0; failed = 0; skipped = 0 }
                $failures = @()
                $validationErrors = [System.Collections.Generic.List[string]]::new()
                $trxPath = Join-Path $runDir $trxName

                if (Test-Path -LiteralPath $trxPath) {
                    try {
                        [xml]$trx = Get-Content -LiteralPath $trxPath -Raw
                        $counter = $trx.SelectSingleNode("//*[local-name()='ResultSummary']/*[local-name()='Counters']")
                        if (-not $counter) {
                            $validationErrors.Add("Il TRX non contiene ResultSummary/Counters.")
                        }
                        else {
                            $counts.total = [int]$counter.total
                            $counts.passed = [int]$counter.passed
                            $counts.failed = [int]$counter.failed
                            $counts.skipped = [int]$counter.notExecuted
                            if ($counts.total -le 0) {
                                $validationErrors.Add("Il TRX è valido ma non contiene test eseguiti o scoperti.")
                            }
                        }

                        $failures = @($trx.SelectNodes("//*[local-name()='UnitTestResult'][@outcome='Failed']") | ForEach-Object {
                            $errorInfo = $_.SelectSingleNode(".//*[local-name()='ErrorInfo']")
                            $messageNode = if ($errorInfo) { $errorInfo.SelectSingleNode("./*[local-name()='Message']") } else { $null }
                            $stackNode = if ($errorInfo) { $errorInfo.SelectSingleNode("./*[local-name()='StackTrace']") } else { $null }
                            [ordered]@{
                                test = $_.testName
                                message = if ($messageNode) { $messageNode.InnerText } else { "Test fallito; consultare il TRX." }
                                stackTrace = if ($stackNode) { $stackNode.InnerText } else { "" }
                            }
                        })
                    }
                    catch {
                        $validationErrors.Add("Il TRX non è leggibile: $($_.Exception.Message)")
                    }
                }
                else {
                    $validationErrors.Add("dotnet test non ha prodotto il file TRX atteso.")
                }

                if ($testResult.TimedOut) { $validationErrors.Add("dotnet test ha superato il timeout di $TestTimeoutSeconds secondi.") }
                if ($code -ne 0) { $validationErrors.Add("dotnet test è terminato con codice $code.") }
                if ($counts.failed -gt 0) { $validationErrors.Add("$($counts.failed) test falliti.") }
                if ($validationErrors.Count -gt 0) { $exit = 1 }

                $items.Add([ordered]@{
                    project = $project.FullName.Substring($root.Length + 1)
                    exitCode = $code
                    trx = $trxName
                    log = $logName
                    totals = $counts
                    failures = $failures
                    validationErrors = @($validationErrors)
                })
                Write-Host "[$code] $($project.Name): $($counts.passed) superati, $($counts.failed) falliti, $($counts.total) totali"
            }
        }
    }
}

$finished = [DateTime]::UtcNow
$totals = @{ total = 0; passed = 0; failed = 0; skipped = 0 }
foreach ($item in $items) {
    $totals.total += [int]$item.totals.total
    $totals.passed += [int]$item.totals.passed
    $totals.failed += [int]$item.totals.failed
    $totals.skipped += [int]$item.totals.skipped
}
$status = if ($exit -eq 0) { "Passed" } elseif ($exit -eq 2) { "NotConfigured" } else { "Failed" }
$report = [ordered]@{
    runId = $runId
    startedUtc = $started.ToString("O")
    finishedUtc = $finished.ToString("O")
    durationSeconds = [Math]::Round(($finished - $started).TotalSeconds, 3)
    diagnosticsBuild = $diagnosticsBuild
    buildTimeoutSeconds = $BuildTimeoutSeconds
    testTimeoutSeconds = $TestTimeoutSeconds
    status = $status
    errors = @($errors)
    totals = $totals
    projects = @($items)
}
$report | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $runDir "latest.json") -Encoding utf8
@(
    "UnderGrind standalone tests"
    "Diagnostics build: $diagnosticsBuild"
    "Status: $status"
    "Total: $($totals.total) | Passed: $($totals.passed) | Failed: $($totals.failed) | Skipped: $($totals.skipped)"
    "Duration: $($report.durationSeconds)s"
    "Errors: $($errors -join ' | ')"
    "Run details: TestResults/test/"
) | Set-Content (Join-Path $runDir "latest.txt") -Encoding utf8

Write-Host "Status: $status"
Write-Host "Report: TestResults/test/latest.json"
exit $exit

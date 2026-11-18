[CmdletBinding()]
param(
    [ValidateSet("EditMode", "PlayMode", "All")][string]$Platform = "All",
    [string]$UnityEditorPath = $env:UNITY_EDITOR_PATH,
    [switch]$ReuseCurrentRun
)

$ErrorActionPreference = "Stop"
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../.."))
$results = Join-Path $root "TestResults"
. (Join-Path $PSScriptRoot "TestResults.Retention.ps1")
$runDir = Initialize-UnderGrindTestRun -ProjectRoot $root -ReuseCurrentRun:$ReuseCurrentRun
$callbackResults = Join-Path $runDir "UnderGrind"
$unityOutput = Join-Path $runDir "Unity"
New-Item -ItemType Directory -Path $unityOutput -Force | Out-Null
$runId = Get-Date -Format "yyyyMMdd-HHmmss-fff"
$runStartedUtc = [DateTime]::UtcNow
$items = [System.Collections.Generic.List[object]]::new()
$exit = 0
$environmentErrors = [System.Collections.Generic.List[string]]::new()

if ([string]::IsNullOrWhiteSpace($UnityEditorPath)) {
    $projectVersionFile = Join-Path $root "ProjectSettings/ProjectVersion.txt"
    $projectVersion = if (Test-Path -LiteralPath $projectVersionFile) {
        (Select-String -LiteralPath $projectVersionFile -Pattern "^m_EditorVersion:\s*(.+)$").Matches.Groups[1].Value.Trim()
    } else { "" }

    $editorRoots = @(
        (Join-Path $env:ProgramFiles "Unity/Hub/Editor"),
        (Join-Path $env:LOCALAPPDATA "Programs/Unity/Hub/Editor"),
        (Join-Path $env:ProgramFiles "Unity")
    ) | Where-Object { $_ -and (Test-Path -LiteralPath $_ -PathType Container) }

    $candidates = [System.Collections.Generic.List[string]]::new()
    foreach ($editorRoot in $editorRoots) {
        if ($projectVersion) {
            $versionCandidate = Join-Path $editorRoot "$projectVersion/Editor/Unity.exe"
            if (Test-Path -LiteralPath $versionCandidate) { $candidates.Add($versionCandidate) }
        }
        Get-ChildItem -LiteralPath $editorRoot -Filter "Unity.exe" -File -Recurse -ErrorAction SilentlyContinue |
            ForEach-Object { $candidates.Add($_.FullName) }
    }
    $UnityEditorPath = $candidates | Select-Object -Unique | Select-Object -First 1
}
if ([string]::IsNullOrWhiteSpace($UnityEditorPath) -or -not (Test-Path -LiteralPath $UnityEditorPath)) {
    $environmentErrors.Add("Unity Editor non trovato nelle installazioni Unity Hub standard. Impostare UNITY_EDITOR_PATH o passare -UnityEditorPath con il percorso completo a Unity.exe. Versione richiesta dal progetto: $projectVersion.")
    $exit = 2
}
else {
    $testAssemblies = @(Get-ChildItem (Join-Path $root "Assets") -Filter "*.asmdef" -File -Recurse |
        Where-Object { $_.DirectoryName -match "Tests" })
    if ($testAssemblies.Count -eq 0) {
        $environmentErrors.Add("Nessuna assembly Unity di test trovata sotto Assets.")
        $exit = 2
    }
    else {
        $platforms = if ($Platform -eq "All") { @("EditMode", "PlayMode") } else { @($Platform) }
        foreach ($target in $platforms) {
            $platformStartedUtc = [DateTime]::UtcNow
            $callbackPath = Join-Path $callbackResults ($target + ".xml")
            $callbackBaseline = if (Test-Path -LiteralPath $callbackPath) {
                (Get-Item -LiteralPath $callbackPath).LastWriteTimeUtc
            } else { [DateTime]::MinValue }

            $xmlName = "$target.xml"
            $logName = "$target.log"
            $xmlPath = Join-Path $unityOutput $xmlName
            $logPath = Join-Path $unityOutput $logName
            $arguments = @(
                "-batchmode", "-nographics", "-accept-apiupdate",
                "-projectPath", $root,
                "-runTests",
                "-testPlatform", $target,
                "-testResults", $xmlPath,
                "-logFile", $logPath
            )
            Write-Host "Running Unity $target tests..."
            $argumentLine = [string]::Join(" ", @($arguments | ForEach-Object {
                if ($_ -match '\s') { '"' + $_ + '"' } else { $_ }
            }))
            $counts = @{ total = 0; passed = 0; failed = 0; skipped = 0; inconclusive = 0 }
            $validationErrors = [System.Collections.Generic.List[string]]::new()
            $compilerDiagnostics = @()
            $hasValidResult = $false
            $code = 1

            try {
                $process = Start-Process -FilePath $UnityEditorPath -ArgumentList $argumentLine -WorkingDirectory $root -Wait -PassThru -NoNewWindow
                $code = $process.ExitCode
            }
            catch {
                $validationErrors.Add("Impossibile avviare o attendere Unity: $($_.Exception.Message)")
            }
            if ($code -ne 0) { $validationErrors.Add("Unity è terminato con codice $code.") }

            $actualResultPath = $xmlPath
            if (-not (Test-Path -LiteralPath $actualResultPath) -and (Test-Path -LiteralPath $callbackPath)) {
                $callbackInfo = Get-Item -LiteralPath $callbackPath
                if ($callbackInfo.LastWriteTimeUtc -gt $callbackBaseline -and
                    $callbackInfo.LastWriteTimeUtc -ge $platformStartedUtc) {
                    $actualResultPath = $callbackPath
                    Write-Host "Usato report TestRunner API aggiornato: TestResults/test/UnderGrind/$target.xml"
                }
            }

            if (Test-Path -LiteralPath $actualResultPath) {
                try {
                    [xml]$testXml = Get-Content -LiteralPath $actualResultPath -Raw
                    $suite = $testXml.DocumentElement
                    if ($suite -and $suite.LocalName -notin @("test-run", "test-suite")) {
                        $suite = $testXml.SelectSingleNode("//*[local-name()='test-run' or local-name()='test-suite'][@name='UnderGrind']")
                    }
                    if ($suite -and $suite.LocalName -in @("test-run", "test-suite")) {
                        $counts.total = [int]$suite.total
                        $counts.passed = [int]$suite.passed
                        $counts.failed = [int]$suite.failed
                        $counts.skipped = [int]$suite.skipped
                        $counts.inconclusive = [int]$suite.inconclusive
                        $hasValidResult = $true
                        if ($counts.total -le 0) { $validationErrors.Add("Il report Unity non contiene test eseguiti o scoperti.") }
                        if ($suite.result -ne "Passed") { $validationErrors.Add("Il risultato Unity è '$($suite.result)', non Passed.") }
                        if ($counts.failed -gt 0) { $validationErrors.Add("Unity ha riportato $($counts.failed) test falliti.") }
                        if ($counts.inconclusive -gt 0) { $validationErrors.Add("Unity ha riportato $($counts.inconclusive) test inconcludenti.") }
                    }
                    else {
                        $validationErrors.Add("La radice XML non è un nodo test-run o test-suite.")
                    }
                }
                catch {
                    $validationErrors.Add("Il risultato XML non è leggibile: $($_.Exception.Message)")
                }
            }
            else {
                $validationErrors.Add("Unity non ha prodotto un XML nuovo e valido per questa piattaforma.")
            }

            if (Test-Path -LiteralPath $logPath) {
                $unityLog = Get-Content -LiteralPath $logPath -Raw
                $compilerDiagnostics = @($unityLog -split "`r?`n" | Where-Object {
                    $_ -match "error CS\d+|Scripts have compiler errors\.|multiple assembly definition files"
                })
                foreach ($diagnostic in $compilerDiagnostics) {
                    $validationErrors.Add("Compilazione: $diagnostic")
                }
            }
            else {
                $validationErrors.Add("Unity non ha prodotto il log atteso.")
            }

            $resultValid = $hasValidResult -and $counts.total -gt 0 -and $validationErrors.Count -eq 0
            $resultFile = if (Test-Path -LiteralPath $actualResultPath) {
                $actualResultPath.Substring($root.Length + 1).Replace("\", "/")
            } else { "TestResults/test/Unity/$xmlName" }
            $items.Add([ordered]@{
                platform = $target
                exitCode = $code
                resultValid = $resultValid
                validationErrors = @($validationErrors)
                compilerDiagnostics = @($compilerDiagnostics)
                resultFile = $resultFile
                logFile = "TestResults/test/Unity/$logName"
                totals = $counts
            })
            if ($code -ne 0 -or -not $resultValid) { $exit = 1 }
            Write-Host "Unity $target : $($counts.passed) superati, $($counts.failed) falliti, $($counts.total) totali"
        }
    }
}

$totals = @{ total = 0; passed = 0; failed = 0; skipped = 0; inconclusive = 0 }
foreach ($item in $items) {
    $totals.total += [int]$item.totals.total
    $totals.passed += [int]$item.totals.passed
    $totals.failed += [int]$item.totals.failed
    $totals.skipped += [int]$item.totals.skipped
    $totals.inconclusive += [int]$item.totals.inconclusive
}
$status = if ($exit -eq 0) { "Passed" } elseif ($exit -eq 2) { "NotConfigured" } else { "Failed" }
$report = [ordered]@{
    runId = $runId
    startedUtc = $runStartedUtc.ToString("O")
    finishedUtc = [DateTime]::UtcNow.ToString("O")
    status = $status
    environmentErrors = @($environmentErrors)
    totals = $totals
    platforms = @($items)
}
$report | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $runDir "unity-latest.json") -Encoding utf8
@(
    "UnderGrind Unity tests"
    "Status: $status"
    "Total: $($totals.total) | Passed: $($totals.passed) | Failed: $($totals.failed) | Skipped: $($totals.skipped) | Inconclusive: $($totals.inconclusive)"
    "Environment errors: $($environmentErrors -join ' | ')"
    "Run details: TestResults/test/Unity/"
) | Set-Content (Join-Path $runDir "unity-latest.txt") -Encoding utf8
Write-Host "Unity status: $status"
exit $exit

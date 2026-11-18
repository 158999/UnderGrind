function Initialize-UnderGrindTestRun {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [switch]$ReuseCurrentRun
    )

    $resultsRoot = Join-Path $ProjectRoot "TestResults"
    $currentRun = Join-Path $resultsRoot "test"
    $previousRun = Join-Path $resultsRoot "test old"

    if (-not (Test-Path -LiteralPath $resultsRoot)) {
        New-Item -ItemType Directory -Path $resultsRoot -Force | Out-Null
    }

    if ($ReuseCurrentRun) {
        if (-not (Test-Path -LiteralPath $currentRun -PathType Container)) {
            throw "La cartella TestResults/test non esiste. Avvia prima una nuova esecuzione senza -ReuseCurrentRun."
        }
        return $currentRun
    }

    if (Test-Path -LiteralPath $previousRun) {
        Remove-Item -LiteralPath $previousRun -Recurse -Force
    }

    if (Test-Path -LiteralPath $currentRun -PathType Container) {
        Move-Item -LiteralPath $currentRun -Destination $previousRun
    }
    else {
        New-Item -ItemType Directory -Path $previousRun -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $previousRun ".gitkeep") -Value "" -Encoding utf8
    }

    # Remove timestamped legacy runs and root-level reports. Only the two
    # retained run directories are allowed at the TestResults root.
    Get-ChildItem -LiteralPath $resultsRoot -Force |
        Where-Object { $_.Name -notin @("test", "test old") } |
        ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force }

    New-Item -ItemType Directory -Path $currentRun -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $currentRun ".gitkeep") -Value "" -Encoding utf8
    $runId = Get-Date -Format "yyyyMMdd-HHmmss-fff"
    Set-Content -LiteralPath (Join-Path $currentRun "run-id.txt") -Value $runId -Encoding utf8
    return $currentRun
}
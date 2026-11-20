$ErrorActionPreference = "Stop"

$command = "Write-Output 'stdout-marker'; [Console]::Error.WriteLine('stderr-marker'); exit 23"
$startInfo = [System.Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = "powershell.exe"
$startInfo.Arguments = "-NoProfile -NonInteractive -Command `"$command`""
$startInfo.UseShellExecute = $false
$startInfo.CreateNoWindow = $true
$startInfo.RedirectStandardOutput = $true
$startInfo.RedirectStandardError = $true

$process = [System.Diagnostics.Process]::new()
try {
    $process.StartInfo = $startInfo
    if (-not $process.Start()) {
        throw "Impossibile avviare il processo di prova."
    }

    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit(10000)) {
        try { & taskkill.exe /PID $process.Id /T /F 2>$null | Out-Null } catch { }
        throw "Il processo di prova non è terminato entro 10 secondi."
    }

    $process.WaitForExit()
    $stdout = $stdoutTask.GetAwaiter().GetResult()
    $stderr = $stderrTask.GetAwaiter().GetResult()
    $exitCode = [int]$process.ExitCode

    if ($exitCode -ne 23) {
        throw "Codice di uscita inatteso: $exitCode (atteso 23)."
    }
    if ($stdout -notmatch "stdout-marker" -or $stderr -notmatch "stderr-marker") {
        throw "Gli stream stdout/stderr non sono stati acquisiti correttamente."
    }

    Write-Host "Process runner smoke test: PASS (exit code 23, stdout e stderr acquisiti)."
}
finally {
    $process.Dispose()
}

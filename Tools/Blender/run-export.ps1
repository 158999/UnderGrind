[CmdletBinding()]
param(
    [string]$ProjectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../..")),
    [string]$BlenderExecutable = $env:BLENDER_EXECUTABLE,
    [ValidateRange(30, 1800)][int]$TimeoutSeconds = 300,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

function Resolve-ConfiguredAssetPath {
    param(
        [Parameter(Mandatory = $true)][string]$FieldName,
        [Parameter(Mandatory = $true)]$Defaults,
        [Parameter(Mandatory = $true)][string]$AssetPath,
        [Parameter(Mandatory = $true)][string]$Root
    )

    $value = $null
    if (Test-Path -LiteralPath $AssetPath -PathType Leaf) {
        $content = Get-Content -LiteralPath $AssetPath -Raw
        $match = [regex]::Match($content, "(?m)^\s*" + [regex]::Escape($FieldName) + ":\s*(.*?)\s*$")
        if ($match.Success) {
            $value = $match.Groups[1].Value.Trim().Trim('"')
        }
    }

    if ([string]::IsNullOrWhiteSpace($value)) {
        $property = $Defaults.PSObject.Properties[$FieldName]
        if ($null -ne $property) {
            $value = [string]$property.Value
        }
    }

    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "Percorso non configurato: $FieldName."
    }

    $value = $value.Replace("/", [IO.Path]::DirectorySeparatorChar).Replace("\", [IO.Path]::DirectorySeparatorChar)
    if ([IO.Path]::IsPathRooted($value)) {
        return [IO.Path]::GetFullPath($value)
    }

    return [IO.Path]::GetFullPath((Join-Path $Root $value))
}

function ConvertTo-ProcessArgument {
    param([Parameter(Mandatory = $true)][string]$Value)
    return '"' + $Value.Replace('"', '\"') + '"'
}

function Stop-ProcessTree {
    param([Parameter(Mandatory = $true)][System.Diagnostics.Process]$Process)

    if ($Process.HasExited) { return }

    if ($env:OS -eq "Windows_NT") {
        & taskkill.exe /PID $Process.Id /T /F 2>$null | Out-Null
    }
    else {
        try { $Process.Kill() } catch { }
    }
}

$ProjectRoot = [IO.Path]::GetFullPath($ProjectRoot)
$blendPath = Join-Path $ProjectRoot "Assets/UnderGrind/Art/Source/humans.blend"
$exportScriptPath = Join-Path $ProjectRoot "Tools/Blender/export-undergrind-humans.py"
$defaultsPath = Join-Path $ProjectRoot "Assets/UnderGrind/Settings/Defaults/PlayerRigSettings.json"
$settingsAssetPath = Join-Path $ProjectRoot "Assets/UnderGrind/Settings/PlayerRigSettings.asset"
$reportPath = Join-Path $ProjectRoot "Tools/Blender/last-export-report.json"

foreach ($requiredPath in @($blendPath, $exportScriptPath, $defaultsPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "File richiesto non trovato: $requiredPath"
    }
}

if ([string]::IsNullOrWhiteSpace($BlenderExecutable)) {
    $blenderCommand = Get-Command "blender" -ErrorAction SilentlyContinue
    if ($null -eq $blenderCommand) {
        throw "Blender non trovato. Indicare -BlenderExecutable o impostare BLENDER_EXECUTABLE."
    }
    $BlenderExecutable = $blenderCommand.Source
}

if (-not (Test-Path -LiteralPath $BlenderExecutable -PathType Leaf)) {
    $resolvedCommand = Get-Command $BlenderExecutable -ErrorAction SilentlyContinue
    if ($null -eq $resolvedCommand) {
        throw "Eseguibile Blender non trovato: $BlenderExecutable"
    }
    $BlenderExecutable = $resolvedCommand.Source
}

$defaults = Get-Content -LiteralPath $defaultsPath -Raw | ConvertFrom-Json
$maleOutput = Resolve-ConfiguredAssetPath -FieldName "maleSourceAssetPath" -Defaults $defaults -AssetPath $settingsAssetPath -Root $ProjectRoot
$femaleOutput = Resolve-ConfiguredAssetPath -FieldName "femaleSourceAssetPath" -Defaults $defaults -AssetPath $settingsAssetPath -Root $ProjectRoot

if ($maleOutput -eq $femaleOutput) {
    throw "I percorsi di output maschile e femminile coincidono."
}

$existingOutputs = @($maleOutput, $femaleOutput) | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf }
if ($existingOutputs.Count -gt 0 -and -not $Force) {
    throw ("Gli FBX esistono già. Per sostituirli esplicitamente, ripetere con -Force:" +
        [Environment]::NewLine + "- " + ($existingOutputs -join ([Environment]::NewLine + "- ")))
}

$startedAtUtc = [DateTime]::UtcNow
$startInfo = [System.Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = $BlenderExecutable
$startInfo.Arguments = "--background " + (ConvertTo-ProcessArgument $blendPath) +
    " --python " + (ConvertTo-ProcessArgument $exportScriptPath)
$startInfo.WorkingDirectory = $ProjectRoot
$startInfo.UseShellExecute = $false
$startInfo.CreateNoWindow = $true
$startInfo.RedirectStandardOutput = $true
$startInfo.RedirectStandardError = $true

$process = [System.Diagnostics.Process]::new()
$processStarted = $false
try {
    $process.StartInfo = $startInfo
    if (-not $process.Start()) {
        throw "Impossibile avviare Blender."
    }
    $processStarted = $true

    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        Stop-ProcessTree -Process $process
        $process.WaitForExit()
        throw "Timeout Blender dopo $TimeoutSeconds secondi."
    }

    $process.WaitForExit()
    $stdout = $stdoutTask.GetAwaiter().GetResult()
    $stderr = $stderrTask.GetAwaiter().GetResult()
    $exitCode = [int]$process.ExitCode

    if (-not [string]::IsNullOrWhiteSpace($stdout)) {
        Write-Host $stdout.TrimEnd()
    }
    if (-not [string]::IsNullOrWhiteSpace($stderr)) {
        Write-Host "=== Blender STDERR ==="
        Write-Host $stderr.TrimEnd()
    }
    if ($exitCode -ne 0) {
        throw "Blender terminato con codice $exitCode."
    }
}
finally {
    if ($processStarted -and -not $process.HasExited) {
        Stop-ProcessTree -Process $process
    }
    $process.Dispose()
}

if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf)) {
    throw "Blender è terminato senza produrre il report: $reportPath"
}

$reportFile = Get-Item -LiteralPath $reportPath
if ($reportFile.LastWriteTimeUtc -lt $startedAtUtc.AddSeconds(-1)) {
    throw "Il report trovato non è stato aggiornato da questa esecuzione."
}

$report = @(Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json)
if ($report.Count -ne 2) {
    throw "Il report deve contenere esattamente due personaggi; trovati: $($report.Count)."
}

foreach ($characterName in @("male", "female")) {
    $entry = $report | Where-Object { $_.character -eq $characterName } | Select-Object -First 1
    if ($null -eq $entry) {
        throw "Report incompleto: personaggio $characterName mancante."
    }

    if ([int]$entry.bones -le 0 -or @($entry.parts).Count -ne 6) {
        throw "Report non valido per $characterName: scheletro o numero di mesh inatteso."
    }

    $expectedPath = if ($characterName -eq "male") { $maleOutput } else { $femaleOutput }
    $reportedPath = [IO.Path]::GetFullPath([string]$entry.file)
    if ($reportedPath -ne $expectedPath) {
        throw "Percorso esportato inatteso per $characterName: $reportedPath"
    }

    $outputFile = Get-Item -LiteralPath $expectedPath -ErrorAction Stop
    if ($outputFile.Length -le 0 -or $outputFile.Length -ne [long]$entry.bytes) {
        throw "Dimensione FBX non coerente con il report per $characterName."
    }

    foreach ($part in @($entry.parts)) {
        if ([int]$part.vertices -le 0 -or [int]$part.triangles -le 0) {
            throw "Mesh vuota nel report $characterName: $($part.name)"
        }
    }

    Write-Host ("Export verificato: {0} | {1:N0} byte | {2} ossa | {3} mesh" -f
        $characterName, $outputFile.Length, $entry.bones, @($entry.parts).Count)
}

Write-Host "Esportazione FBX maschile e femminile completata e verificata."

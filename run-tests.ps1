[CmdletBinding()]
param([ValidateSet("Debug", "Release")][string]$Configuration = "Release")

& (Join-Path $PSScriptRoot "Tools/Testing/run-tests.ps1") -Configuration $Configuration
exit $LASTEXITCODE

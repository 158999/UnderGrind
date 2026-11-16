[CmdletBinding()]
param(
    [ValidateSet("EditMode", "PlayMode", "All")][string]$Platform = "All",
    [string]$UnityEditorPath = $env:UNITY_EDITOR_PATH
)

& (Join-Path $PSScriptRoot "Tools/Testing/run-unity-tests.ps1") -Platform $Platform -UnityEditorPath $UnityEditorPath
exit $LASTEXITCODE

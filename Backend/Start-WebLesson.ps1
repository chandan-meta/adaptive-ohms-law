param([string]$NodePath='node')
$ErrorActionPreference='Stop'
$taskProject = Split-Path $PSScriptRoot -Parent
if (!(Test-Path -LiteralPath (Join-Path $taskProject 'Builds/WebGL/index.html'))) {
    throw 'Build the Web lesson in Unity first: Adaptive Learning > Build Complete Web Lesson.'
}
if (!(Get-NetTCPConnection -LocalPort 8787 -State Listen -ErrorAction SilentlyContinue)) {
    if (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'Local/google-key.dpapi')) {
        & (Join-Path $PSScriptRoot 'Start-Wattson.ps1') -NodePath $NodePath
    }
}
if (!(Get-NetTCPConnection -LocalPort 8080 -State Listen -ErrorAction SilentlyContinue)) {
    $taskServer = Join-Path $PSScriptRoot 'serve-webgl.mjs'
    Start-Process -FilePath $NodePath -ArgumentList ('"'+$taskServer+'"') -WorkingDirectory $taskProject -WindowStyle Hidden | Out-Null
}
Write-Output 'Open http://localhost:8080 and choose Start lesson.'

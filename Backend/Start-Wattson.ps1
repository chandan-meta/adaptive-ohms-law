param([string]$NodePath='node')
$ErrorActionPreference='Stop'
$taskKeyPath=Join-Path $PSScriptRoot 'Local/google-key.dpapi'
if (!(Test-Path -LiteralPath $taskKeyPath)) { throw 'Set up the encrypted Google credential first. See README.md.' }
$taskSecure=(Get-Content -LiteralPath $taskKeyPath -Raw).Trim() | ConvertTo-SecureString
$taskPrevious=$env:GEMINI_API_KEY
try {
    $env:GEMINI_API_KEY=[System.Net.NetworkCredential]::new('', $taskSecure).Password
    $taskServer=Join-Path $PSScriptRoot 'wattson-server.mjs'
    $taskProcess=Start-Process -FilePath $NodePath -ArgumentList ('"'+$taskServer+'"') -WorkingDirectory $PSScriptRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $PSScriptRoot 'Local/server.log') -RedirectStandardError (Join-Path $PSScriptRoot 'Local/server-errors.log')
    $taskProcess.Id | Set-Content (Join-Path $PSScriptRoot 'Local/server.pid')
    Write-Output 'Wattson backend started. Credential is not stored in Unity.'
} finally { $env:GEMINI_API_KEY=$taskPrevious;$taskSecure=$null }

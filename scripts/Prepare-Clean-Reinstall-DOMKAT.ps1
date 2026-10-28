param(
    [string] $Version = '0.2.1',
    [string] $Stage = 'D:\ChatGPTMCP-Migration'
)
# Read-only preflight. This does not stop or delete the live tunnel.
$ErrorActionPreference = 'Stop'
if ($env:COMPUTERNAME -ne 'DOMKAT') { throw 'This script is intended for DOMKAT.' }
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid version' }
$idFile = Join-Path $env:LOCALAPPDATA 'ChatGptMcpTunnel\tunnel-id.txt'
if (!(Test-Path -LiteralPath $idFile)) { throw 'Existing OpenAI Tunnel ID not found. Do not uninstall yet.' }
$id = (Get-Content -LiteralPath $idFile -Raw).Trim()
if ($id -notmatch '^tunnel_(?:[a-z0-9]{4}_)?[a-z0-9]{32}$') { throw 'Existing OpenAI Tunnel ID invalid. No changes performed.' }
$asset = "ChatGPT_MCP_Tunnel_Setup_v$Version.exe"
$api = "https://api.github.com/repos/lvlaksim1/chatgpt-mcp-tunnel/releases/tags/v$Version"
$release = Invoke-RestMethod -Uri $api -Headers @{'User-Agent'='ChatGPTMCPMigration/1.0'}
$file = @($release.assets | Where-Object { $_.name -ceq $asset })
if ($file.Count -ne 1) { throw "Verified installer not found in GitHub Release v$Version" }
New-Item -ItemType Directory -Force -Path $Stage | Out-Null
$installer = Join-Path $Stage $asset
Invoke-WebRequest -Uri $file[0].browser_download_url -OutFile $installer
if (!$file[0].digest -or !$file[0].digest.StartsWith('sha256:')) {
    throw 'Release SHA256 digest missing. No changes to current tunnel.'
}
$actual = (Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash.ToLowerInvariant()
$expected = $file[0].digest.Substring(7).ToLowerInvariant()
if ($actual -ne $expected) {
    Remove-Item -LiteralPath $installer -Force
    throw 'Installer SHA256 mismatch. Old tunnel has not been touched.'
}
Set-Content -LiteralPath (Join-Path $Stage 'installer-sha256.txt') -Value $expected -Encoding ASCII
# Preserve only the existing logical Tunnel ID: no old token, DPAPI key, auth profile.
Set-Content -LiteralPath (Join-Path $Stage 'existing-tunnel-id.txt') -Value $id -Encoding ASCII
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Complete-Clean-Reinstall-DOMKAT.ps1') -Destination (Join-Path $Stage 'Complete-Clean-Reinstall-DOMKAT.ps1') -Force
$runner = @'
@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Complete-Clean-Reinstall-DOMKAT.ps1"
pause
'@
[IO.File]::WriteAllText((Join-Path $Stage 'Start-Clean-Install.cmd'), $runner, [Text.Encoding]::ASCII)
Write-Host 'CLEAN_INSTALL_READY'
Write-Host "Folder: $Stage"
Write-Host "Installer: $asset"
Write-Host 'Existing logical OpenAI Tunnel ID retained. No keys retained.'
Write-Host 'Live tunnel and its autostart remain unchanged until Start-Clean-Install.cmd is run.'

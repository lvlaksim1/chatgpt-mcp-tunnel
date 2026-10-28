@echo off
setlocal
set "CHATGPT_MCP_RUNNER_DIR=%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$s=Get-Content -LiteralPath '%~f0' -Raw -Encoding UTF8; $tag=':: POWERSHELL BELOW ::'; & ([scriptblock]::Create($s.Substring($s.LastIndexOf($tag)+$tag.Length)))"
exit /b %errorlevel%

:: POWERSHELL BELOW ::
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath (Join-Path $env:CHATGPT_MCP_RUNNER_DIR '..')).Path
$script = Join-Path $root 'scripts\Start-Tunnel.ps1'
if (-not (Test-Path -LiteralPath $script -PathType Leaf)) { throw "Missing tunnel launcher: $script" }

$rx = [regex]::Escape($root)
$active = @(Get-CimInstance Win32_Process | Where-Object {
    $_.CommandLine -match $rx -and (
        $_.Name -eq 'tunnel-client.exe' -or
        ($_.Name -eq 'powershell.exe' -and $_.CommandLine -match '\-File\s+' -and $_.CommandLine -match 'Start-Tunnel\.ps1')
    )
})
if ($active.Count -gt 0) {
    Write-Output 'ChatGPT MCP Tunnel is already running.'
    exit 0
}

$exe = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
$arguments = '-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File ' + [char]34 + $script + [char]34
Start-Process -FilePath $exe -ArgumentList $arguments -WorkingDirectory $root -WindowStyle Hidden
Write-Output 'ChatGPT MCP Tunnel launch requested.'

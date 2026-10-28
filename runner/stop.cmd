@echo off
setlocal
set "CHATGPT_MCP_RUNNER_DIR=%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$s=Get-Content -LiteralPath '%~f0' -Raw -Encoding UTF8; $tag=':: POWERSHELL BELOW ::'; & ([scriptblock]::Create($s.Substring($s.LastIndexOf($tag)+$tag.Length)))"
exit /b %errorlevel%

:: POWERSHELL BELOW ::
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath (Join-Path $env:CHATGPT_MCP_RUNNER_DIR '..')).Path
$rx = [regex]::Escape($root)
$targets = @(Get-CimInstance Win32_Process | Where-Object {
    $_.CommandLine -match $rx -and (
        $_.Name -eq 'tunnel-client.exe' -or
        ($_.Name -eq 'powershell.exe' -and $_.CommandLine -match '\-File\s+' -and $_.CommandLine -match 'Start-Tunnel\.ps1')
    )
})
if ($targets.Count -eq 0) {
    Write-Output 'ChatGPT MCP Tunnel is not running.'
    exit 0
}
foreach ($process in $targets) {
    try {
        Stop-Process -Id $process.ProcessId -Force -ErrorAction Stop
        Write-Output ('Stopped ' + $process.Name + ' PID ' + $process.ProcessId)
    } catch {
        if (Get-Process -Id $process.ProcessId -ErrorAction SilentlyContinue) { throw }
    }
}
Write-Output 'ChatGPT MCP Tunnel stopped. Startup setting is unchanged.'

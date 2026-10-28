@echo off
setlocal
set "CHATGPT_MCP_RUNNER_DIR=%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$s=Get-Content -LiteralPath '%~f0' -Raw -Encoding UTF8; $tag=':: POWERSHELL BELOW ::'; & ([scriptblock]::Create($s.Substring($s.LastIndexOf($tag)+$tag.Length)))"
exit /b %errorlevel%

:: POWERSHELL BELOW ::
$ErrorActionPreference = 'Stop'
$taskName = 'ChatGPT MCP Tunnel'
$service = New-Object -ComObject 'Schedule.Service'
$service.Connect()
$folder = $service.GetFolder('\')
$task = $null
try { $task = $folder.GetTask($taskName) } catch {}
if ($null -ne $task) {
    $folder.DeleteTask($taskName, 0)
    Write-Output 'ChatGPT MCP Tunnel autostart disabled.'
} else {
    Write-Output 'Autostart was not installed.'
}
Write-Output 'The currently running tunnel is unaffected.'

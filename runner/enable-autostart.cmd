@echo off
setlocal
set "CHATGPT_MCP_RUNNER_DIR=%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$s=Get-Content -LiteralPath '%~f0' -Raw -Encoding UTF8; $tag=':: POWERSHELL BELOW ::'; & ([scriptblock]::Create($s.Substring($s.LastIndexOf($tag)+$tag.Length)))"
exit /b %errorlevel%

:: POWERSHELL BELOW ::
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath (Join-Path $env:CHATGPT_MCP_RUNNER_DIR '..')).Path
$startFile = Join-Path $env:CHATGPT_MCP_RUNNER_DIR 'start.cmd'
if (-not (Test-Path -LiteralPath $startFile -PathType Leaf)) { throw "Missing runner: $startFile" }

$taskName = 'ChatGPT MCP Tunnel'
$user = [Security.Principal.WindowsIdentity]::GetCurrent().Name
$service = New-Object -ComObject 'Schedule.Service'
$service.Connect()
$definition = $service.NewTask(0)
$definition.RegistrationInfo.Description = 'Start the independent ChatGPT MCP Tunnel when this Windows user signs in.'
$definition.Principal.UserId = $user
$definition.Principal.LogonType = 3
$definition.Principal.RunLevel = 0
$trigger = $definition.Triggers.Create(9)
$trigger.UserId = $user
$trigger.Delay = 'PT20S'
$action = $definition.Actions.Create(0)
$action.Path = $env:ComSpec
$action.Arguments = '/d /c ' + [char]34 + [char]34 + $startFile + [char]34 + [char]34
$action.WorkingDirectory = $root
$definition.Settings.MultipleInstances = 2
$definition.Settings.ExecutionTimeLimit = 'PT0S'
$definition.Settings.StopIfGoingOnBatteries = $false
$definition.Settings.DisallowStartIfOnBatteries = $false
$definition.Settings.StartWhenAvailable = $true
$definition.Settings.Hidden = $true
$null = $service.GetFolder('\').RegisterTaskDefinition($taskName, $definition, 6, $null, $null, 3, $null)
Write-Output ('Autostart enabled for ' + $user + ' (20 seconds after Windows sign-in).')
Write-Output 'The current tunnel is unaffected; the task applies at the next sign-in.'

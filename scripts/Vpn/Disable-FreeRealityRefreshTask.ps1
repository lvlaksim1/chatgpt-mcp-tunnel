$ErrorActionPreference='Stop'
$name='ChatGPTMCP-FreeReality-Refresh'
$task=Get-ScheduledTask -TaskName $name -ErrorAction SilentlyContinue
if(-not $task){Write-Output 'AUTO_REFRESH_TASK_NOT_INSTALLED';exit 0}
if($task.State -eq 'Disabled'){Write-Output 'AUTO_REFRESH_ALREADY_DISABLED';exit 0}
Disable-ScheduledTask -TaskName $name|Out-Null
Write-Output 'AUTO_REFRESH_DISABLED; EXISTING_PROFILE_PRESERVED; MCP_NOT_RESTARTED'

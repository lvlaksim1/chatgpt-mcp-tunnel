param(
 [string]$ProjectRoot='D:\Dev\ChatGPT-MCP-Tunnel',
 [int]$EveryHours=3
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
if($EveryHours -lt 1 -or $EveryHours -gt 12 -or (24 % $EveryHours -ne 0)){
 throw 'Update interval must divide 24 hours and be between 1 and 12'
}
$taskName='ChatGPTMCP-FreeReality-Refresh'
$existing=Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if($existing){throw 'Scheduled task already exists; inspect it before replacing'}
$script=Join-Path $ProjectRoot 'scripts\Vpn\free_reality_updater.py'
if(!(Test-Path $script)){throw 'Updater script missing'}
$python=(Get-Command python.exe -ErrorAction Stop).Source
$pythonw=Join-Path (Split-Path $python -Parent) 'pythonw.exe'
if(!(Test-Path $pythonw)){throw 'pythonw.exe not installed'}
$manifest=Join-Path $ProjectRoot '.tools\vpn-pilot\binary-manifest.json'
if(!(Test-Path $manifest)){throw 'Verified Xray binary missing'}
# The task never switches the live WebView or MCP processes. It only updates
# the catalog and DPAPI-protected "Free-Reality-Auto" profile in LocalAppData.
$args='"'+$script+'" --max-probes 16 --timeout 9 --port 11836'
$action=New-ScheduledTaskAction -Execute $pythonw -Argument $args -WorkingDirectory $ProjectRoot
$trigger=New-ScheduledTaskTrigger -Daily -At (Get-Date).AddHours($EveryHours)
$rep=New-CimInstance -Namespace root/Microsoft/Windows/TaskScheduler -ClassName MSFT_TaskRepetitionPattern -ClientOnly -Property @{
 Interval=('PT'+$EveryHours+'H')
 Duration='P1D'
 StopAtDurationEnd=$false
}
$trigger.Repetition=$rep
$settings=New-ScheduledTaskSettingsSet -StartWhenAvailable -ExecutionTimeLimit (New-TimeSpan -Minutes 15) -MultipleInstances IgnoreNew -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
$user=[Security.Principal.WindowsIdentity]::GetCurrent().Name
$principal=New-ScheduledTaskPrincipal -UserId $user -LogonType Interactive -RunLevel Limited
Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Settings $settings -Principal $principal -Description 'Refresh public free VLESS/REALITY list and DPAPI profile without touching live MCP or Happ' | Out-Null
$task=Get-ScheduledTask -TaskName $taskName
$info=Get-ScheduledTaskInfo -TaskName $taskName
[pscustomobject]@{
 status='REGISTERED'
 taskName=$taskName
 user=$user
 everyHours=$EveryHours
 nextRun=$info.NextRunTime.ToString('yyyy-MM-dd HH:mm:ss')
 python=$pythonw
 stateDirectory=(Join-Path $env:LOCALAPPDATA 'ChatGPTMCPAgent\Vpn\profiles')
 noLiveProxySwitch=$true
}|ConvertTo-Json -Compress

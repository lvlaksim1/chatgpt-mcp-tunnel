param([string]$StorageRoot='')
$ErrorActionPreference='Stop'
if([string]::IsNullOrWhiteSpace($StorageRoot)){
 $StorageRoot=Join-Path $env:LOCALAPPDATA 'ChatGPTMCPAgent\Vpn\profiles'
}
$path=Join-Path $StorageRoot 'status.json'
$taskName='ChatGPTMCP-FreeReality-Refresh'
$task=Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
$taskInfo=if($task){Get-ScheduledTaskInfo -TaskName $taskName}else{$null}
$status=if(Test-Path $path){Get-Content -LiteralPath $path -Raw -Encoding UTF8|ConvertFrom-Json}else{$null}
[pscustomobject]@{
 TaskInstalled=[bool]$task
 TaskState=if($task){$task.State.ToString()}else{'Not installed'}
 NextUpdate=if($taskInfo){$taskInfo.NextRunTime.ToString('yyyy-MM-dd HH:mm:ss')}else{$null}
 LastTaskResult=if($taskInfo){$taskInfo.LastTaskResult}else{$null}
 LastUpdate=if($status){$status.updatedAt}else{$null}
 RefreshStatus=if($status){$status.status}else{'No data'}
 Candidates=if($status){$status.availableCandidates}else{0}
 Probed=if($status){$status.probed}else{0}
 Healthy=if($status){$status.healthy}else{0}
 SelectedEndpoint=if($status -and $status.selected){$status.selected.host+':'+$status.selected.port}else{$null}
 SelectedSource=if($status -and $status.selected){$status.selected.source}else{$null}
 ProfileEncrypted=(Test-Path (Join-Path $StorageRoot 'Free-Reality-Auto.dpapi'))
 LiveMcpProxyChanged=$false
}|ConvertTo-Json -Depth 4 -Compress

[CmdletBinding()]
param(
    [switch]$Apply,
    [switch]$ReplaceExisting,
    [ValidateRange(1,12)][int]$EveryHours = 3,
    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA 'Programs\ChatGPTMCPAgent'),
    [string]$PythonExe = ''
)
# Recovers ONLY the local client's free VPN watchdog and node refresh.
# Read-only without -Apply. Never changes a server or its single MCP tunnel-client.
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
if(24 % $EveryHours -ne 0){throw 'EveryHours must divide 24'}
$role=(Get-ItemProperty 'HKCU:\Software\ChatGPTMCPAgent' -ErrorAction Stop).Role
if($role -ne 'client'){throw 'This procedure is only for a Router Lite client'}
$installDir=[IO.Path]::GetFullPath($InstallDir)
$source=Join-Path $installDir 'Recovery'
$supervisor=Join-Path $source 'Client-FreeXraySupervisor.ps1'
$updater=Join-Path $source 'scripts\Vpn\free_reality_updater.py'
$feeds=Join-Path $source 'scripts\Vpn\free_reality_sources.json'
$xray=Join-Path $installDir 'Vpn\xray.exe'
$xrayHash=Join-Path $installDir 'Vpn\xray.sha256'
foreach($file in @($supervisor,$updater,$feeds,$xray,$xrayHash)){
    if(-not (Test-Path -LiteralPath $file -PathType Leaf)){throw "Missing installed recovery dependency: $file"}
}
if((Get-FileHash -LiteralPath $xray -Algorithm SHA256).Hash -ine
   (Get-Content -LiteralPath $xrayHash -Raw).Trim()){
    throw 'Installed Xray failed SHA256 verification'
}
if(-not $PythonExe){
    $candidate=Get-Command python.exe -ErrorAction SilentlyContinue
    if($null -eq $candidate){throw 'Python runtime required for free node updater'}
    $PythonExe=$candidate.Source
}
$PythonExe=[IO.Path]::GetFullPath($PythonExe)
if(-not (Test-Path $PythonExe -PathType Leaf)){throw 'Python executable does not exist'}
$tasks=@(
  [pscustomobject]@{
    name='ChatGPTMCP-Client-FreeXray'
    execute=(Join-Path $PSHOME 'powershell.exe')
    arguments=('-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "'+$supervisor+'" -InstallDir "'+$installDir+'"')
  },
  [pscustomobject]@{
    name='ChatGPTMCP-Client-FreeReality-Refresh'
    execute=$PythonExe
    arguments=('"'+$updater+'" --sources "'+$feeds+'" --xray-binary "'+$xray+'" --xray-sha256-file "'+$xrayHash+'" --max-probes 16 --timeout 9 --port 11836')
  }
)
$existing=@()
foreach($task in $tasks){
    if($null -ne (Get-ScheduledTask -TaskName $task.name -ErrorAction SilentlyContinue)){
        $existing += $task.name
    }
}
if($existing.Count -gt 0 -and $Apply -and -not $ReplaceExisting){
    throw ('Refusing to change existing tasks: '+($existing -join ', '))
}
if(-not $Apply){
    [pscustomobject]@{
        status='INSPECT_ONLY'
        requiredTaskNames=@($tasks|ForEach-Object {$_.name})
        existingTaskNames=$existing
        scriptSource=$source
        xraySha256Verified=$true
        mode='client-only'
        liveProcessesChanged=$false
    }|ConvertTo-Json -Compress
    return
}
$user=[Security.Principal.WindowsIdentity]::GetCurrent().Name
$principal=New-ScheduledTaskPrincipal -UserId $user -LogonType Interactive -RunLevel Limited
$settings=New-ScheduledTaskSettingsSet -StartWhenAvailable -MultipleInstances IgnoreNew -ExecutionTimeLimit (New-TimeSpan -Minutes 30) -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
$logon=New-ScheduledTaskTrigger -AtLogOn -User $user
$timer=New-ScheduledTaskTrigger -Daily -At (Get-Date).Date.AddHours($EveryHours)
$repeat=New-CimInstance -Namespace root/Microsoft/Windows/TaskScheduler -ClassName MSFT_TaskRepetitionPattern -ClientOnly -Property @{
    Interval=('PT'+$EveryHours+'H')
    Duration='P1D'
    StopAtDurationEnd=$false
}
$timer.Repetition=$repeat
foreach($task in $tasks){
    $trigger=if($task.name -eq 'ChatGPTMCP-Client-FreeXray'){$logon}else{$timer}
    $action=New-ScheduledTaskAction -Execute $task.execute -Argument $task.arguments -WorkingDirectory $source
    Register-ScheduledTask -TaskName $task.name -Principal $principal -Action $action -Settings $settings -Trigger $trigger -Force:$ReplaceExisting -Description 'ChatGPT MCP Tunnel local CLIENT VPN recovery; no Happ, no additional OpenAI tunnel-client'|Out-Null
}
[pscustomobject]@{
    status='REGISTERED'
    names=@($tasks|ForEach-Object {$_.name})
    user=$user
    everyHours=$EveryHours
    liveProcessesChanged=$false
    requirement='Run installed GUI and ensure local Free VPN health before ChatGPT login'
}|ConvertTo-Json -Compress

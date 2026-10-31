param(
 [string]$ProfileName='LamaVPN-Germany',
 [string]$StorageRoot=''
)
$ErrorActionPreference='Stop'
if([string]::IsNullOrWhiteSpace($StorageRoot)){$StorageRoot=Join-Path $env:LOCALAPPDATA 'ChatGPTMCPAgent\Vpn\profiles'}
$slug=[Regex]::Replace($ProfileName,'[^A-Za-z0-9_-]','-').Trim('-')
$statePath=Join-Path $StorageRoot ($slug+'.state.json')
if(!(Test-Path $statePath)){Write-Output 'PROTECTED_XRAY_NOT_RUNNING';exit 0}
$state=Get-Content -LiteralPath $statePath -Raw|ConvertFrom-Json
$p=Get-CimInstance Win32_Process -Filter "ProcessId=$($state.pid)" -ErrorAction SilentlyContinue
if($p) {
 if($p.Name -ine 'xray.exe' -or $p.ExecutablePath -ine $state.executable){
   throw 'Process owner changed; refusing to terminate unrelated PID'
 }
 Stop-Process -Id $state.pid -Force -ErrorAction Stop
 for($i=0;$i -lt 30 -and (Get-Process -Id $state.pid -ErrorAction SilentlyContinue);$i++){Start-Sleep -Milliseconds 100}
 if(Get-Process -Id $state.pid -ErrorAction SilentlyContinue){throw 'Protected Xray process could not be stopped'}
}
Remove-Item -LiteralPath $statePath -Force
Write-Output ("PROTECTED_XRAY_STOPPED profile="+$slug)

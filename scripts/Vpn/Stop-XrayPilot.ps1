param([int]$Port=11809,[string]$Root='D:\Dev\ChatGPT-MCP-Tunnel')
$ErrorActionPreference='Stop'
$base=Join-Path $Root '.tools\vpn-pilot'
$state=Join-Path $base $(if($Port -eq 11809){'pilot-state.json'}else{"pilot-state-$Port.json"})
if(!(Test-Path $state)){Write-Output 'NO_PILOT_STATE';exit 0}
$o=Get-Content $state -Raw|ConvertFrom-Json
$proc=Get-CimInstance Win32_Process -Filter "ProcessId=$($o.pid)" -ErrorAction SilentlyContinue
if($proc){
 if($proc.Name -ine 'xray.exe' -or $proc.ExecutablePath -ine $o.executable){
   throw 'PID ownership changed: refusing to stop unrelated process'
 }
 Stop-Process -Id $o.pid -Force -ErrorAction Stop
 for($i=0;$i -lt 25 -and (Get-Process -Id $o.pid -ErrorAction SilentlyContinue);$i++){Start-Sleep -Milliseconds 100}
 if(Get-Process -Id $o.pid -ErrorAction SilentlyContinue){throw 'Pilot still running'}
}
Remove-Item -LiteralPath $state -Force
Write-Output ("XRAY_PILOT_STOPPED PID="+$o.pid+" MODE="+$o.mode)

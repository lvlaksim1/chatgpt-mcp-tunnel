param(
 [ValidateSet('direct','via-happ')][string]$Mode='direct',
 [int]$Port=11809,
 [string]$Root='D:\Dev\ChatGPT-MCP-Tunnel'
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
if($Port -lt 1025 -or $Port -gt 65535 -or $Port -in @(10808,10809)){throw 'Invalid port or reserved Happ port'}
$base=Join-Path $Root '.tools\vpn-pilot'
$manifest=Join-Path $base 'binary-manifest.json'
if(!(Test-Path $manifest)){throw 'Run Install-XrayPilot.ps1 first'}
$bin=Get-Content $manifest -Raw|ConvertFrom-Json
$exe=$bin.xray_exe
if(!(Test-Path $exe) -or (Get-FileHash $exe).Hash -ine $bin.binary_sha256){throw 'Xray executable SHA256 mismatch'}
$state=Join-Path $base $(if($Port -eq 11809){'pilot-state.json'}else{"pilot-state-$Port.json"})
Write-Output ("PILOT_PREFLIGHT mode="+$Mode+" port="+$Port+" state="+$state)
if(Test-Path $state){
  $old=Get-Content $state -Raw|ConvertFrom-Json
  $previous=Get-CimInstance Win32_Process -Filter "ProcessId=$($old.pid)" -ErrorAction SilentlyContinue
  if($previous -and $previous.ExecutablePath -ieq $exe){
    if($old.mode -ne $Mode -or $old.port -ne $Port){throw 'Existing pilot mode or port differs'}
    [pscustomobject]@{status='already-running';mode=$Mode;pid=$old.pid;proxy=("http://127.0.0.1:"+$Port)}|ConvertTo-Json -Compress
    return
  }
  throw 'Stale pilot state; inspect before removing state file'
}
if(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue){throw "Port $Port already has a listener"}
if($Mode -eq 'via-happ' -and !(Get-NetTCPConnection -LocalPort 10808 -State Listen -ErrorAction SilentlyContinue)){throw 'Happ SOCKS proxy 10808 not available'}
$configDir=Join-Path $base 'configs'
New-Item -ItemType Directory -Path $configDir -Force|Out-Null
$configPath=Join-Path $configDir ("pilot-$Mode-$Port.json")
$outbound=if($Mode -eq 'direct'){
 @{tag='pilot-egress';protocol='freedom';settings=@{}}
}else{
 @{tag='pilot-egress';protocol='socks';settings=@{servers=@(@{address='127.0.0.1';port=10808})}}
}
$config=@{
 log=@{loglevel='warning'}
 inbounds=@(@{tag='pilot-http';listen='127.0.0.1';port=$Port;protocol='http';settings=@{}})
 outbounds=@($outbound)
}
[IO.File]::WriteAllText($configPath,($config | ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
$test=& $exe run -test -config $configPath 2>&1
if($LASTEXITCODE -ne 0){throw "Xray config validation failed: $test"}
$stdout=Join-Path $base 'pilot.stdout.log'
$stderr=Join-Path $base 'pilot.stderr.log'
$proc=Start-Process -FilePath $exe -ArgumentList @('run','-config',('"' + $configPath + '"')) -WorkingDirectory (Split-Path $exe -Parent) -RedirectStandardOutput $stdout -RedirectStandardError $stderr -PassThru
$open=$false
for($i=0;$i -lt 70;$i++){
 if($proc.HasExited){throw "Xray pilot exited code $($proc.ExitCode): $(Get-Content $stderr -Raw -ErrorAction SilentlyContinue)"}
 if(Get-NetTCPConnection -LocalPort $Port -State Listen -OwningProcess $proc.Id -ErrorAction SilentlyContinue){$open=$true;break}
 Start-Sleep -Milliseconds 150
}
if(-not $open){Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue;throw 'Xray pilot did not open the port'}
@{pid=$proc.Id;port=$Port;mode=$Mode;executable=$exe;config=$configPath;started=[DateTimeOffset]::UtcNow.ToString('O')} | ConvertTo-Json -Depth 5|Set-Content -LiteralPath $state -Encoding UTF8
[pscustomobject]@{status='started';mode=$Mode;pid=$proc.Id;proxy="http://127.0.0.1:$Port";config=$configPath}|ConvertTo-Json -Compress

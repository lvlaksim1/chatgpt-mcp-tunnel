param([string]$Root='D:\Dev\ChatGPT-MCP-Tunnel')
$ErrorActionPreference='Stop'
$base=Join-Path $Root '.tools\vpn-pilot'
$manifest=Get-Content (Join-Path $base 'binary-manifest.json') -Raw|ConvertFrom-Json
$exe=$manifest.xray_exe
if((Get-FileHash $exe).Hash -ine $manifest.binary_sha256){throw 'Binary checksum mismatch'}
foreach($port in @(11812,11813)){
 if(Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue){throw "Pilot port $port already used"}
}
$guid=[guid]::NewGuid().ToString('D')
$dir=Join-Path $base 'vless-loopback'
New-Item -Path $dir -ItemType Directory -Force|Out-Null
$server=@{
 log=@{loglevel='warning'}
 inbounds=@(@{listen='127.0.0.1';port=11812;protocol='vless';tag='local-vless';settings=@{clients=@(@{id=$guid});decryption='none'}})
 outbounds=@(@{protocol='freedom';tag='direct';settings=@{}})
}
$client=@{
 log=@{loglevel='warning'}
 inbounds=@(@{listen='127.0.0.1';port=11813;protocol='http';tag='local-http';settings=@{}})
 outbounds=@(@{protocol='vless';tag='vless-to-local-server';settings=@{address='127.0.0.1';port=11812;id=$guid;encryption='none'}})
}
$cfgServer=Join-Path $dir 'server.json'
$cfgClient=Join-Path $dir 'client.json'
[IO.File]::WriteAllText($cfgServer,($server|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText($cfgClient,($client|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
$p1=$null;$p2=$null
try{
 foreach($file in @($cfgServer,$cfgClient)){
  $log=& $exe run -test -config $file 2>&1
  if($LASTEXITCODE -ne 0){throw "Invalid VLESS loopback config: $log"}
 }
 $p1=Start-Process -FilePath $exe -ArgumentList @('run','-config',('"'+$cfgServer+'"')) -WorkingDirectory (Split-Path $exe) -RedirectStandardOutput (Join-Path $dir 'server.stdout.log') -RedirectStandardError (Join-Path $dir 'server.stderr.log') -PassThru
 Start-Sleep -Milliseconds 900
 if($p1.HasExited){throw 'Local VLESS server exited'}
 $p2=Start-Process -FilePath $exe -ArgumentList @('run','-config',('"'+$cfgClient+'"')) -WorkingDirectory (Split-Path $exe) -RedirectStandardOutput (Join-Path $dir 'client.stdout.log') -RedirectStandardError (Join-Path $dir 'client.stderr.log') -PassThru
 Start-Sleep -Milliseconds 900
 if($p2.HasExited){throw 'VLESS client exited'}
 $response=(& curl.exe --disable --proxy http://127.0.0.1:11813 --head --silent --show-error --max-time 15 -w 'HTTP_STATUS=%{http_code}' https://example.com/ 2>&1 | Out-String)
 $exit=$LASTEXITCODE
 $ok=($exit -eq 0 -and $response -match 'HTTP_STATUS=200')
 $report=@{scenario='VLESS loopback transport (unencrypted, localhost only)';server='127.0.0.1:11812';clientProxy='http://127.0.0.1:11813';httpOk=$ok;curlExit=$exit;testedAt=[DateTimeOffset]::UtcNow.ToString('O')}
 $report|ConvertTo-Json -Depth 3|Set-Content (Join-Path $dir 'test-report.json') -Encoding UTF8
 if(-not $ok){throw "VLESS loopback HTTP request failed code=function () { [native code] }: $($response.Substring(0,[Math]::Min(700,$response.Length)))"}
 $report|ConvertTo-Json -Compress
}finally{
 foreach($p in @($p2,$p1)){
  if($p -and -not $p.HasExited){Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue}
 }
}

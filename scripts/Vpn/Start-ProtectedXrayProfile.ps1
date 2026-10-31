param(
 [string]$ProfileName='LamaVPN-Germany',
 [string]$StorageRoot='',
 [string]$ProjectRoot='D:\Dev\ChatGPT-MCP-Tunnel'
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
if([string]::IsNullOrWhiteSpace($StorageRoot)){$StorageRoot=Join-Path $env:LOCALAPPDATA 'ChatGPTMCPAgent\Vpn\profiles'}
$slug=[Regex]::Replace($ProfileName,'[^A-Za-z0-9_-]','-').Trim('-')
if(-not $slug){$slug='profile'}
$secretPath=Join-Path $StorageRoot ($slug+'.dpapi')
$statePath=Join-Path $StorageRoot ($slug+'.state.json')
if(!(Test-Path $secretPath)){throw 'The protected profile is not imported'}
if(Test-Path $statePath){throw 'Profile state exists; stop or inspect the previous instance'}
$manifest=Get-Content (Join-Path $ProjectRoot '.tools\vpn-pilot\binary-manifest.json') -Raw | ConvertFrom-Json
$exe=$manifest.xray_exe
if((Get-FileHash $exe).Hash -ine $manifest.binary_sha256){throw 'Xray executable integrity mismatch'}
Add-Type -AssemblyName System.Security
$cipher=[IO.File]::ReadAllBytes($secretPath)
$plaintext=[Security.Cryptography.ProtectedData]::Unprotect($cipher,$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
try {$json=[Text.Encoding]::UTF8.GetString($plaintext)}
finally{[Array]::Clear($plaintext,0,$plaintext.Length)}
$config=$json | ConvertFrom-Json
if($config.outbounds[0].streamSettings.security -ne 'reality' -or
    $config.outbounds[0].protocol -ne 'vless' -or
    $config.inbounds[0].listen -ne '127.0.0.1'){throw 'Unexpected decrypted profile type'}
$port=[int]$config.inbounds[0].port
if(Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue){throw 'Selected private proxy port is busy'}
$temp=Join-Path $env:TEMP ("xray-protected-"+[Guid]::NewGuid().ToString('N')+".json")
$logRoot=Join-Path (Split-Path $StorageRoot -Parent) 'logs'
New-Item $logRoot -ItemType Directory -Force|Out-Null
$out=Join-Path $logRoot ($slug+'.stdout.log')
$err=Join-Path $logRoot ($slug+'.stderr.log')
$proc=$null
try {
 [IO.File]::WriteAllText($temp,$json,[Text.UTF8Encoding]::new($false))
 $proc=Start-Process -FilePath $exe -ArgumentList @('run','-config',('"'+$temp+'"')) -WorkingDirectory (Split-Path $exe -Parent) -RedirectStandardOutput $out -RedirectStandardError $err -PassThru
 $listening=$false
 for($i=0;$i -lt 90;$i++){
  if($proc.HasExited){throw 'Xray profile exited during startup'}
  if(Get-NetTCPConnection -LocalPort $port -State Listen -OwningProcess $proc.Id -ErrorAction SilentlyContinue){$listening=$true;break}
  Start-Sleep -Milliseconds 120
 }
 if(-not $listening){throw 'Xray profile did not listen on its expected local port'}
 @{pid=$proc.Id;executable=$exe;port=$port;profile=$slug;startedAt=[DateTimeOffset]::UtcNow.ToString('O')}|
  ConvertTo-Json|Set-Content -LiteralPath $statePath -Encoding UTF8
 [pscustomobject]@{status='STARTED';profile=$slug;pid=$proc.Id;localProxy=("http://127.0.0.1:"+$port);mode='VLESS/TCP/REALITY';configuredOnly=$true} | ConvertTo-Json -Compress
} catch {
 if($proc -and -not $proc.HasExited){Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue}
 throw
} finally {
 # Xray loads and parses config during startup; remove plaintext immediately.
 if(Test-Path $temp){Remove-Item $temp -Force}
}

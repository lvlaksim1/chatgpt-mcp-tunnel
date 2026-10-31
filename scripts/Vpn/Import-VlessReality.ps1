param(
    [Parameter(Mandatory=$true)][string]$SourceFile,
    [string]$ProfileName='LamaVPN-Germany',
    [string]$StorageRoot='',
    [int]$ProxyPort=11821,
    [string]$ProjectRoot='D:\Dev\ChatGPT-MCP-Tunnel'
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
if($ProxyPort -lt 1025 -or $ProxyPort -gt 65535 -or $ProxyPort -in @(10808,10809)){throw 'Invalid private proxy port'}
if([string]::IsNullOrWhiteSpace($StorageRoot)){$StorageRoot=Join-Path $env:LOCALAPPDATA 'ChatGPTMCPAgent\Vpn\profiles'}
$raw=Get-Content -LiteralPath $SourceFile -Raw -Encoding UTF8
$lines=@($raw -split '[\r\n]+' | Where-Object {$_.Trim().StartsWith('vless://')})
if($lines.Count -ne 1){throw 'Expected exactly one standard vless:// profile in the input file'}
$uri=[Uri]$lines[0].Trim()
if($uri.Scheme -ne 'vless'){throw 'Unsupported scheme'}
$uuid=$uri.UserInfo
$id=[Guid]::Empty
if(-not [Guid]::TryParse($uuid,[ref]$id) -or $id -eq [Guid]::Empty){throw 'Expected a VLESS UUID'}
$serverHost=$uri.DnsSafeHost
$serverPort=$uri.Port
if([string]::IsNullOrWhiteSpace($serverHost) -or $serverPort -lt 1 -or $serverPort -gt 65535){throw 'Invalid VLESS server endpoint'}
Add-Type -AssemblyName System.Web
$q=[System.Web.HttpUtility]::ParseQueryString($uri.Query.TrimStart('?'))
$security=$q['security'];$network=$q['type']
if([string]::IsNullOrWhiteSpace($network)){$network='tcp'};$encryption=$q['encryption']
if([string]::IsNullOrWhiteSpace($encryption)){$encryption='none'}
if($security -ne 'reality' -or $network -notin @('tcp','raw') -or $encryption -ne 'none'){
    throw 'Only VLESS TCP/REALITY with encryption=none is supported by this importer'
}
$pbk=$q['pbk'];$sni=$q['sni'];$sid=$q['sid'];$fp=$q['fp'];$flow=$q['flow']
if($pbk -notmatch '^[A-Za-z0-9_-]{42,44}$' -or [string]::IsNullOrWhiteSpace($sni)){
    throw 'Missing/invalid REALITY pbk or sni'
}
if($sid -and ($sid -notmatch '^[a-fA-F0-9]{0,16}$' -or ($sid.Length % 2) -ne 0)){throw 'Invalid REALITY shortId'}
if([string]::IsNullOrWhiteSpace($fp)){$fp='chrome'}
if($flow -and $flow -ne 'xtls-rprx-vision'){throw 'Unsupported VLESS flow'}
$user=@{id=$uuid;encryption='none'}
if($flow){$user['flow']=$flow}
$config=@{
    log=@{loglevel='warning'}
    inbounds=@(@{tag='tunnel-http';listen='127.0.0.1';port=$ProxyPort;protocol='http';settings=@{}})
    outbounds=@(@{
        tag='remote-vless'
        protocol='vless'
        settings=@{vnext=@(@{address=$serverHost;port=$serverPort;users=@($user)})}
        streamSettings=@{
            network='tcp'
            security='reality'
            realitySettings=@{serverName=$sni;fingerprint=$fp;password=$pbk;shortId=([string]$sid);spiderX=([string]$q['spx'])}
        }
    })
}
$json=$config|ConvertTo-Json -Depth 16 -Compress
$pilot=Get-Content -LiteralPath (Join-Path $ProjectRoot '.tools\vpn-pilot\binary-manifest.json') -Raw|ConvertFrom-Json
if(-not (Test-Path $pilot.xray_exe)){throw 'Verified Xray-core pilot is not installed'}
if((Get-FileHash $pilot.xray_exe).Hash -ine $pilot.binary_sha256){throw 'Xray binary hash changed'}
$tmp=Join-Path $env:TEMP ("xray-verify-"+[Guid]::NewGuid().ToString('N')+".json")
try{
    [IO.File]::WriteAllText($tmp,$json,[Text.UTF8Encoding]::new($false))
    $validation=& $pilot.xray_exe run -test -config $tmp 2>&1
    if($LASTEXITCODE -ne 0){throw ('Xray config validation failed: '+($validation|Out-String).Split([char]10)[0])}
}finally{
    if(Test-Path $tmp){Remove-Item -LiteralPath $tmp -Force}
}
$slug=[Regex]::Replace($ProfileName,'[^A-Za-z0-9_-]','-').Trim('-')
if(-not $slug){$slug='profile'}
New-Item -Path $StorageRoot -ItemType Directory -Force|Out-Null
$target=Join-Path $StorageRoot ($slug+'.dpapi')
$metadata=Join-Path $StorageRoot ($slug+'.meta.json')
if(Test-Path $target){throw 'Encrypted profile already exists; refusing overwrite'}
Add-Type -AssemblyName System.Security
$plaintext=[Text.Encoding]::UTF8.GetBytes($json)
try {
    $cipher=[Security.Cryptography.ProtectedData]::Protect(
        $plaintext,$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
    [IO.File]::WriteAllBytes($target,$cipher)
} finally { [Array]::Clear($plaintext,0,$plaintext.Length) }
@{name=$ProfileName;slug=$slug;proxyPort=$ProxyPort;protocol='VLESS/TCP/REALITY';encryption='DPAPI CurrentUser';createdAt=[DateTimeOffset]::UtcNow.ToString('O')}|
    ConvertTo-Json|Set-Content -LiteralPath $metadata -Encoding UTF8
[pscustomobject]@{status='IMPORTED';profile=$slug;proxyPort=$ProxyPort;storedEncrypted=$true;validatedByXray=$true;sourceNotModified=$true}|
    ConvertTo-Json -Compress

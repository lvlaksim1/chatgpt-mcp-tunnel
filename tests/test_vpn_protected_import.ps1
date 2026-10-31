$ErrorActionPreference='Stop'
$root='D:\Dev\ChatGPT-MCP-Tunnel'
$bin=Join-Path $root '.tools\vpn-pilot\xray-v26.10.29\xray.exe'
$generated=(& $bin x25519 | Out-String)
$m=[regex]::Match($generated,'Password \(PublicKey\):\s*([A-Za-z0-9_-]{42,44})')
if(!$m.Success){throw 'Xray key generation failed'}
$pk=$m.Groups[1].Value
$uuid=[Guid]::NewGuid().ToString('D')
$folder=Join-Path $root '.tools\vpn-pilot\test-protected'
$source=Join-Path $folder 'synthetic-vless.input'
$profile='synthetic-lab'
$port=11825
New-Item -Path $folder -ItemType Directory -Force|Out-Null
try{
 $url='vless://'+$uuid+'@127.0.0.1:11812?type=tcp&security=reality&encryption=none&pbk='+$pk+'&sni=example.com&fp=chrome&sid=abcdef12&flow=xtls-rprx-vision#synthetic'
 [IO.File]::WriteAllText($source,$url,[Text.UTF8Encoding]::new($false))
 $import=& (Join-Path $root 'scripts\Vpn\Import-VlessReality.ps1') -SourceFile $source -ProfileName $profile -StorageRoot $folder -ProxyPort $port -ProjectRoot $root
 $obj=$import|ConvertFrom-Json
 if($obj.status -ne 'IMPORTED' -or !$obj.validatedByXray){throw 'Import failed validation'}
 Remove-Item $source -Force
 $run=& (Join-Path $root 'scripts\Vpn\Start-ProtectedXrayProfile.ps1') -ProfileName $profile -StorageRoot $folder -ProjectRoot $root
 $p=$run|ConvertFrom-Json
 if($p.status -ne 'STARTED'){throw 'Xray process failed to start'}
 if(!(Get-NetTCPConnection -State Listen -LocalPort $port -OwningProcess $p.pid -ErrorAction SilentlyContinue)){throw 'Protected profile listener missing'}
 if(!(Test-Path (Join-Path $folder ($profile+'.dpapi')))){throw 'DPAPI profile missing'}
 $encrypted=[IO.File]::ReadAllBytes((Join-Path $folder ($profile+'.dpapi')))
 if([Text.Encoding]::UTF8.GetString($encrypted).Contains($pk)){throw 'Protected file contains plaintext public key'}
 Write-Output 'XRAY_PROTECTED_PROFILE_IMPORT_START_STOP_PASS'
}finally{
 & (Join-Path $root 'scripts\Vpn\Stop-ProtectedXrayProfile.ps1') -ProfileName $profile -StorageRoot $folder |Out-Null
 if(Test-Path $folder){Remove-Item -LiteralPath $folder -Force -Recurse}
}

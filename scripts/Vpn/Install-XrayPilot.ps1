param(
 [string]$Root='D:\Dev\ChatGPT-MCP-Tunnel'
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$base=Join-Path $Root '.tools\vpn-pilot'
$dir=Join-Path $base 'xray-v26.10.29'
$zip=Join-Path $base 'Xray-windows-64-v26.10.29.zip'
$expected='fce33e81cccf50e7aab572b175519a50b5e0f9d2f5c8d1ed73012ea5c9ad4d58'
$uri='https://github.com/XTLS/Xray-core/releases/download/v26.10.29/Xray-windows-64.zip'
New-Item -ItemType Directory -Path $base,$dir -Force | Out-Null
if(!(Test-Path $zip)) {
 $temp=$zip+'.part'
 if(Test-Path $temp){Remove-Item -LiteralPath $temp -Force}
 Invoke-WebRequest -Uri $uri -OutFile $temp -UseBasicParsing -TimeoutSec 130
 if((Get-FileHash $temp -Algorithm SHA256).Hash -ine $expected){throw 'Xray official asset SHA256 mismatch'}
 Move-Item -LiteralPath $temp -Destination $zip
}
if((Get-FileHash $zip -Algorithm SHA256).Hash -ine $expected){throw 'Xray cache SHA256 mismatch'}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$entries=[IO.Compression.ZipFile]::OpenRead($zip)
try {
 foreach($entry in $entries.Entries) {
  if($entry.FullName -in @('xray.exe','LICENSE','geoip.dat','geosite.dat')) {
   $dest=Join-Path $dir $entry.Name
   [IO.Compression.ZipFileExtensions]::ExtractToFile($entry,$dest,$true)
  }
 }
}finally {$entries.Dispose()}
$exe=Join-Path $dir 'xray.exe'
if(!(Test-Path $exe)){throw 'Expected Windows executable absent from verified archive'}
$version=(& $exe version | Out-String).Trim()
if($LASTEXITCODE -ne 0){throw 'Xray version command failed'}
$result=[ordered]@{
 installed_at=[DateTimeOffset]::UtcNow.ToString('O')
 source='XTLS/Xray-core official GitHub release v26.10.29'
 uri=$uri
 archive_sha256=$expected
 xray_exe=$exe
 binary_sha256=(Get-FileHash $exe).Hash.ToLowerInvariant()
 version=($version -split '\r?\n')[0]
}
$result|ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $base 'binary-manifest.json') -Encoding UTF8
$result|ConvertTo-Json -Compress

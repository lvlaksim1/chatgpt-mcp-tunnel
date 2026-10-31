[CmdletBinding()]
param(
    [string]$Kit = $PSScriptRoot,
    [string]$InstallDir = ''
)
# Offline integrity check. Uses stock Windows PowerShell 5.1; no Python,
# network, cookies, DPAPI or other credentials are required.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
function Require([bool]$condition,[string]$reason){
    if(-not $condition){throw $reason}
}
if($InstallDir){
    $root=Join-Path ([IO.Path]::GetFullPath($InstallDir)) 'RecoveryKit'
    $indexPath=Join-Path $root 'latest.json'
    Require (Test-Path -LiteralPath $indexPath -PathType Leaf) 'Sidecar index missing'
    $index=Get-Content -LiteralPath $indexPath -Raw -Encoding UTF8|ConvertFrom-Json
    Require ($index.schema -eq 'chatgpt-mcp-tunnel-recovery-sidecar-v1') 'Unknown sidecar index schema'
    Require ($index.latestVersion -match '^(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)$') 'Invalid sidecar version'
    Require ($index.relativePath -eq ('Versions/v'+$index.latestVersion)) 'Sidecar path not trusted'
    $Kit=Join-Path $root ('Versions\v'+$index.latestVersion)
}
$Kit=[IO.Path]::GetFullPath($Kit)
$manifestFile=Join-Path $Kit 'recovery-manifest.json'
Require (Test-Path -LiteralPath $manifestFile -PathType Leaf) 'Recovery manifest missing'
$m=Get-Content -LiteralPath $manifestFile -Raw -Encoding UTF8 |ConvertFrom-Json
Require ($m.schema -eq 'chatgpt-mcp-tunnel-recovery-kit-v1') 'Unknown recovery manifest schema'
Require ($m.containsSecrets -eq $false) 'Credential-free manifest expected'
Require ($m.version -match '^(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)$') 'Invalid recovery version'
Require (@($m.artifacts).Count -ge 3) 'Incomplete artifact list'
$names=@{}
foreach($file in $m.artifacts){
    $rel=[string]$file.path
    Require ($rel -match '^[A-Za-z0-9][A-Za-z0-9._-]*$') 'Unsafe artifact filename'
    Require (-not $names.ContainsKey($rel)) 'Duplicate artifact'
    $names[$rel]=$true
    $entry=Join-Path $Kit $rel
    Require (Test-Path -LiteralPath $entry -PathType Leaf) ("Missing artifact: "+$rel)
    $item=Get-Item -LiteralPath $entry
    Require ($item.Length -eq [long]$file.bytes) ("Artifact length mismatch: "+$rel)
    $hash=(Get-FileHash -LiteralPath $entry -Algorithm SHA256).Hash
    Require ($hash -ieq [string]$file.sha256) ("Artifact SHA256 mismatch: "+$rel)
}
Require ($names.ContainsKey('source-snapshot.zip')) 'Source snapshot missing'
Require (@($names.Keys|Where-Object {$_ -match '\.exe$'}).Count -ge 2) 'Installers missing'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive=[IO.Compression.ZipFile]::OpenRead((Join-Path $Kit 'source-snapshot.zip'))
try{
    $entries=@{}
    foreach($e in $archive.Entries){
        $path=$e.FullName.Replace('\','/')
        Require (-not ($path.StartsWith('/') -or $path.Contains('../') -or $path.Contains(':'))) 'Unsafe ZIP entry'
        $entries[$path]=$true
    }
    foreach($src in @(
        'src/ChatGptMcpTunnel/Program.cs',
        'src/ChatGptMcpTunnel/ChatUiTools.cs',
        'src/ChatGptMcpTunnel/RouterLite.cs',
        'src/ChatGptMcpTunnel/ChatRelayApp.html',
        'src/ChatGptMcpTunnel/ChatLifecycleApp.html',
        'src/ChatGptMcpTunnel/ChatGlobalHubApp.html',
        'src/ChatGptMcpTunnel/ChatBootstrapApp.html',
        'src/ChatGptMcpAgent/ChatGptMcpAgent.csproj',
        'src/ChatGptMcpProxyBridge/ChatGptMcpProxyBridge.csproj',
        'src/ChatGptMcpVoice/ChatGptMcpVoice.csproj',
        'scripts/Recovery/Client-FreeXraySupervisor.ps1',
        'scripts/Recovery/Install-ClientFreeVpnTasks.ps1',
        'scripts/Recovery/publish_recovery_sidecar.py',
        'scripts/Recovery/Verify-RecoveryKit.ps1'
    )){
        Require ($entries.ContainsKey($src)) ('Missing source: '+$src)
    }
}finally{$archive.Dispose()}
if($InstallDir){
    Require ($index.latestVersion -eq $m.version) 'Latest index version mismatch'
    Require ($index.sourceCommit -eq $m.sourceCommit) 'Latest index commit mismatch'
    Require ((Get-FileHash -LiteralPath $manifestFile -Algorithm SHA256).Hash -ieq $index.manifestSha256) 'Latest index digest mismatch'
}
[pscustomobject]@{
    status='PASS'
    kit=$Kit
    version=$m.version
    sourceCommit=$m.sourceCommit
    artifactCount=@($m.artifacts).Count
    credentialFree=$true
    checkedWith='WindowsPowerShell'
}|ConvertTo-Json -Compress

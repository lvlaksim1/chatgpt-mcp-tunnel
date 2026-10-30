param(
  [ValidatePattern('^\d+\.\d+\.\d+$')]
  [string]$Version = '0.2.27',
  [string]$ClientSource = 'D:\ChatGPTMCPAgent\OpenAI',
  [switch]$SkipTests
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = Split-Path -Parent $PSScriptRoot
$out = Join-Path $repo ".tools\local-release\v$Version"
$publish = Join-Path $out 'publish'
$runtime = Join-Path $publish 'Runtime'
$dist = Join-Path $out 'dist'
$innoPaths = @((Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),(Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Inno Setup 6\ISCC.exe'),(Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe'))
$iscc = $innoPaths | Where-Object { Test-Path $_ -PathType Leaf } | Select-Object -First 1
if(-not $iscc) { throw 'Local Inno Setup compiler is missing' }
$client = Join-Path $ClientSource 'tunnel-client.exe'
if(-not (Test-Path $client)) { throw 'Local tunnel-client.exe is missing' }
$clientVersion = & $client --version
if($LASTEXITCODE -ne 0 -or $clientVersion -notmatch '0\.0\.18') { throw "Unexpected tunnel client: $clientVersion" }
$clientHash = (Get-FileHash $client -Algorithm SHA256).Hash
if(Test-Path (Join-Path $publish 'ChatGptMcpAgent.exe')) { throw "Release already built: $out" }
New-Item -Path (Join-Path $publish 'OpenAI'),$runtime,$dist -ItemType Directory -Force | Out-Null
Push-Location $repo
try {
  Write-Output "LOCAL_PUBLISH_AGENT v$Version"
  & dotnet publish src/ChatGptMcpAgent/ChatGptMcpAgent.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false "-p:Version=$Version" -o $publish --nologo
  if($LASTEXITCODE -ne 0){throw 'Agent publish failed'}
  Write-Output 'LOCAL_PUBLISH_RUNTIME'
  & dotnet publish src/ChatGptMcpTunnel/ChatGptMcpTunnel.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:DebugType=None -p:DebugSymbols=false "-p:Version=$Version" -o $runtime --nologo
  if($LASTEXITCODE -ne 0){throw 'Runtime publish failed'}
  foreach($file in @('AutomationWorker.ps1','ChatRelayApp.html','ChatGlobalHubApp.html','ChatLifecycleApp.html','ChatBootstrapApp.html')){
    if(-not (Test-Path (Join-Path $runtime $file))){throw "Missing resource: $file"}
  }
  Copy-Item $client (Join-Path $publish 'OpenAI\tunnel-client.exe')
  $cloudflared=Join-Path $ClientSource 'cloudflared.exe'
  if(Test-Path $cloudflared){Copy-Item $cloudflared (Join-Path $publish 'OpenAI\cloudflared.exe')}
  if((Get-FileHash (Join-Path $publish 'OpenAI\tunnel-client.exe') -Algorithm SHA256).Hash -ne $clientHash){throw 'Bundled client SHA mismatch'}
  if(-not $SkipTests){
    $app=Join-Path $publish 'ChatGptMcpAgent.exe'
    foreach($flag in @('--selftest','--test-control','--test-overlay-auto','--test-overlay-tray','--test-chat-window','--test-chat-sessions','--test-chat-attachments','--test-transfer-window','--test-icons','--test-chat-thread-activator','--test-webview-auth')){
      Write-Output "SELFTEST $flag"
      $process=Start-Process -FilePath $app -ArgumentList $flag -Wait -PassThru
      if($process.ExitCode -ne 0){throw "Packaged selftest $flag failed code $($process.ExitCode)"}
    }
    $env:CHATGPT_MCP_TEST_DLL=Join-Path $runtime 'ChatGptMcpTunnel.dll'
    try{
      foreach($test in @('test_mcp_stdio.py','test_windows_tools.py','test_lifecycle_entrypoints.py','test_uia_worker_persistent.py','test_chat_ui_mcp.py','test_multichat_routing.py','test_webview_auth_only.py','test_utf8_filesystem.py','test_v0218_stable_relay.py')){
        Write-Output "TEST $test"
        & python (Join-Path $repo "tests\$test")
        if($LASTEXITCODE -ne 0){throw "MCP test failed: $test"}
      }
    }finally{Remove-Item Env:CHATGPT_MCP_TEST_DLL -ErrorAction SilentlyContinue}
    foreach($test in @('test_chat_ui_bridge.js','test_chat_relay_resilience.js','test_chat_route_modes.js','test_multichat_widget.js')){
      Write-Output "NODE_TEST $test"
      & node (Join-Path $repo "tests\$test")
      if($LASTEXITCODE -ne 0){throw "Node test failed: $test"}
    }
  }
  foreach($kind in @('full','update')){
    Write-Output "INNO $kind"
    & $iscc (Join-Path $repo "installer\agent-$kind.iss") "/DAppVersion=$Version" "/DPublishDir=$publish" "/DOutputDir=$dist"
    if($LASTEXITCODE -ne 0){throw "Inno compilation failed: $kind"}
  }
  $full=Join-Path $dist "ChatGPT_MCP_Tunnel_Setup_v$Version.exe"
  $update=Join-Path $dist "ChatGPT_MCP_Tunnel_Update_v$Version.exe"
  $legacy=Join-Path $dist "ChatGPT_MCP_Agent_Update_v$Version.exe"
  foreach($file in @($full,$update)){if(-not (Test-Path $file) -or (Get-Item $file).Length -lt 1000000){throw "Installer missing: $file"}}
  Copy-Item $update $legacy
  $manifest=@($full,$update,$legacy)|ForEach-Object{ $x=Get-Item $_;[pscustomobject]@{Name=$x.Name;Length=$x.Length;SHA256=(Get-FileHash $_ -Algorithm SHA256).Hash.ToLowerInvariant()}}
  $manifest|ConvertTo-Json -Depth 4|Set-Content (Join-Path $dist "SHA256-v$Version.json") -Encoding UTF8
  Write-Output 'LOCAL_RELEASE_BUILD_OK'
  Write-Output "DIST=$dist"
  $manifest|Format-Table -AutoSize|Out-String|Write-Output
}
finally{Pop-Location}

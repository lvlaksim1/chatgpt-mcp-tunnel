param(
  [ValidatePattern('^\d+\.\d+\.\d+$')]
  [Parameter(Mandatory=$true)]
  [string]$Version,
  [string]$ClientSource = 'D:\ChatGPTMCPAgent\OpenAI',
  [string]$RecoveryInstallDir = '',
  [switch]$SkipTests
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = Split-Path -Parent $PSScriptRoot
# Mandatory fail-closed RecoveryKit release gate: exact committed source.
$clean = & git -C $repo status --porcelain
if($LASTEXITCODE -ne 0 -or $clean) { throw 'Commit source changes before release build: Recovery Kit requires immutable HEAD' }
$head = (& git -C $repo rev-parse HEAD).Trim()
if($LASTEXITCODE -ne 0 -or $head -notmatch '^[0-9a-f]{40}$') { throw 'Cannot establish source commit' }
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
  Write-Output 'LOCAL_PUBLISH_SERVER_PROXY_BRIDGE'
  & dotnet publish src/ChatGptMcpProxyBridge/ChatGptMcpProxyBridge.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false "-p:Version=$Version" -o $publish --nologo
  if($LASTEXITCODE -ne 0){throw 'Server proxy bridge publish failed'}
  $bridgeExe = Join-Path $publish 'ChatGptMcpProxyBridge.exe'
  if(!(Test-Path $bridgeExe)){throw 'Server proxy bridge missing'}
  $bridgeTest=Start-Process -FilePath $bridgeExe -ArgumentList '--selftest' -Wait -PassThru
  if($bridgeTest.ExitCode -ne 0){throw 'Server proxy bridge selftest failed'}

  # Bundled free VLESS/REALITY mode: offline verified Xray-core binary,
  # vendor license and pinned SHA256. Never depend on a global Happ install.
  $xrayManifest=Join-Path $repo '.tools\vpn-pilot\binary-manifest.json'
  if(!(Test-Path $xrayManifest)){throw 'Verified Xray pilot required for VPN-enabled release'}
  $xrayInfo=Get-Content $xrayManifest -Raw|ConvertFrom-Json
  if(!(Test-Path $xrayInfo.xray_exe) -or
     (Get-FileHash $xrayInfo.xray_exe -Algorithm SHA256).Hash -ine $xrayInfo.binary_sha256){
     throw 'Bundled Xray SHA256 verification failed'
  }
  $xrayOut=Join-Path $publish 'Vpn'
  New-Item -Path $xrayOut -ItemType Directory -Force|Out-Null
  Copy-Item $xrayInfo.xray_exe (Join-Path $xrayOut 'xray.exe') -Force
  Copy-Item (Join-Path (Split-Path $xrayInfo.xray_exe -Parent) 'LICENSE') (Join-Path $xrayOut 'LICENSE.Xray.txt') -Force
  Set-Content -LiteralPath (Join-Path $xrayOut 'xray.sha256') -Value $xrayInfo.binary_sha256 -Encoding ascii
  if((Get-FileHash (Join-Path $xrayOut 'xray.exe') -Algorithm SHA256).Hash -ine $xrayInfo.binary_sha256){throw 'Packaged Xray SHA256 mismatch'}

  Write-Output 'LOCAL_PUBLISH_VOICE'
  $voice = Join-Path $publish 'Voice'
  & dotnet publish src/ChatGptMcpVoice/ChatGptMcpVoice.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false "-p:Version=$Version" -o $voice --nologo
  if($LASTEXITCODE -ne 0){throw 'Voice worker publish failed'}
  $modelsRoot=Join-Path $repo '.tools\voice-models'
  $models=Join-Path $voice 'Models'
  $asr=Join-Path $models 'sherpa-onnx-zipformer-ru-2024-09-18'
  $tts=Join-Path $models 'vits-piper-ru_RU-irina-medium'
  New-Item -ItemType Directory -Path $asr,$tts -Force | Out-Null
  foreach($file in @('encoder.int8.onnx','decoder.int8.onnx','joiner.int8.onnx','tokens.txt')){
    Copy-Item (Join-Path $modelsRoot "sherpa-onnx-zipformer-ru-2024-09-18\$file") (Join-Path $asr $file) -Force
  }
  foreach($file in @('ru_RU-irina-medium.onnx','tokens.txt')){
    Copy-Item (Join-Path $modelsRoot "vits-piper-ru_RU-irina-medium\$file") (Join-Path $tts $file) -Force
  }
  Copy-Item (Join-Path $modelsRoot 'vits-piper-ru_RU-irina-medium\espeak-ng-data') (Join-Path $tts 'espeak-ng-data') -Recurse -Force
  Copy-Item (Join-Path $modelsRoot 'silero_vad.onnx') (Join-Path $models 'silero_vad.onnx') -Force
  $voiceExe=Join-Path $voice 'ChatGptMcpVoice.exe'
  $voiceSelf=Start-Process $voiceExe -ArgumentList '--selftest' -Wait -PassThru
  if($voiceSelf.ExitCode -ne 0){throw 'Voice worker basic selftest failed'}
  $voiceModels=Start-Process $voiceExe -ArgumentList '--check-models' -Wait -PassThru
  if($voiceModels.ExitCode -ne 0){throw 'Voice worker packaged models missing'}
  Write-Output 'LOCAL_PUBLISH_RUNTIME'
  & dotnet publish src/ChatGptMcpTunnel/ChatGptMcpTunnel.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:DebugType=None -p:DebugSymbols=false "-p:Version=$Version" -o $runtime --nologo
  if($LASTEXITCODE -ne 0){throw 'Runtime publish failed'}
  foreach($file in @('AutomationWorker.ps1','ChatRelayApp.html','ChatGlobalHubApp.html','ChatLifecycleApp.html','ChatBootstrapApp.html')){
    if(-not (Test-Path (Join-Path $runtime $file))){throw "Missing resource: $file"}
  }
  # Browser engine is WebView2 in the agent. Keep the legacy JS only in Git for rollback.
  & node (Join-Path $repo 'src\ChatGptMcpAgent\BrowserChatBridge.js') --selftest
  if($LASTEXITCODE -ne 0) { throw 'Legacy bridge regression test failed' }
  Copy-Item $client (Join-Path $publish 'OpenAI\tunnel-client.exe')
  $cloudflared=Join-Path $ClientSource 'cloudflared.exe'
  if(Test-Path $cloudflared){Copy-Item $cloudflared (Join-Path $publish 'OpenAI\cloudflared.exe')}
  if((Get-FileHash (Join-Path $publish 'OpenAI\tunnel-client.exe') -Algorithm SHA256).Hash -ne $clientHash){throw 'Bundled client SHA mismatch'}
  if(-not $SkipTests){
    $runtimeExe=Join-Path $runtime 'ChatGptMcpTunnel.exe'
    Write-Output 'SELFTEST --test-uac-shell (no elevation requested)'
    $uacTest=Start-Process -FilePath $runtimeExe -ArgumentList '--test-uac-shell' -Wait -PassThru
    if($uacTest.ExitCode -ne 0){throw "UAC shell local IPC selftest failed: $($uacTest.ExitCode)"}
    $brokerClientTest=Start-Process -FilePath $runtimeExe -ArgumentList '--test-admin-broker-client' -Wait -PassThru
    if($brokerClientTest.ExitCode -ne 0){throw "Broker client test failed code $($brokerClientTest.ExitCode)"}
    $app=Join-Path $publish 'ChatGptMcpAgent.exe'
    foreach($flag in @('--selftest','--test-control','--test-overlay-auto','--test-overlay-tray','--test-transfer-window','--test-icons','--test-chat-thread-activator','--test-toolbar-dom','--test-voice-dom','--test-main-chat-ui','--test-main-chat-live','--test-admin-service','--test-admin-pipe-exec','--test-webview-network','--test-theme')){
      Write-Output "SELFTEST $flag"
      $process=Start-Process -FilePath $app -ArgumentList $flag -Wait -PassThru
      if($process.ExitCode -ne 0){throw "Packaged selftest $flag failed code $($process.ExitCode)"}
    }
    $env:CHATGPT_MCP_TEST_DLL=Join-Path $runtime 'ChatGptMcpTunnel.dll'
    try{
      foreach($test in @('test_mcp_stdio.py','test_windows_tools.py','test_lifecycle_entrypoints.py','test_uia_worker_persistent.py','test_chat_ui_mcp.py','test_multichat_routing.py','test_primary_webview_ui.py','test_utf8_filesystem.py','test_v0218_stable_relay.py')){
        Write-Output "TEST $test"
        & python (Join-Path $repo "tests\$test")
        if($LASTEXITCODE -ne 0){throw "MCP test failed: $test"}
      }
    }finally{Remove-Item Env:CHATGPT_MCP_TEST_DLL -ErrorAction SilentlyContinue}
    foreach($test in @('test_chat_ui_bridge.js','test_chat_relay_resilience.js','test_chat_route_modes.js','test_multichat_widget.js','test_browser_chat_bridge.js','test_browser_heartbeat.js','test_browser_transport_v0243.js','test_browser_recovery_bindings_v0247.js')){
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
  # RecoveryKit is a mandatory sidecar, NOT packed into its own installer.
  # Publishing uses a copy-verify-promote sequence that retains older versions.
  $kitBuilder=Join-Path $repo 'scripts\Recovery\build_recovery_kit.py'
  $sidecarPublisher=Join-Path $repo 'scripts\Recovery\publish_recovery_sidecar.py'
  $kit=Join-Path $dist 'RecoveryKit'
  Write-Output "RECOVERY_KIT_BUILD v$Version"
  & python $kitBuilder build --repo $repo --dist $dist --kit $kit --version $Version
  if($LASTEXITCODE -ne 0){throw 'Release aborted: mandatory Recovery Kit generation failed'}
  & python $kitBuilder verify --kit $kit
  if($LASTEXITCODE -ne 0){throw 'Release aborted: mandatory Recovery Kit verification failed'}
  & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $kit 'Verify-RecoveryKit.ps1') -Kit $kit
  if($LASTEXITCODE -ne 0){throw 'Release aborted: standalone Windows Recovery Kit verification failed'}
  $actualInstallDir=$RecoveryInstallDir
  if(-not $actualInstallDir){
    $installed=Get-ItemProperty 'HKCU:\Software\ChatGPTMCPAgent' -ErrorAction SilentlyContinue
    if($installed -and $installed.InstallDir){$actualInstallDir=[string]$installed.InstallDir}
  }
  if($actualInstallDir -and (Test-Path -LiteralPath (Join-Path $actualInstallDir 'ChatGptMcpAgent.exe') -PathType Leaf)){
    Write-Output "RECOVERY_SIDECAR_PUBLISH v$Version"
    & python $sidecarPublisher publish --kit $kit --install-dir $actualInstallDir
    if($LASTEXITCODE -ne 0){throw 'Release aborted: sidecar publication failed'}
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $kit 'Verify-RecoveryKit.ps1') -InstallDir $actualInstallDir
    if($LASTEXITCODE -ne 0){throw 'Release aborted: published sidecar integrity check failed'}
  }else{
    Write-Output 'RECOVERY_SIDECAR_DIST_ONLY: no installed app found; Kit bundled with dist'
  }
  Write-Output "RELEASE_SOURCE_COMMIT=$head"
  Write-Output 'RECOVERY_KIT_BUILD_OK'
  Write-Output 'LOCAL_RELEASE_BUILD_OK'
  Write-Output "DIST=$dist"
  $manifest|Format-Table -AutoSize|Out-String|Write-Output
}
finally{Pop-Location}

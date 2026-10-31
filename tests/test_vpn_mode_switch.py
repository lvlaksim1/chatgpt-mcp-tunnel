from pathlib import Path
root=Path(__file__).resolve().parents[1]
c=(root/"src/ChatGptMcpAgent/VpnModeController.cs").read_text(encoding="utf-8")
form=(root/"src/ChatGptMcpAgent/ChatGptMainForm.cs").read_text(encoding="utf-8")
switch=(root/"src/ChatGptMcpAgent/ChatGptMainForm.Vpn.cs").read_text(encoding="utf-8")
tray=(root/"src/ChatGptMcpAgent/Program.cs").read_text(encoding="utf-8")
chrome=(root/"src/ChatGptMcpAgent/Themes/BrowserChrome.html").read_text(encoding="utf-8")
builder=(root/"scripts/Build-LocalRelease.ps1").read_text(encoding="utf-8-sig")
assert 'enum VpnConnectionMode { Happ, FreeReality }' in c
assert 'Current == VpnConnectionMode.Happ' in c
assert 'WebViewNetworkProxy.BrowserArguments()' in c
assert 'VpnModeController.BrowserArguments()' in form
assert 'VpnModeController.BrowserProfileFolder(ProfileFolder)' in form
assert 'BrowserProfileFolder(string existingProfile)' in c
assert 'WebView2-FreeReality' not in c
assert 'existingProfile' in c
assert 'PortOpen(LocalProxyPort)' in c and 'ProbeFreeViaBridgeAsync()' in c
assert 'RecreateBrowserAfterVpnChangeAsync' in tray and 'chat.ExitApplication()' in tray
assert 'VpnModeChanged?.Invoke' in switch
assert 'ProbeFreeViaBridgeAsync' in switch
assert 'DialogResult.Yes' in switch
assert 'vpn-switch' in form and 'networkMode = ' in form
assert 'id="vpn-switch"' in chrome and 'id="vpn-use-happ"' in chrome and 'id="vpn-use-free"' in chrome
assert "send('vpn-switch',{mode})" in chrome
assert 'Copy-Item $xrayInfo.xray_exe' in builder and 'Bundled Xray SHA256 verification failed' in builder
assert '<Version>0.2.65</Version>' in (root/"src/ChatGptMcpAgent/ChatGptMcpAgent.csproj").read_text(encoding="utf-8")
print("VPN_MODE_SWITCH_SOURCE_REGRESSION_OK")

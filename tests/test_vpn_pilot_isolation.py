from pathlib import Path
root=Path(__file__).resolve().parents[1]
scripts=root/"scripts/Vpn"
for f in ("Install-XrayPilot.ps1","Start-XrayPilot.ps1","Stop-XrayPilot.ps1","Test-XrayVlessLoopback.ps1"):
    assert (scripts/f).is_file(), f
launcher=(root/"installer/ServerStart.ps1").read_text(encoding="utf-8-sig")
webview=(root/"src/ChatGptMcpAgent/WebViewNetworkProxy.cs").read_text(encoding="utf-8")
app=(root/"src/ChatGptMcpAgent/Program.cs").read_text(encoding="utf-8")
start=(scripts/"Start-XrayPilot.ps1").read_text(encoding="utf-8-sig")
stop=(scripts/"Stop-XrayPilot.ps1").read_text(encoding="utf-8-sig")
assert "CHATGPT_MCP_TUNNEL_PROXY_PORT" in launcher
assert "CHATGPT_MCP_WEBVIEW_PROXY_PORT" in webview
assert "127.0.0.1" in webview and "127.0.0.1" in launcher
assert "PortOpen(port)" in webview
assert "direct" in start and "via-happ" in start
assert "11809" in start and "10809" in webview
assert "ExecutablePath" in stop and "Stop-Process -Id $o.pid" in stop
assert "--test-vpn-webview" in app
assert (root/"src/ChatGptMcpAgent/VpnWebViewPilotTests.cs").is_file()
assert "Get-NetTCPConnection" in start
assert "binary_sha256" in start
assert "New-NetRoute" not in start and "Set-NetIPInterface" not in start
assert "Set-ItemProperty" not in start
print("XRAY_PILOT_ISOLATION_REGRESSION_OK")

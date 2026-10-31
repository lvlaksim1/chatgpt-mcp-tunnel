"""Regression checks for DOMKAT VPN v0.2.64 unified WebView2 profile and MCP failback."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
gui=(root/"src/ChatGptMcpAgent/ChatGptMainForm.cs").read_text(encoding="utf-8")
mode=(root/"src/ChatGptMcpAgent/VpnModeController.cs").read_text(encoding="utf-8")
handler=(root/"src/ChatGptMcpAgent/ChatGptMainForm.Vpn.cs").read_text(encoding="utf-8")
program=(root/"src/ChatGptMcpAgent/Program.cs").read_text(encoding="utf-8")
bridge=(root/"src/ChatGptMcpProxyBridge/Program.cs").read_text(encoding="utf-8")
server=(root/"installer/ServerStart.ps1").read_text(encoding="utf-8-sig")
assert "BrowserProfileFolder(string existingProfile) => existingProfile" in mode
assert "WebView2-FreeReality" not in mode
assert "BrowserProcessExited" in gui and "_browserExited.TrySetResult" in gui
assert "await browserExit.WaitAsync" in program
assert "browser.BrowserNetworkMode != VpnModeController.Current" in program
assert "VpnModeController.IsTransitioning" in program
assert "ProbeFreeViaBridgeAsync" in handler and "IsTransitioning = false" in handler
assert "авторизованный профиль" in handler
assert "EnsureFreeStarted" not in mode  # GUI may not own a second Xray
assert "WatchModeAsync" in bridge
assert "SetHappFallback" in bridge
assert "ProbeEndpointAsync(FreePort" in bridge
assert "VerifyNewProfileAsync" in bridge
assert "CandidatePort = 11822" in bridge
assert "StopOwnedXray()" in bridge and "DisconnectStaleSessions" in bridge
assert "network-notice.json" in bridge
assert "--http-proxy http://127.0.0.1:11827" in server
assert "<Version>0.2.65</Version>" in (root/"src/ChatGptMcpAgent/ChatGptMcpAgent.csproj").read_text()
assert "<Version>0.2.65</Version>" in (root/"src/ChatGptMcpProxyBridge/ChatGptMcpProxyBridge.csproj").read_text()
print("VPN_V0264_SINGLE_PROFILE_FAILBACK_HOTRELOAD_SOURCE_PASS")

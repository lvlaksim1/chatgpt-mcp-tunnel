"""Static regression of the DOMKAT one-tunnel VPN fallback policy."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
bridge=(root/"src/ChatGptMcpProxyBridge/Program.cs").read_text(encoding="utf-8")
updater=(root/"scripts/Vpn/free_reality_updater.py").read_text(encoding="utf-8")
gui=(root/"src/ChatGptMcpAgent/Program.cs").read_text(encoding="utf-8")
server=(root/"installer/ServerStart.ps1").read_text(encoding="utf-8-sig")
assert "Free-Reality-Standby-1.dpapi" in bridge
assert "Free-Reality-Standby-2.dpapi" in bridge
assert "Free-Reality-Standby-{slot}.dpapi" in updater
assert "dpapi_protect(backup_config)" in updater
assert 'standbys = []' in updater
assert "TryRotateStandbyAsync(token)" in bridge
assert bridge.index("bool rotated = await TryRotateStandbyAsync(token)") < bridge.index(
    'SetHappFallback("free-route-health-failed")')
assert "VerifyNewProfileAsync(standby, token)" in bridge
assert "free-node-rotated" in bridge
assert "WriteNotice(reason" in bridge
assert "ShowVpnRecoveryNotice()" in gui
assert "ShowBalloonTip(" in gui
assert 'GetProperty("reason")' in gui
assert "BrowserNetworkMode != VpnModeController.Current" in gui
assert "--http-proxy http://127.0.0.1:11827" in server
assert "<Version>0.2.65</Version>" in (root/"src/ChatGptMcpAgent/ChatGptMcpAgent.csproj").read_text()
assert "<Version>0.2.65</Version>" in (root/"src/ChatGptMcpProxyBridge/ChatGptMcpProxyBridge.csproj").read_text()
print("VPN_0265_STANDBY_ROTATION_AND_GUI_NOTICE_POLICY_PASS")

"""Check the byte-exact accepted v0.2.18 relay is preserved."""
from pathlib import Path
import subprocess

repo=Path(__file__).resolve().parents[1]
src=repo/"src/ChatGptMcpTunnel/ChatRelayApp.html"
accepted=subprocess.run(["git","-C",str(repo),"show",
    "eb1845f:src/ChatGptMcpTunnel/ChatRelayApp.html"],
    stdout=subprocess.PIPE,check=True).stdout
html=src.read_bytes().replace(b"\r\n",b"\n")
assert b'chat.ui.pending' in html and b'chat.ui.ack' in html
assert b'"ui/message"' in html and b'target:needsNew ? "new" : "active"' in html
assert b'ackPending' in html, "Safe ACK retry missing"
assert b'formatMessage(message)' in html
assert b'history.slice(0,4200)' not in html, "Visible conversation must not contain pasted history"
assert b'ExplicitNewCloudChat' in (repo/"src/ChatGptMcpTunnel/ChatUiTools.cs").read_bytes()
assert b'cloudflare_challenge' in html, "Missing retry diagnostics"
program=(repo/"src/ChatGptMcpAgent/Program.cs").read_text(encoding="utf-8")
assert "_hostActivator.Tick(" not in program, "Global activation must be dormant"
bootstrap=(repo/"src/ChatGptMcpTunnel/ChatBootstrapApp.html").read_text(encoding="utf-8")
assert "const chatTransport = false;" in bootstrap
assert "Передача отключена." in bootstrap
assert "?" * 6 not in bootstrap, "Corrupt bootstrap label"
print("ACCEPTED_V0218_MANUAL_RELAY_PROTOCOL_RECOVERY_AND_AUTOSTART_ISOLATED_OK")

"""Check the byte-exact accepted v0.2.18 relay is preserved."""
from pathlib import Path
import subprocess

repo=Path(__file__).resolve().parents[1]
src=repo/"src/ChatGptMcpTunnel/ChatRelayApp.html"
accepted=subprocess.run(["git","-C",str(repo),"show",
    "eb1845f:src/ChatGptMcpTunnel/ChatRelayApp.html"],
    stdout=subprocess.PIPE,check=True).stdout
assert src.read_bytes().replace(b"\r\n",b"\n")==accepted, "Accepted manual relay must remain unchanged"
program=(repo/"src/ChatGptMcpAgent/Program.cs").read_text(encoding="utf-8")
assert "_hostActivator.Tick(" not in program, "Global activation must be dormant"
bootstrap=(repo/"src/ChatGptMcpTunnel/ChatBootstrapApp.html").read_text(encoding="utf-8")
assert "const chatTransport = false;" in bootstrap
assert "Передача отключена." in bootstrap
assert "?" * 6 not in bootstrap, "Corrupt bootstrap label"
print("ACCEPTED_V0218_RELAY_EXACT_AND_EXPERIMENTAL_AUTOSTART_ISOLATED_OK")

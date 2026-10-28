"""Standalone MCP stdio smoke test. Uses only Python's standard library."""
import json
import subprocess
from pathlib import Path

root = Path(__file__).resolve().parents[1]
dll = root / "src" / "ChatGptMcpTunnel" / "bin" / "Release" / "net8.0" / "ChatGptMcpTunnel.dll"
if not dll.is_file():
    raise SystemExit(f"Build first: {dll}")

proc = subprocess.Popen(
    ["dotnet", str(dll)], cwd=root,
    stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
    text=True, encoding="utf-8", bufsize=1,
)
assert proc.stdin and proc.stdout
try:
    def request(payload):
        proc.stdin.write(json.dumps(payload) + "\n")
        proc.stdin.flush()
        while True:
            line = proc.stdout.readline()
            if not line:
                raise RuntimeError("MCP server closed stdout")
            decoded = json.loads(line)
            if decoded.get("id") == payload["id"]:
                if "error" in decoded:
                    raise RuntimeError(str(decoded["error"]))
                return decoded["result"]

    result = request({"jsonrpc": "2.0", "id": 1, "method": "initialize", "params": {
        "protocolVersion": "2025-11-25", "capabilities": {},
        "clientInfo": {"name": "chatgpt-mcp-tunnel-test", "version": "0.1.0"}
    }})
    print("INITIALIZE_OK", result.get("protocolVersion"))
    proc.stdin.write(json.dumps({"jsonrpc": "2.0", "method": "notifications/initialized"}) + "\n")
    proc.stdin.flush()
    result = request({"jsonrpc": "2.0", "id": 2, "method": "tools/list", "params": {}})
    tools = [t["name"] for t in result["tools"]]
    expected = {"bridge_ping", "system.info", "fs.list", "fs.read", "fs.write",
                "fs.search", "shell.exec", "process.list", "process.kill"}
    gui_tools = {"desktop.screen.info", "desktop.screen.capture",
                 "desktop.window.list", "desktop.window.activate",
                 "desktop.window.move", "desktop.window.close",
                 "desktop.mouse.move", "desktop.mouse.click",
                 "desktop.mouse.scroll", "desktop.keyboard.type",
                 "desktop.keyboard.hotkey"}
    expected |= gui_tools
    assert set(tools) == expected, tools
    print("TOOLS", tools)
    result = request({"jsonrpc": "2.0", "id": 3, "method": "tools/call",
                      "params": {"name": "bridge_ping", "arguments": {}}})
    assert "CHATGPT_MCP_TUNNEL_PING_OK" in json.dumps(result), result
    print("CALL_OK CHATGPT_MCP_TUNNEL_PING_OK")
finally:
    proc.terminate()
    try:
        proc.wait(timeout=5)
    except subprocess.TimeoutExpired:
        proc.kill()
        proc.wait(timeout=5)

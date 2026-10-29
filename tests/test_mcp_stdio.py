"""Standalone MCP stdio smoke test. Uses only Python's standard library."""
import json
import os
import subprocess
from pathlib import Path

root = Path(__file__).resolve().parents[1]
dll = Path(os.environ.get("CHATGPT_MCP_TEST_DLL", str(root / "src" / "ChatGptMcpTunnel" / "bin" / "Release" / "net8.0" / "ChatGptMcpTunnel.dll")))
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
    uia_tools = {"desktop.uia.tree", "desktop.uia.find", "desktop.uia.get", "desktop.uia.action"}
    expected |= uia_tools
    github_tools = {"github.auth", "github.api", "github.graphql", "github.repositories", "github.files", "github.branches", "github.commits", "github.batch_commit", "github.issues", "github.pulls", "github.actions", "github.jobs", "github.releases", "github.release_assets", "github.artifacts", "github.search", "github.git"}
    expected |= github_tools
    expected.update({"chat.receive", "chat.reply"})
    expected.add("computers.list")
    expected.update({"file.transfer.start", "file.transfer.status", "file.transfer.cancel"})
    assert len(github_tools) == 17
    assert set(tools) == expected, tools
    # Logical category alias: keep existing tool IDs, add Russian user-facing titles
    # and descriptions so "используй мультимедиа" selects this family of 11 tools.
    for tool in result["tools"]:
        if tool["name"] in gui_tools or tool["name"] in uia_tools:
            assert tool.get("title", "").startswith("Мультимедиа · "), tool
            assert "Категория «Мультимедиа»" in tool.get("description", ""), tool
        else:
            assert not tool.get("title", "").startswith("Мультимедиа · "), tool
    print("MULTIMEDIA_CATEGORY_OK", len(gui_tools | uia_tools))
    for tool in result["tools"]:
        if tool["name"] in github_tools:
            assert tool.get("title","").startswith("GitHub · "), tool
            assert "Категория «GitHub»" in tool.get("description",""), tool
    print("GITHUB_CATEGORY_OK",len(github_tools))
    print("TOOLS", tools)
    result = request({"jsonrpc": "2.0", "id": 3, "method": "tools/call",
                      "params": {"name": "bridge_ping", "arguments": {}}})
    assert "CHATGPT_MCP_TUNNEL_PING_OK" in json.dumps(result), result
    print("CALL_OK CHATGPT_MCP_TUNNEL_PING_OK")

    # Test the full chat message lifecycle; do not alter existing user messages.
    import tempfile
    import uuid
    from datetime import datetime, timezone
    folder = Path(os.environ.get("LOCALAPPDATA", str(Path.home()))) / "ChatGPTMCPAgent" / "Chat" / "messages"
    folder.mkdir(parents=True, exist_ok=True)
    message_id = uuid.uuid4().hex
    message_file = folder / f"{message_id}.json"
    message_file.write_text(json.dumps({
        "Id": message_id, "CreatedUtc": datetime.now(timezone.utc).isoformat(),
        "Text": "CHAT_E2E_TEST", "Status": "pending",
        "Reply": None, "RepliedUtc": None
    }), encoding="utf-8")
    try:
        received = request({"jsonrpc": "2.0", "id": 4, "method": "tools/call",
            "params": {"name": "chat.receive", "arguments": {"wait_seconds": 0, "max_messages": 30}}})
        assert message_id in json.dumps(received), received
        replied = request({"jsonrpc": "2.0", "id": 5, "method": "tools/call",
            "params": {"name": "chat.reply", "arguments": {
                "message_id": message_id, "reply": "CHAT_E2E_REPLY"}}})
        assert any('"ok":true' in c.get("text", "") for c in replied.get("content", [])), replied
        content = json.loads(message_file.read_text(encoding="utf-8"))
        assert content["Status"] == "answered" and content["Reply"] == "CHAT_E2E_REPLY", content
        print("CHAT_RECEIVE_REPLY_OK")
    finally:
        message_file.unlink(missing_ok=True)

finally:
    proc.terminate()
    try:
        proc.wait(timeout=5)
    except subprocess.TimeoutExpired:
        proc.kill()
        proc.wait(timeout=5)

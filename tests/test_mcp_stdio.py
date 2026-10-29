"""Standalone MCP stdio smoke test. Uses only Python's standard library."""
import json
import os
import subprocess
from pathlib import Path

root = Path(__file__).resolve().parents[1]
dll = Path(os.environ.get("CHATGPT_MCP_TEST_DLL", str(root / "src" / "ChatGptMcpTunnel" / "bin" / "Release" / "net8.0" / "ChatGptMcpTunnel.dll")))
if not dll.is_file():
    raise SystemExit(f"Build first: {dll}")

import tempfile
import shutil
cycle_sandbox = Path(tempfile.mkdtemp(prefix="chatgpt-mcp-cycle-test-"))
proc_env = os.environ.copy()
proc_env["CHATGPT_MCP_CYCLE_TEST_CHAT_ROOT"] = str(cycle_sandbox)

proc = subprocess.Popen(
    ["dotnet", str(dll)], cwd=root, env=proc_env,
    stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
    text=True, encoding="utf-8", errors="replace", bufsize=1,
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
    expected.update({"chat.receive", "chat.reply", "chat.cycle", "chat.attachment.read", "chat.ui.open", "chat.ui.pending", "chat.ui.ack"})
    expected.update({"chat.hub.open","chat.thread.open","chat.lifecycle.event","chat.lifecycle.status"})
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
        # Five-minute protocol: hello -> step -> status; early completion is rejected.
        def cycle(number, **kwargs):
            data = request({"jsonrpc": "2.0", "id": number, "method": "tools/call",
                "params": {"name": "chat.cycle", "arguments": kwargs}})
            return json.loads(data["content"][0]["text"])

        hello = cycle(30, action="hello", signal="я тут")
        assert hello["ok"] and hello["duration_seconds"] == 300, hello
        keys = {"session_id": hello["session_id"], "task_id": hello["task_id"]}
        tick = cycle(31, action="step", wait_seconds=0, **keys)
        assert tick["cycle"]["ok"] and tick["cycle"]["seconds_remaining"] > 0, tick
        assert tick["cycle"]["sample"]["ProcessCount"] > 0, tick
        status = cycle(32, action="status")
        assert status["online"] and status["cycle_number"] == 1, status
        early = cycle(33, action="complete", report="Not yet", **keys)
        assert early["error"] == "cycle_not_finished", early
        print("CHAT_CYCLE_START_STEP_STATUS_EARLY_COMPLETE_OK")

        # Fast-forward only the isolated sandbox and verify report + next task.
        cycle_status = cycle_sandbox / "cycle-status.json"
        saved = json.loads(cycle_status.read_text(encoding="utf-8"))
        from datetime import timedelta
        saved["ExpiresUtc"] = (datetime.now(timezone.utc) - timedelta(seconds=1)).isoformat()
        cycle_status.write_text(json.dumps(saved), encoding="utf-8")
        finished = cycle(34, action="complete",
            report="MCP diagnostic cycle finished.", **keys)
        assert finished["ok"] and finished["completed_cycle_number"] == 1, finished
        assert finished["next_task"]["cycle_number"] == 2, finished
        reported = list((cycle_sandbox / "messages").glob("*.json"))
        assert len(reported) == 1, reported
        message = json.loads(reported[0].read_text(encoding="utf-8"))
        assert message["Source"] == "system" and message["Reply"] == "MCP diagnostic cycle finished.", message
        new_keys = {"session_id": finished["next_task"]["session_id"],
                    "task_id": finished["next_task"]["task_id"]}
        stale = cycle(35, action="step", wait_seconds=0, **keys)
        assert stale["error"] == "stale_session_or_task", stale
        fresh = cycle(36, action="step", wait_seconds=0, **new_keys)
        assert fresh["cycle"]["ok"], fresh
        print("CHAT_CYCLE_REPORT_AND_NEXT_TASK_OK")

        saved = json.loads(cycle_status.read_text(encoding="utf-8"))
        saved["LastSeenUtc"] = (datetime.now(timezone.utc) - timedelta(seconds=125)).isoformat()
        cycle_status.write_text(json.dumps(saved), encoding="utf-8")
        expired = cycle(37, action="status")
        assert expired["state"] == "offline" and not expired["online"], expired
        resumed = cycle(38, action="hello", signal="я тут")
        assert resumed["ok"] and resumed["session_id"] != hello["session_id"], resumed
        print("CHAT_CYCLE_OFFLINE_AND_HOURLY_RECOVERY_OK")


    finally:
        message_file.unlink(missing_ok=True)

finally:
    proc.terminate()
    try:
        proc.wait(timeout=5)
    except subprocess.TimeoutExpired:
        proc.kill()
        proc.wait(timeout=5)
    shutil.rmtree(cycle_sandbox, ignore_errors=True)

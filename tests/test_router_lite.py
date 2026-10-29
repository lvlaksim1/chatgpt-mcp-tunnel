"""Router Lite E2E: one MCP stdio router, one local WebSocket agent.
Uses only the Python standard library; does not alter the live tunnel or working documents.
"""
import json
import base64
import ast
import tempfile
import os
from pathlib import Path
import secrets
import socket
import subprocess
import time

root = Path(__file__).resolve().parents[1]
dll = Path(os.environ.get("CHATGPT_MCP_TEST_DLL", str(root / "src/ChatGptMcpTunnel/bin/Release/net8.0/ChatGptMcpTunnel.dll")))
assert dll.is_file(), dll
probe = socket.socket()
probe.bind(("127.0.0.1", 0))
port = probe.getsockname()[1]
probe.close()
token = secrets.token_urlsafe(32)
base_env = {**os.environ, "CHATGPT_MCP_AGENT_TOKEN": token}
router_env = {**base_env, "CHATGPT_MCP_ROUTER_ENABLED": "1",
              "CHATGPT_MCP_ROUTER_LISTEN": f"http://127.0.0.1:{port}"}
agent_env = {**base_env, "CHATGPT_MCP_COMPUTER_ID": "TEST-AGENT",
             "CHATGPT_MCP_ROUTER_URL": f"ws://127.0.0.1:{port}/agent"}
router_log = (root / ".tools" / "router-test-stderr.log").open("w", encoding="utf-8")
agent_log = (root / ".tools" / "agent-test-stderr.log").open("w", encoding="utf-8")
router = subprocess.Popen(["dotnet", str(dll)], cwd=root, env=router_env,
        stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=router_log,
        text=True, encoding="utf-8", bufsize=1)
agent = None

def request(id, method, params):
    router.stdin.write(json.dumps({"jsonrpc": "2.0", "id": id, "method": method, "params": params}) + "\n")
    router.stdin.flush()
    while True:
        line = router.stdout.readline()
        if not line:
            raise RuntimeError("Router stopped, read .tools/router-test-stderr.log")
        result = json.loads(line)
        if result.get("id") == id:
            return result

def call(id, name, args=None):
    reply = request(id, "tools/call", {"name": name, "arguments": args or {}})
    assert "error" not in reply, reply
    return reply["result"]

def payload(result):
    return json.loads(result["content"][0]["text"])

try:
    reply = request(1, "initialize", {"protocolVersion": "2025-11-25",
            "capabilities": {}, "clientInfo": {"name": "router-test", "version": "1.0"}})
    assert "result" in reply, reply
    router.stdin.write(json.dumps({"jsonrpc": "2.0", "method": "notifications/initialized"}) + "\n")
    router.stdin.flush()
    listing = request(2, "tools/list", {})["result"]["tools"]
    names = {t["name"] for t in listing}
    assert len(names) == 48, (len(names), names)
    assert "computers.list" in names
    sysinfo = next(t for t in listing if t["name"] == "system.info")
    assert "computer" in sysinfo["inputSchema"]["properties"], sysinfo
    print("MCP_ROUTER_TOOLS_OK", len(names), flush=True)

    before = payload(call(3, "computers.list"))
    assert len(before["computers"]) == 1, before
    agent = subprocess.Popen(["dotnet", str(dll), "--agent"], cwd=root, env=agent_env,
                             stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL, stderr=agent_log)
    ready = False
    for seq in range(4, 24):
        time.sleep(.3)
        data = payload(call(seq, "computers.list"))
        if any(p["computer"] == "TEST-AGENT" and p["connected"] for p in data["computers"]):
            ready = True
            break
    assert ready, "agent did not connect"
    print("AGENT_CONNECTED_OK", flush=True)

    result = payload(call(24, "system.info", {"computer": "TEST-AGENT"}))
    assert result["computer"], result
    assert result["logical_processors"] > 0, result
    print("REMOTE_SYSTEM_INFO_OK", result["computer"], flush=True)
    bad = call(25, "system.info", {"computer": "UNREGISTERED-PC"})
    assert bad.get("isError") is True, bad
    print("UNKNOWN_COMPUTER_REJECTED_OK", flush=True)

    # All computer-bound tools must expose the optional target selector.
    remote_tools = {n for n in names if n.startswith(("fs.", "process.", "shell.", "desktop.")) or n == "system.info"}
    assert len(remote_tools) == 23, sorted(remote_tools)
    for tool in listing:
        if tool["name"] in remote_tools:
            assert "computer" in tool["inputSchema"]["properties"], tool["name"]
    print("ALL_23_TARGET_SCHEMAS_OK", flush=True)

    with tempfile.TemporaryDirectory(prefix="mcp-router-", dir=root / ".tools") as temp:
        test_file = str(Path(temp) / "sample.txt")
        sample_text = "REMOTE UNICODE Тест №42"
        wrote = payload(call(50, "fs.write", {"computer":"TEST-AGENT","path":test_file,"content":sample_text}))
        assert wrote["bytes_written"] == len(sample_text.encode("utf-8")), wrote
        fetched = payload(call(51, "fs.read", {"computer":"TEST-AGENT","path":test_file}))
        assert fetched["text"] == sample_text, fetched
        listed = payload(call(52, "fs.list", {"computer":"TEST-AGENT","path":temp}))
        assert any(e["name"] == "sample.txt" for e in listed["entries"]), listed
        matches = payload(call(53, "fs.search", {"computer":"TEST-AGENT","path":temp,"pattern":"*.txt"}))
        assert matches["match_count"] == 1, matches
        shell = payload(call(54, "shell.exec", {
            "computer":"TEST-AGENT","command":"Write-Output REMOTE-SHELL-OK","timeout_seconds":10}))
        assert shell["exit_code"] == 0 and "REMOTE-SHELL-OK" in shell["stdout"], shell
        processes = payload(call(55, "process.list", {"computer":"TEST-AGENT","name_filter":"python"}))
        assert processes["count"] > 0, processes
        print("REMOTE_FILES_SHELL_PROCESSES_OK", flush=True)

        sleeper = subprocess.Popen(["python","-c","import time;time.sleep(60)"],
                                   stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL,
                                   stderr=subprocess.DEVNULL, creationflags=subprocess.CREATE_NO_WINDOW)
        try:
            killed = payload(call(56, "process.kill", {"computer":"TEST-AGENT","pid":sleeper.pid}))
            assert killed["terminated"] and killed["pid"] == sleeper.pid, killed
            sleeper.wait(timeout=5)
        finally:
            if sleeper.poll() is None:
                sleeper.kill()
                sleeper.wait(timeout=5)
        print("REMOTE_PROCESS_KILL_OK", flush=True)

    if not os.getenv('CHATGPT_MCP_SKIP_GUI_TESTS'):
        screen = payload(call(57, "desktop.screen.info", {"computer":"TEST-AGENT"}))
        virtual = screen["virtual_screen"]
        shot = call(58, "desktop.screen.capture", {
            "computer":"TEST-AGENT",
            "x":virtual["x"],"y":virtual["y"],"width":96,"height":96,"max_width":320})
        assert not shot.get("isError"), shot
        images = [b for b in shot["content"] if b["type"] == "image"]
        assert len(images) == 1, shot
        image_data = base64.b64decode(images[0]["data"])
        assert image_data[:8] == b"\x89PNG\r\n\x1a\n", image_data[:16]
        print("REMOTE_MCP_PNG_OK",len(image_data),flush=True)

    # Submit two calls without waiting; responses may arrive in either order.
    two = [
        {"jsonrpc":"2.0","id":59,"method":"tools/call",
         "params":{"name":"system.info","arguments":{"computer":"TEST-AGENT"}}},
        {"jsonrpc":"2.0","id":60,"method":"tools/call",
         "params":{"name":"process.list","arguments":{"computer":"TEST-AGENT","max_results":2}}}
    ]
    for entry in two:
        router.stdin.write(json.dumps(entry)+"\n")
    router.stdin.flush()
    seen = {}
    while len(seen)<2:
        message=json.loads(router.stdout.readline())
        if message.get("id") in (59,60): seen[message["id"]]=message
    assert set(seen)=={59,60} and all(not m["result"].get("isError") for m in seen.values()),seen
    print("PARALLEL_ROUTING_OK",flush=True)

    # Duplicate computer names cannot replace an already-connected agent.
    with (root / ".tools" / "duplicate-test-stderr.log").open("w", encoding="utf-8") as duplicate_log:
        duplicate = subprocess.Popen(["dotnet", str(dll), "--agent"], cwd=root, env=agent_env,
                     stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL, stderr=duplicate_log)
        try:
            time.sleep(.8)
            clients = payload(call(80, "computers.list"))
            assert sum(c["computer"]=="TEST-AGENT" for c in clients["computers"])==1, clients
            assert payload(call(81, "system.info", {"computer":"TEST-AGENT"}))["computer"], clients
        finally:
            duplicate.terminate()
            duplicate.wait(timeout=5)
    print("DUPLICATE_AGENT_REJECTED_OK",flush=True)

    # Authentication: a wrong token cannot register a second agent.
    wrong_env = {**agent_env, "CHATGPT_MCP_COMPUTER_ID": "WRONG-TOKEN",
                 "CHATGPT_MCP_AGENT_TOKEN": "intentionally-invalid-token"}
    with (root / ".tools" / "unauthorized-test-stderr.log").open("w", encoding="utf-8") as bad_log:
        wrong_agent = subprocess.Popen(["dotnet", str(dll), "--agent"], cwd=root, env=wrong_env,
                     stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL, stderr=bad_log)
        try:
            time.sleep(.8)
            after = payload(call(61, "computers.list"))
            assert all(c["computer"] != "WRONG-TOKEN" for c in after["computers"]), after
        finally:
            wrong_agent.terminate()
            wrong_agent.wait(timeout=5)
    print("INVALID_AGENT_TOKEN_REJECTED_OK",flush=True)

    if not os.getenv('CHATGPT_MCP_SKIP_GUI_TESTS'):
        # Real GUI/UIA operations on an isolated WPF window only.
        source = ast.parse((root / "tests" / "test_windows_uia.py").read_text(encoding="utf-8"))
        fixture = next(ast.literal_eval(node.value) for node in source.body
                       if isinstance(node, ast.Assign) and any(
                           isinstance(t, ast.Name) and t.id == "fixture" for t in node.targets))
        with tempfile.TemporaryDirectory(prefix="mcp-router-wpf-",dir=root / ".tools") as temp:
            script = Path(temp) / "fixture.ps1"
            script.write_text(fixture,encoding="utf-8-sig")
            marker = Path(temp) / "clicked.txt"
            gui = subprocess.Popen([
                "powershell.exe","-NoProfile","-STA","-ExecutionPolicy","Bypass",
                "-File",str(script),str(marker)], creationflags=subprocess.CREATE_NO_WINDOW)
            try:
                window = None
                for _ in range(70):
                    windows = payload(call(62, "desktop.window.list", {
                        "computer":"TEST-AGENT", "title_filter":"ChatGPT MCP UIA TEST WINDOW"}))["windows"]
                    window=next((w for w in windows if w["pid"] == gui.pid),None)
                    if window: break
                    if gui.poll() is not None: raise RuntimeError("WPF fixture exited")
                    time.sleep(.15)
                assert window, "Temporary WPF window not found through remote agent"
                hwnd = window["hwnd"]
                activated = payload(call(63,"desktop.window.activate",{"computer":"TEST-AGENT","hwnd":hwnd}))
                assert activated["foreground"],activated
                tree = payload(call(64,"desktop.uia.tree",{
                    "computer":"TEST-AGENT","hwnd":hwnd,"max_depth":5}))
                assert tree["count"]>=3,tree
                found = payload(call(65,"desktop.uia.find",{
                    "computer":"TEST-AGENT","hwnd":hwnd,"name":"Apply MCP"}))
                assert found["count"]>0,found
                button_path=found["matches"][0]["path"]
                detail = payload(call(66,"desktop.uia.get",{
                    "computer":"TEST-AGENT","hwnd":hwnd,"path":button_path}))
                assert "invoke" in detail["patterns"],detail
                pressed = payload(call(67,"desktop.uia.action",{
                    "computer":"TEST-AGENT","hwnd":hwnd,"path":button_path,"action":"invoke"}))
                assert pressed["success"],pressed
                for _ in range(25):
                    if marker.exists(): break
                    time.sleep(.1)
                assert marker.exists(), "UIA button action not delivered to temporary WPF window"
                edits = payload(call(68,"desktop.uia.find",{
                    "computer":"TEST-AGENT","hwnd":hwnd,"control_type":"Edit"}))
                edit_path=edits["matches"][0]["path"]
                updated = payload(call(69,"desktop.uia.action",{
                    "computer":"TEST-AGENT","hwnd":hwnd,"path":edit_path,
                    "action":"set_value","value":"REMOTE UIA ТЕСТ"}))
                assert updated["success"],updated
                value = payload(call(70,"desktop.uia.get",{
                    "computer":"TEST-AGENT","hwnd":hwnd,"path":edit_path}))
                assert value["value"]=="REMOTE UIA ТЕСТ",value
                moved = payload(call(71,"desktop.window.move",{
                    "computer":"TEST-AGENT","hwnd":hwnd,"x":135,"y":135,"width":600,"height":310}))
                assert abs(moved["bounds"]["x"]-135)<15,moved

                # Mouse moves and click strictly inside the disposable fixture.
                cursor_before=screen["cursor"]
                try:
                    target_x=moved["bounds"]["x"]+100
                    target_y=moved["bounds"]["y"]+85
                    payload(call(72,"desktop.mouse.move",{
                        "computer":"TEST-AGENT","x":target_x,"y":target_y}))
                    payload(call(73,"desktop.mouse.click",{
                        "computer":"TEST-AGENT","x":target_x,"y":target_y}))
                    payload(call(74,"desktop.mouse.scroll",{
                        "computer":"TEST-AGENT","notches":1,"x":target_x,"y":target_y}))
                    payload(call(79,"desktop.uia.action",{
                        "computer":"TEST-AGENT","hwnd":hwnd,"path":edit_path,"action":"focus"}))
                    payload(call(75,"desktop.keyboard.hotkey",{
                        "computer":"TEST-AGENT","keys":"END"}))
                    payload(call(76,"desktop.keyboard.type",{
                        "computer":"TEST-AGENT","text":" REMOTE"}))
                    typed = payload(call(82,"desktop.uia.get",{
                        "computer":"TEST-AGENT","hwnd":hwnd,"path":edit_path}))
                    assert "REMOTE" in typed["value"] and len(typed["value"]) > len("REMOTE UIA ТЕСТ"),typed
                finally:
                    payload(call(77,"desktop.mouse.move",{
                        "computer":"TEST-AGENT","x":cursor_before["x"],"y":cursor_before["y"]}))
                print("REMOTE_GUI_UIA_ACTIONS_OK",flush=True)
                payload(call(78,"desktop.window.close",{"computer":"TEST-AGENT","hwnd":hwnd}))
                gui.wait(timeout=8)
                print("REMOTE_WINDOW_CLOSE_OK",flush=True)
            finally:
                if gui.poll() is None:
                    gui.kill()
                    gui.wait(timeout=5)

    agent.terminate()
    agent.wait(timeout=5)
    agent = None
    disconnected = False
    for seq in range(26, 46):
        time.sleep(.3)
        data = payload(call(seq, "computers.list"))
        if all(p["computer"] != "TEST-AGENT" for p in data["computers"]):
            disconnected = True
            break
    assert disconnected, "agent remained connected after termination"
    print("AGENT_DISCONNECT_OK", flush=True)
finally:
    for p in [agent, router]:
        if p is None: continue
        p.terminate()
        try: p.wait(timeout=5)
        except subprocess.TimeoutExpired:
            p.kill()
            p.wait(timeout=5)
    router_log.close()
    agent_log.close()

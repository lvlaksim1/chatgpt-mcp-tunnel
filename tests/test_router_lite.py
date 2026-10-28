"""Router Lite E2E: one MCP stdio router, one local WebSocket agent.
Uses only the Python standard library; does not alter the live tunnel or working documents.
"""
import json
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
    assert len(names) == 42, (len(names), names)
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

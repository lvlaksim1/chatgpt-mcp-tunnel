#!/usr/bin/env python3
"""Bridge an existing shell.exec MCP tool to the current ChatGPT MCP Tunnel
chat tools. Fallback only for conversations whose captured tool list is stale.

Usage:
  py ChatCycleAdapter.py hello
  py ChatCycleAdapter.py step SESSION_ID TASK_ID [WAIT_SECONDS]
  py ChatCycleAdapter.py complete SESSION_ID TASK_ID BASE64_UTF8_REPORT
  py ChatCycleAdapter.py reply MESSAGE_ID BASE64_UTF8_REPLY
  py ChatCycleAdapter.py status
  py ChatCycleAdapter.py receive [WAIT_SECONDS]
"""
import base64
import json
import os
import subprocess
import sys
from pathlib import Path

RUNTIME = Path(__file__).resolve().parent / "Runtime" / "ChatGptMcpTunnel.dll"
if not RUNTIME.is_file():
    # When running from repository/scripts, resolve the installed server.
    RUNTIME = Path(r"D:\ChatGPTMCPAgent\Runtime\ChatGptMcpTunnel.dll")


def die(msg):
    print(json.dumps({"ok": False, "error": msg}, ensure_ascii=False))
    return 2


def rpc(proc, identifier, method, params):
    request = {"jsonrpc": "2.0", "id": identifier, "method": method, "params": params}
    proc.stdin.write(json.dumps(request, ensure_ascii=False) + "\n")
    proc.stdin.flush()
    while True:
        line = proc.stdout.readline()
        if not line:
            raise RuntimeError("MCP process exited before JSON-RPC reply")
        response = json.loads(line)
        if response.get("id") != identifier:
            continue
        if "error" in response:
            raise RuntimeError("MCP error: " + str(response["error"]))
        return response["result"]


def decoded(value):
    try:
        return base64.b64decode(value, validate=True).decode("utf-8")
    except (ValueError, UnicodeDecodeError) as err:
        raise ValueError("invalid_base64_utf8") from err


def main(args):
    if not RUNTIME.is_file():
        return die("installed_mcp_runtime_not_found")
    if not args:
        return die("missing_action")
    action = args[0].lower()
    params = {}
    tool = "chat.cycle"
    try:
        if action == "hello":
            params = {"action": "hello", "signal": "я тут"}
        elif action == "step" and len(args) >= 3:
            params = {"action": "step", "session_id": args[1], "task_id": args[2],
                      "wait_seconds": int(args[3]) if len(args) > 3 else 35}
        elif action == "complete" and len(args) == 4:
            params = {"action": "complete", "session_id": args[1], "task_id": args[2],
                      "report": decoded(args[3])}
        elif action == "status":
            params = {"action": "status"}
        elif action == "receive":
            tool = "chat.receive"
            params = {"wait_seconds": int(args[1]) if len(args) > 1 else 0}
        elif action == "reply" and len(args) == 3:
            tool = "chat.reply"
            params = {"message_id": args[1], "reply": decoded(args[2])}
        else:
            return die("invalid_action_or_arguments")
    except (ValueError, IndexError) as e:
        return die("invalid_argument: " + str(e))

    env = os.environ.copy()
    # Do not bind a second Router Lite server, or inherit tunnel credentials.
    for key in ("CHATGPT_MCP_ROUTER_ENABLED", "CHATGPT_MCP_ROUTER_LISTEN",
                "CHATGPT_MCP_ROUTER_TLS_PFX", "CHATGPT_MCP_ROUTER_TLS_PASSWORD",
                "CHATGPT_MCP_AGENT_TOKEN", "CONTROL_PLANE_API_KEY"):
        env.pop(key, None)
    # The adapters are short-lived; do not duplicate the existing OpenAI client.
    proc = subprocess.Popen(
        ["dotnet", str(RUNTIME)],
        cwd=RUNTIME.parent, stdin=subprocess.PIPE, stdout=subprocess.PIPE,
        stderr=subprocess.DEVNULL, text=True, encoding="utf-8",
        bufsize=1, env=env)
    try:
        rpc(proc, 1, "initialize", {"protocolVersion": "2025-11-25", "capabilities": {},
            "clientInfo": {"name": "mcp-chat-cycle-adapter", "version": "1.0"}})
        proc.stdin.write(json.dumps({
            "jsonrpc": "2.0", "method": "notifications/initialized"}) + "\n")
        proc.stdin.flush()
        result = rpc(proc, 2, "tools/call", {"name": tool, "arguments": params})
        content = result.get("content", [])
        data = next((c.get("text") for c in content if c.get("type") == "text"), "")
        if not data:
            return die("empty_tool_response")
        print(data)
        return 0 if not result.get("isError") else 3
    except Exception as exc:
        return die(type(exc).__name__ + ": " + str(exc)[:250])
    finally:
        proc.terminate()
        try:
            proc.wait(timeout=4)
        except subprocess.TimeoutExpired:
            proc.kill()
            proc.wait(timeout=4)


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))

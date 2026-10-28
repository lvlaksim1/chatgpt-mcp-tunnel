"""Goose restricted MCP profile: real stdio protocol acceptance, no LLM or user data."""
import json
import os
import subprocess
import tempfile
from pathlib import Path

project = Path(__file__).resolve().parents[1]
dll = Path(os.environ.get(
    "CHATGPT_MCP_TEST_DLL",
    str(project / "src" / "ChatGptMcpTunnel" / "bin" / "Release" / "net8.0" / "ChatGptMcpTunnel.dll"),
))
if not dll.is_file():
    raise SystemExit(f"Build first: {dll}")


def spawn(safe_root):
    env = dict(os.environ)
    env["GOOSE_SAFE_ROOT"] = str(safe_root)
    return subprocess.Popen(
        ["dotnet", str(dll), "--profile", "goose"], cwd=project,
        env=env, stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
        text=True, encoding="utf-8", bufsize=1,
    )


with tempfile.TemporaryDirectory(prefix="goose-safe-mcp-") as temp:
    base = Path(temp)
    safe = base / "safe"
    safe.mkdir()
    outside = base / "outside.txt"
    outside.write_text("PRIVATE OUTSIDE FILE", encoding="utf-8")

    invalid = spawn("")
    invalid_output, invalid_error = invalid.communicate(timeout=10)
    assert invalid.returncode != 0 and "GOOSE_SAFE_ROOT" in invalid_error, (
        invalid.returncode, invalid_output, invalid_error
    )
    print("INVALID_ROOT_REJECTED")

    proc = spawn(safe)
    assert proc.stdin and proc.stdout
    sequence = 0

    def request(method, params=None):
        global sequence
        sequence += 1
        payload = {"jsonrpc": "2.0", "id": sequence, "method": method}
        if params is not None:
            payload["params"] = params
        proc.stdin.write(json.dumps(payload) + "\n")
        proc.stdin.flush()
        while True:
            line = proc.stdout.readline()
            if not line:
                raise RuntimeError(f"Restricted MCP exited early ({proc.poll()})")
            decoded = json.loads(line)
            if decoded.get("id") == sequence:
                return decoded

    def success(method, params=None):
        response = request(method, params)
        assert "result" in response, response
        result = response["result"]
        assert not result.get("isError", False), response
        return result

    def rejected(method, params=None):
        response = request(method, params)
        if "error" in response:
            return
        assert response.get("result", {}).get("isError", False), response

    try:
        initialization = success("initialize", {
            "protocolVersion": "2025-11-25",
            "capabilities": {},
            "clientInfo": {"name": "goose-safe-profile-test", "version": "1"},
        })
        assert initialization.get("protocolVersion"), initialization
        proc.stdin.write(json.dumps({"jsonrpc": "2.0", "method": "notifications/initialized"}) + "\n")
        proc.stdin.flush()

        tool_result = success("tools/list", {})
        tools = {x["name"] for x in tool_result["tools"]}
        assert tools == {"bridge_ping", "fs.list", "fs.read", "fs.write"}, tools
        print("ALLOWLIST_OK", sorted(tools))

        response = success("tools/call", {"name": "bridge_ping", "arguments": {}})
        assert "GOOSE_MCP_SAFE_OK" in json.dumps(response), response

        rejected("tools/call", {"name": "shell.exec", "arguments": {"command": "whoami"}})
        rejected("tools/call", {"name": "github.auth", "arguments": {}})
        rejected("tools/call", {"name": "desktop.window.list", "arguments": {}})
        rejected("tools/call", {"name": "computers.list", "arguments": {}})
        print("UNPUBLISHED_TOOLS_REJECTED")

        denied = [
            {"name": "fs.read", "arguments": {"path": str(outside)}},
            {"name": "fs.read", "arguments": {"path": "../outside.txt"}},
            {"name": "fs.write", "arguments": {"path": "../outside.txt", "content": "WRONG", "overwrite": True}},
            {"name": "fs.write", "arguments": {"path": "program.exe", "content": "WRONG"}},
        ]
        for params in denied:
            rejected("tools/call", params)
        assert outside.read_text(encoding="utf-8") == "PRIVATE OUTSIDE FILE"
        print("PATH_AND_EXTENSION_BOUNDARY_OK")

        result = success("tools/call", {
            "name": "fs.write", "arguments": {"path": "test.bas", "content": "Sub Safe()\nEnd Sub\n"}
        })
        assert "bytes_written" in json.dumps(result)
        read = success("tools/call", {
            "name": "fs.read", "arguments": {"path": "test.bas"}
        })
        assert "Sub Safe()" in json.dumps(read)
        rejected("tools/call", {
            "name": "fs.write", "arguments": {"path": "test.bas", "content": "changed"}
        })
        success("tools/call", {
            "name": "fs.write",
            "arguments": {"path": "test.bas", "content": "Sub Updated()\nEnd Sub\n", "overwrite": True}
        })
        assert (safe / "test.bas").read_text(encoding="utf-8") == "Sub Updated()\nEnd Sub\n"
        print("CONTROLLED_WRITE_AND_READ_OK")

        success("tools/call", {"name": "fs.list", "arguments": {"path": "."}})
        try:
            (safe / "unsafe-link.txt").symlink_to(outside)
        except (OSError, NotImplementedError):
            print("SYMLINK_TEST_NOT_AVAILABLE")
        else:
            rejected("tools/call", {
                "name": "fs.read", "arguments": {"path": "unsafe-link.txt"}
            })
            print("SYMLINK_REJECTED")
    finally:
        proc.terminate()
        try:
            proc.wait(timeout=6)
        except subprocess.TimeoutExpired:
            proc.kill()
            proc.wait(timeout=6)

print("GOOSE_SAFE_PROFILE_PASS")

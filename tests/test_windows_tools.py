"""End-to-end Windows MCP tool test: every tool is called through the stdio protocol.

Only temporary files and a dedicated test process are changed.
"""
import json
import base64
import os
import subprocess
import tempfile
from pathlib import Path

if os.name != "nt":
    raise SystemExit("Windows-only integration test.")

root = Path(__file__).resolve().parents[1]
dll = Path(os.environ.get("CHATGPT_MCP_TEST_DLL", str(root / "src" / "ChatGptMcpTunnel" / "bin" / "Release" / "net8.0" / "ChatGptMcpTunnel.dll")))
control_root = tempfile.TemporaryDirectory(prefix="chatgpt-control-test-")
proc_env = os.environ.copy()
proc_env["CHATGPT_MCP_CONTROL_TEST_ROOT"] = control_root.name
proc = subprocess.Popen(
    ["dotnet", str(dll)], cwd=root, env=proc_env, stdin=subprocess.PIPE, stdout=subprocess.PIPE,
    stderr=subprocess.PIPE, text=True, encoding="utf-8", bufsize=1,
)
assert proc.stdin and proc.stdout
counter = 0

def request(method, params=None):
    global counter
    counter += 1
    payload = {"jsonrpc": "2.0", "id": counter, "method": method, "params": params or {}}
    proc.stdin.write(json.dumps(payload, ensure_ascii=False) + "\n")
    proc.stdin.flush()
    while True:
        line = proc.stdout.readline()
        if not line:
            raise RuntimeError("MCP server closed stdout")
        result = json.loads(line)
        if result.get("id") == counter:
            if result.get("error"):
                raise AssertionError(result["error"])
            return result["result"]

def call(name, **kwargs):
    response = request("tools/call", {"name": name, "arguments": kwargs})
    if response.get("isError"):
        raise AssertionError(f"{name} failed: {response}")
    contents = response.get("content", [])
    text_parts = [part["text"] for part in contents if part.get("type") == "text"]
    assert text_parts, response
    return json.loads(text_parts[0])

test_process = None
try:
    request("initialize", {"protocolVersion": "2025-11-25", "capabilities": {},
                           "clientInfo": {"name": "windows-integration-test", "version": "1"}})
    proc.stdin.write(json.dumps({"jsonrpc": "2.0", "method": "notifications/initialized"}) + "\n")
    proc.stdin.flush()
    tools = request("tools/list")["tools"]
    expected = {"bridge_ping", "system.info", "fs.list", "fs.read", "fs.write",
                "fs.search", "shell.exec", "process.list", "process.kill"}
    gui_tools = {"desktop.batch", "desktop.screen.info", "desktop.screen.capture",
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
    expected.add('computers.list')
    expected.update({"file.transfer.start", "file.transfer.status", "file.transfer.cancel", "desktop.batch"})
    expected.update({"chat.receive", "chat.reply", "chat.cycle", "chat.attachment.read",
                     "chat.ui.open", "chat.ui.pending", "chat.ui.ack",
                     "chat.bootstrap.open", "chat.hub.open", "chat.thread.open",
                     "chat.lifecycle.event", "chat.lifecycle.status", "chat.thread.relay.probe"})
    assert len(github_tools) == 17
    assert {t["name"] for t in tools} == expected, tools
    print("TOOL_DISCOVERY_OK", sorted(expected), flush=True)
    batch = call("desktop.batch", steps_json=json.dumps([
        {"tool": "desktop.screen.info", "arguments": {}},
        {"tool": "desktop.window.list", "arguments": {"title_filter": "UNLIKELY-TEST-MATCH"}},
    ]))
    assert batch["ok"] is True and batch["completed"] == 2, batch
    print("DESKTOP_BATCH_READONLY_OK", flush=True)


    with tempfile.TemporaryDirectory(prefix="chatgpt-mcp-tools-") as temporary:
        location = Path(temporary)
        target = location / "child" / "тест.txt"
        written = call("fs.write", path=str(target), content="Привет, MCP!\nстрока 2")
        assert written["bytes_written"] > 0 and target.exists(), written
        print("FS_WRITE_OK", flush=True)

        read = call("fs.read", path=str(target))
        assert read["format"] == "text" and read["text"] == "Привет, MCP!\nстрока 2", read
        print("FS_READ_OK", flush=True)

        shortened = call("fs.read", path=str(target), max_chars=6)
        assert shortened["format"] == "text" and shortened["truncated"] is True, shortened
        assert shortened["text"] == "Привет", shortened
        print("FS_READ_TRUNCATE_OK", flush=True)

        binary_file = location / "test-data.bin"
        binary_bytes = bytes([0, 1, 2, 255, 127])
        binary_file.write_bytes(binary_bytes)
        binary_result = call("fs.read", path=str(binary_file))
        assert binary_result["format"] == "base64", binary_result
        assert base64.b64decode(binary_result["base64"]) == binary_bytes, binary_result
        print("FS_READ_BINARY_OK", flush=True)

        listed = call("fs.list", path=str(target.parent))
        assert any(e["name"] == target.name for e in listed["entries"]), listed
        print("FS_LIST_OK", flush=True)

        matches = call("fs.search", path=str(location), pattern="*.txt")
        assert any(Path(m["path"]).samefile(target) for m in matches["matches"]), matches
        print("FS_SEARCH_OK", flush=True)

        info = call("system.info")
        assert info["computer"].lower() == os.environ["COMPUTERNAME"].lower(), info
        assert info["logical_processors"] > 0 and info["disks"], info
        print("SYSTEM_INFO_OK", flush=True)

        shell = call("shell.exec", command="Write-Output 'MCP_SHELL_OK'", working_directory=str(location))
        assert shell["exit_code"] == 0 and "MCP_SHELL_OK" in shell["stdout"], shell
        print("SHELL_EXEC_OK", flush=True)
        # Explicit safe activity code survives only as an allowlisted key.
        # Raw commands and sensitive strings must never enter the control log.
        secret = "CONTROL_ACTIVITY_SECRET_123456789"
        shell = call("shell.exec",
                     command=f"Write-Output '{secret}'",
                     activity="tests_run", working_directory=str(location))
        assert shell["exit_code"] == 0 and secret in shell["stdout"], shell
        snapshot = json.loads((Path(control_root.name) / "state.json").read_text(encoding="utf-8"))
        recent = snapshot["Recent"]
        assert any(item["Tool"] == "shell.exec" and item.get("Activity") == "tests_run"
                   for item in recent), recent
        assert secret not in json.dumps(snapshot), snapshot
        print("SHELL_ACTIVITY_REDACTION_OK", flush=True)
        call("shell.exec", command="Write-Output 314159",
             working_directory=str(location), executor="project-manager")
        snapshot = json.loads((Path(control_root.name) / "state.json").read_text(encoding="utf-8"))
        assert snapshot["Recent"][0]["Executor"] == "project-manager", snapshot
        call("shell.exec", command="Write-Output 271828",
             working_directory=str(location))
        snapshot = json.loads((Path(control_root.name) / "state.json").read_text(encoding="utf-8"))
        assert snapshot["Recent"][0]["Executor"] == "chatgpt", snapshot
        print("SHELL_PER_INVOCATION_EXECUTOR_OK", flush=True)



        # shell.exec must close stdin: the MCP server's own stdio pipe stays
        # open for future calls, and Git must not inherit that live input.
        eof = call("shell.exec", command='python -c "import sys; print(\'STDIN_EOF=\' + str(sys.stdin.read() == \'\'))"',
                   working_directory=str(location), timeout_seconds=8)
        assert eof["exit_code"] == 0 and "STDIN_EOF=True" in eof["stdout"], eof
        print("SHELL_STDIN_EOF_OK", flush=True)

        import shutil
        if shutil.which("git"):
            for which_shell in ("powershell", "cmd"):
                git = call("shell.exec", command="git --version", shell=which_shell,
                           working_directory=str(location), timeout_seconds=8)
                assert git["exit_code"] == 0 and "git version" in git["stdout"] and not git["timed_out"], git
                print("SHELL_GIT_" + which_shell.upper() + "_OK", flush=True)

        listed_processes = call("process.list", name_filter="python", max_results=200)
        assert listed_processes["count"] >= 1, listed_processes
        print("PROCESS_LIST_OK", flush=True)

        test_process = subprocess.Popen(
            ["powershell.exe", "-NoProfile", "-NonInteractive", "-Command", "Start-Sleep -Seconds 120"],
            creationflags=subprocess.CREATE_NO_WINDOW,
        )
        terminated = call("process.kill", pid=test_process.pid)
        test_process.wait(timeout=10)
        assert terminated["terminated"] is True and test_process.poll() is not None, terminated
        print("PROCESS_KILL_OK", flush=True)

    print("WINDOWS_MCP_E2E_ALL_PASS", flush=True)
finally:
    if test_process is not None and test_process.poll() is None:
        test_process.kill()
        test_process.wait(timeout=5)
    proc.terminate()
    try:
        proc.wait(timeout=5)
    except subprocess.TimeoutExpired:
        proc.kill()
        proc.wait(timeout=5)

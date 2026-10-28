"""Windows-only direct WAN TLS regression; disposable localhost router/agents and test cert.
No live tunnel, router, firewall, credentials or client machine are modified.
"""
import hashlib
import json
import os
from pathlib import Path
import secrets
import socket
import ssl
import subprocess
import tempfile
import time

ROOT = Path(__file__).resolve().parents[1]
DLL = Path(os.environ.get("CHATGPT_MCP_TEST_DLL", ROOT / "src/ChatGptMcpTunnel/bin/Release/net8.0/ChatGptMcpTunnel.dll"))
assert DLL.is_file(), DLL


def free_port():
    with socket.socket() as probe:
        probe.bind(("127.0.0.1", 0))
        return probe.getsockname()[1]


def mcp_request(router, ident, method, params):
    router.stdin.write(json.dumps({"jsonrpc": "2.0", "id": ident,
                                  "method": method, "params": params}) + "\n")
    router.stdin.flush()
    while True:
        line = router.stdout.readline()
        if not line:
            raise RuntimeError("Disposable router stopped; inspect test stderr log")
        answer = json.loads(line)
        if answer.get("id") == ident:
            assert "error" not in answer, answer
            return answer["result"]


def computers(router, ident):
    output = mcp_request(router, ident, "tools/call",
                         {"name": "computers.list", "arguments": {}})
    return json.loads(output["content"][0]["text"])["computers"]


def connected(router, name, ident):
    return any(item["computer"] == name and item["connected"]
               for item in computers(router, ident))


def wait_ready(router, name, start_id, connected_expected=True, tries=35):
    for idx in range(tries):
        if connected(router, name, start_id + idx) == connected_expected:
            return True
        time.sleep(.2)
    return False


with tempfile.TemporaryDirectory(prefix="mcp-wss-test-") as d:
    tmp = Path(d)
    pfx = tmp / "test-only.pfx"
    pwd = secrets.token_urlsafe(32)
    env = {**os.environ, "TEST_CERT_FILE": str(pfx), "TEST_CERT_PASS": pwd}
    fixture = ROOT / "tests" / "tls-fixture" / "TlsFixture.csproj"
    result = subprocess.run(["dotnet", "run", "--project", str(fixture), "-c", "Release"],
                            check=False, env=env, cwd=ROOT, timeout=45,
                            capture_output=True, text=True)
    assert result.returncode == 0, result.stdout + "\\n" + result.stderr
    assert pfx.is_file()

    ws_port, wss_port = free_port(), free_port()
    while wss_port == ws_port:
        wss_port = free_port()
    token = secrets.token_urlsafe(32)
    env = {**os.environ, "CHATGPT_MCP_AGENT_TOKEN": token,
           "CHATGPT_MCP_ROUTER_ENABLED": "1",
           "CHATGPT_MCP_ROUTER_LISTEN": f"http://127.0.0.1:{ws_port}",
           "CHATGPT_MCP_ROUTER_TLS_PFX": str(pfx),
           "CHATGPT_MCP_ROUTER_TLS_PASSWORD": pwd,
           "CHATGPT_MCP_ROUTER_TLS_PORT": str(wss_port)}
    router_log = (tmp / "router.stderr").open("w", encoding="utf-8")
    router = subprocess.Popen(["dotnet", str(DLL)], cwd=ROOT, env=env,
                              stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                              stderr=router_log, text=True, encoding="utf-8", bufsize=1)
    children = []
    try:
        mcp_request(router, 1, "initialize",
                    {"protocolVersion": "2025-11-25", "capabilities": {},
                     "clientInfo": {"name": "wss-regression", "version": "1"}})
        router.stdin.write('{"jsonrpc":"2.0","method":"notifications/initialized"}\n')
        router.stdin.flush()

        ctx = ssl.SSLContext(ssl.PROTOCOL_TLS_CLIENT)
        ctx.check_hostname = False
        ctx.verify_mode = ssl.CERT_NONE  # Test harness only: obtain fixture certificate hash.
        for _ in range(30):
            try:
                with socket.create_connection(("127.0.0.1", wss_port), timeout=1) as tcp:
                    with ctx.wrap_socket(tcp, server_hostname="localhost") as conn:
                        digest = hashlib.sha256(conn.getpeercert(binary_form=True)).hexdigest().upper()
                        break
            except (ConnectionRefusedError, TimeoutError, ssl.SSLError, OSError):
                time.sleep(.15)
        else:
            raise AssertionError("TLS listener did not start")
        print("WSS_TLS_HANDSHAKE_OK", flush=True)

        def launch(name, url, fingerprint=None):
            e = {**os.environ, "CHATGPT_MCP_AGENT_TOKEN": token,
                 "CHATGPT_MCP_COMPUTER_ID": name,
                 "CHATGPT_MCP_ROUTER_URL": url}
            if fingerprint is not None:
                e["CHATGPT_MCP_ROUTER_TLS_SHA256"] = fingerprint
            stream = (tmp / f"{name}.stderr").open("w", encoding="utf-8")
            child = subprocess.Popen(["dotnet", str(DLL), "--agent"], cwd=ROOT,
                                     env=e, stdin=subprocess.DEVNULL,
                                     stdout=subprocess.DEVNULL, stderr=stream)
            children.append((child, stream))
            return child

        wrong = launch("BAD-CERT", f"wss://localhost:{wss_port}/agent", "0" * 64)
        time.sleep(1.4)
        assert not connected(router, "BAD-CERT", 10), "Wrong TLS fingerprint accepted"
        wrong.terminate()
        wrong.wait(timeout=5)
        print("WSS_BAD_CERT_REJECTED_OK", flush=True)

        good = launch("WSS-CLIENT", f"wss://localhost:{wss_port}/agent", digest)
        assert wait_ready(router, "WSS-CLIENT", 50), "Pinned WSS client failed to connect"
        reply = mcp_request(router, 100, "tools/call",
                            {"name": "system.info", "arguments": {"computer": "WSS-CLIENT"}})
        assert not reply.get("isError"), reply
        print("WSS_PINNED_REMOTE_TOOL_OK", flush=True)

        plain = launch("LAN-CLIENT", f"ws://127.0.0.1:{ws_port}/agent")
        assert wait_ready(router, "LAN-CLIENT", 110), "Existing LAN listener regressed"
        print("EXISTING_LAN_LISTENER_PRESERVED_OK", flush=True)
    finally:
        for child, stream in children:
            if child.poll() is None:
                child.terminate()
                try:
                    child.wait(timeout=4)
                except subprocess.TimeoutExpired:
                    child.kill()
                    child.wait(timeout=4)
            stream.close()
        if router.poll() is None:
            router.terminate()
            try:
                router.wait(timeout=4)
            except subprocess.TimeoutExpired:
                router.kill()
                router.wait(timeout=4)
        router_log.close()

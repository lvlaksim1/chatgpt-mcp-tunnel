"""Authenticated Router Lite streaming transfer end-to-end, no live ports or user files."""
import hashlib
import json
import os
from pathlib import Path
import secrets
import socket
import subprocess
import tempfile
import time

ROOT = Path(__file__).resolve().parents[1]
DLL = Path(os.environ.get("CHATGPT_MCP_TEST_DLL", str(ROOT / "src/ChatGptMcpTunnel/bin/Release/net8.0/ChatGptMcpTunnel.dll")))
assert DLL.is_file(), DLL

def request(proc, req_id, method, params):
    proc.stdin.write(json.dumps({"jsonrpc":"2.0","id":req_id,"method":method,"params":params})+"\n")
    proc.stdin.flush()
    while True:
        line = proc.stdout.readline()
        if not line:
            raise RuntimeError("MCP router stopped unexpectedly")
        answer = json.loads(line)
        if answer.get("id") == req_id:
            if "error" in answer:
                raise RuntimeError(str(answer["error"]))
            return answer["result"]

def call(proc, seq, name, args=None):
    result = request(proc, seq, "tools/call", {"name":name, "arguments":args or {}})
    if result.get("isError"):
        raise RuntimeError(result["content"][0]["text"])
    return json.loads(result["content"][0]["text"])

with tempfile.TemporaryDirectory(prefix="mcp-file-transfer-") as tmp:
    folder = Path(tmp)
    probe = socket.socket()
    probe.bind(("127.0.0.1", 0))
    port = probe.getsockname()[1]
    probe.close()
    token = secrets.token_urlsafe(40)
    base = {**os.environ, "CHATGPT_MCP_AGENT_TOKEN":token, "LOCALAPPDATA":str(folder / "appdata")}
    router_env = {**base, "CHATGPT_MCP_ROUTER_ENABLED":"1",
                  "CHATGPT_MCP_ROUTER_LISTEN":f"http://127.0.0.1:{port}"}
    agent_env = {**base, "CHATGPT_MCP_COMPUTER_ID":"TEST-FILE-AGENT",
                 "CHATGPT_MCP_ROUTER_URL":f"ws://127.0.0.1:{port}/agent"}
    with (folder / "router.log").open("w") as rl, (folder / "agent.log").open("w") as al:
        router = subprocess.Popen(["dotnet",str(DLL)], cwd=ROOT,env=router_env,
                                  stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=rl,
                                  text=True, encoding="utf-8",bufsize=1)
        agent = None
        seq = 1
        def rpc(name, args=None):
            global seq
            seq += 1
            return call(router, seq, name, args)
        def wait(jobid, timeout=90):
            until = time.monotonic()+timeout
            while time.monotonic()<until:
                status=rpc("file.transfer.status",{"transfer_id":jobid})
                if status["status"] in ("completed","failed","cancelled"):
                    return status
                time.sleep(.16)
            raise TimeoutError(rpc("file.transfer.status",{"transfer_id":jobid}))
        try:
            request(router,seq,"initialize",{"protocolVersion":"2025-11-25",
                "capabilities":{},"clientInfo":{"name":"file-transfer-test","version":"1.0"}})
            router.stdin.write(json.dumps({"jsonrpc":"2.0","method":"notifications/initialized"})+"\n")
            router.stdin.flush()
            listing = request(router,seq+1,"tools/list",{})["tools"]
            assert {"file.transfer.start","file.transfer.status","file.transfer.cancel"}.issubset({t["name"] for t in listing})
            seq += 1
            agent=subprocess.Popen(["dotnet",str(DLL),"--agent"],cwd=ROOT,env=agent_env,
                                    stdout=subprocess.DEVNULL,stderr=al)
            for i in range(60):
                if any(c["computer"]=="TEST-FILE-AGENT" for c in rpc("computers.list")["computers"]):
                    break
                time.sleep(.2)
            else:
                raise AssertionError("Test agent did not connect")

            src=folder/"source-15mb.bin"
            src.write_bytes(os.urandom(15*1024*1024+317))
            expected=hashlib.sha256(src.read_bytes()).hexdigest()
            dst=folder/"transferred.bin"
            job=rpc("file.transfer.start",{"source_path":str(src),
                "destination_path":str(dst),"destination_computer":"TEST-FILE-AGENT"})["transfer_id"]
            result=wait(job)
            assert result["status"]=="completed",result
            assert hashlib.sha256(dst.read_bytes()).hexdigest()==expected
            assert result["transferred_bytes"]==src.stat().st_size, result
            print("REMOTE_UPLOAD_15MB_SHA256_OK",result["transferred_bytes"],flush=True)
            failed=rpc("file.transfer.start",{"source_path":str(src),
                "destination_path":str(dst),"destination_computer":"TEST-FILE-AGENT"})["transfer_id"]
            assert wait(failed)["status"]=="failed", "Existing target must be protected"
            replaced=rpc("file.transfer.start",{"source_path":str(src),
                "destination_path":str(dst),"destination_computer":"TEST-FILE-AGENT","overwrite":True})["transfer_id"]
            assert wait(replaced)["status"]=="completed"
            print("OVERWRITE_GUARD_AND_EXPLICIT_OVERWRITE_OK",flush=True)
            download=folder/"round-trip.bin"
            back=rpc("file.transfer.start",{"source_path":str(dst),
                "source_computer":"TEST-FILE-AGENT","destination_path":str(download)})["transfer_id"]
            assert wait(back)["status"]=="completed"
            assert hashlib.sha256(download.read_bytes()).hexdigest()==expected
            print("REMOTE_DOWNLOAD_SHA256_OK",flush=True)

            big=folder/"large-source.bin"
            with big.open("wb") as target:
                for n in range(90):
                    target.write(os.urandom(1024*1024))
            partial=folder/"resumable.bin"
            started=rpc("file.transfer.start",{"source_path":str(big),
                "destination_path":str(partial),"destination_computer":"TEST-FILE-AGENT"})["transfer_id"]
            for i in range(80):
                state=rpc("file.transfer.status",{"transfer_id":started})
                if state["transferred_bytes"]>=1048576 or state["status"]=="completed":break
                time.sleep(.1)
            if state["status"]!="completed":
                rpc("file.transfer.cancel",{"transfer_id":started})
                cancelled=wait(started)
                assert cancelled["status"]=="cancelled",cancelled
                assert (folder/"resumable.bin.mcp-transfer.part").is_file()
                resumed=rpc("file.transfer.start",{"source_path":str(big),
                    "destination_path":str(partial),"destination_computer":"TEST-FILE-AGENT"})["transfer_id"]
                result=wait(resumed,timeout=180)
                assert result["status"]=="completed",result
                print("CANCEL_AND_RESUME_OK",flush=True)
            assert hashlib.sha256(big.read_bytes()).digest()==hashlib.sha256(partial.read_bytes()).digest()
            print("LARGE_90MB_SHA256_OK",flush=True)
        finally:
            router.terminate()
            if agent:agent.terminate()
            for proc in (router,agent):
                if proc:
                    try:proc.wait(timeout=4)
                    except subprocess.TimeoutExpired:proc.kill()

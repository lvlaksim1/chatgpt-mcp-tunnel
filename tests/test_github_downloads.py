"""Check GitHub download media types through the own MCP server.

Always checks a small public GitHub CLI checksum release asset.
Optionally set GITHUB_E2E_JOB_REPO/JOB_ID and GITHUB_E2E_ARTIFACT_REPO/
ARTIFACT_ID to test Actions logs/artifact ZIP. Never mutates GitHub data.
"""
import json
import os
from pathlib import Path
import subprocess
import tempfile

root = Path(__file__).resolve().parents[1]
dll = Path(os.environ.get("MCP_TEST_DLL", str(root / "src/ChatGptMcpTunnel/bin/Release/net8.0/ChatGptMcpTunnel.dll")))
if not dll.is_file():
    raise SystemExit(f"Build first: {dll}")
if os.name != "nt" and not (os.environ.get("GH_TOKEN") or os.environ.get("GITHUB_TOKEN")):
    print("GITHUB_DOWNLOADS_AUTH_UNAVAILABLE_SKIP")
    raise SystemExit(0)
proc = subprocess.Popen(["dotnet", str(dll)], cwd=root, stdin=subprocess.PIPE, stdout=subprocess.PIPE,
    stderr=subprocess.PIPE, text=True, encoding="utf-8", bufsize=1)
assert proc.stdin and proc.stdout
seq = 0

def request(method, params):
    global seq
    seq += 1
    proc.stdin.write(json.dumps({"jsonrpc":"2.0","id":seq,"method":method,"params":params},ensure_ascii=False)+"\n")
    proc.stdin.flush()
    while True:
        line = proc.stdout.readline()
        if not line:
            raise RuntimeError("MCP server terminated")
        response = json.loads(line)
        if response.get("id") == seq:
            if response.get("error"):
                raise RuntimeError(response["error"])
            return response["result"]

def tool(name, **params):
    reply = request("tools/call", {"name":name,"arguments":params})
    if reply.get("isError"):
        raise RuntimeError(f"{name} failed: {reply}")
    payload = json.loads(next(part["text"] for part in reply["content"] if part["type"] == "text"))
    if payload.get("success") is False:
        raise RuntimeError(f"{name}: {payload['error']}")
    return payload

def data(result):
    if result.get("truncated"):
        raise RuntimeError("Response truncated")
    return json.loads(result["data"])

try:
    request("initialize", {"protocolVersion":"2025-11-25","capabilities":{},"clientInfo":{"name":"github-download-regression","version":"1"}})
    proc.stdin.write('{"jsonrpc":"2.0","method":"notifications/initialized"}\n')
    proc.stdin.flush()
    with tempfile.TemporaryDirectory(prefix="mcp-github-downloads-") as folder:
        path = Path(folder)
        release = data(tool("github.releases",action="latest",repository="cli/cli"))
        assets = [a for a in release["assets"] if a["name"].endswith("checksums.txt") and 0<a["size"]<100000]
        assert assets, "Expected a small public checksums.txt release asset"
        asset = assets[0]
        dest = path/"release-checksums.txt"
        result=tool("github.release_assets",action="download",repository="cli/cli",
                    asset_id=asset["id"],local_path=str(dest))
        content=dest.read_bytes()
        assert result["downloaded"] and len(content)==asset["size"],(result,asset)
        assert content[:1]!=b"{" and b"gh_" in content, content[:200]
        print("GITHUB_RELEASE_ASSET_DOWNLOAD_OK",len(content),flush=True)

        job_repo=os.environ.get("GITHUB_E2E_JOB_REPO")
        job_id=os.environ.get("GITHUB_E2E_JOB_ID")
        if job_repo and job_id:
            dest=path/"job-log.txt"
            result=tool("github.jobs",action="download_logs",repository=job_repo,
                        job_id=int(job_id),destination=str(dest))
            assert result["downloaded"] and dest.stat().st_size>50,result
            print("GITHUB_JOB_LOGS_DOWNLOAD_OK",dest.stat().st_size,flush=True)
        else:
            print("GITHUB_JOB_LOGS_DOWNLOAD_NOT_CONFIGURED",flush=True)

        artifact_repo=os.environ.get("GITHUB_E2E_ARTIFACT_REPO")
        artifact_id=os.environ.get("GITHUB_E2E_ARTIFACT_ID")
        if artifact_repo and artifact_id:
            dest=path/"artifact.zip"
            result=tool("github.artifacts",action="download",repository=artifact_repo,
                        artifact_id=int(artifact_id),destination=str(dest))
            assert result["downloaded"] and dest.read_bytes().startswith(b"PK"),result
            print("GITHUB_ACTIONS_ARTIFACT_DOWNLOAD_OK",dest.stat().st_size,flush=True)
        else:
            print("GITHUB_ACTIONS_ARTIFACT_DOWNLOAD_NOT_CONFIGURED",flush=True)
    print("GITHUB_DOWNLOAD_MEDIA_TYPE_REGRESSION_PASS",flush=True)
finally:
    proc.terminate()
    try:
        proc.wait(timeout=5)
    except subprocess.TimeoutExpired:
        proc.kill()
        proc.wait(timeout=5)

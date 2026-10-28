"""E2E tests for the independent Github MCP tools.

Read-only on CI. With GITHUB_E2E_WRITES=1, creates and deletes one temporary
branch in an existing repository. No changes to main, issues, or releases.
"""
import json
import os
import subprocess
import tempfile
import time
import uuid
from pathlib import Path

root = Path(__file__).resolve().parents[1]
dll = Path(os.getenv("MCP_TEST_DLL", str(root / "src/ChatGptMcpTunnel/bin/Release/net8.0/ChatGptMcpTunnel.dll")))
repo = os.getenv("GITHUB_E2E_REPO", "lvlaksim1/chatgpt-mcp-tunnel")
writes = os.getenv("GITHUB_E2E_WRITES") == "1"
proc = subprocess.Popen(["dotnet",str(dll)],cwd=root,stdin=subprocess.PIPE,stdout=subprocess.PIPE,
    stderr=subprocess.PIPE,encoding="utf-8",text=True,bufsize=1)
assert proc.stdin and proc.stdout
seq=0

def request(method,params=None):
    global seq
    seq+=1
    proc.stdin.write(json.dumps({"jsonrpc":"2.0","id":seq,"method":method,"params":params or {}},ensure_ascii=False)+"\n")
    proc.stdin.flush()
    while True:
        line=proc.stdout.readline()
        if not line: raise RuntimeError("MCP server terminated")
        response=json.loads(line)
        if response.get("id")==seq:
            if response.get("error"):raise RuntimeError(str(response["error"]))
            return response["result"]

def tool(name,**args):
    response=request("tools/call",{"name":name,"arguments":args})
    if response.get("isError"):raise RuntimeError(f"{name} failed: {response}")
    parts=[x["text"] for x in response.get("content",[]) if x.get("type")=="text"]
    if not parts:raise RuntimeError("No tool response: "+name)
    data=json.loads(parts[0])
    if data.get("success") is False:raise RuntimeError(f"{name}: {data}")
    return data

def api_data(response):
    return json.loads(response["data"])

branch=None
try:
    request("initialize",{"protocolVersion":"2025-11-25","capabilities":{},
      "clientInfo":{"name":"own-github-e2e","version":"1"}})
    proc.stdin.write('{"jsonrpc":"2.0","method":"notifications/initialized"}\n');proc.stdin.flush()
    registry=request("tools/list",{})["tools"]
    names={x["name"] for x in registry}
    owned={n for n in names if n.startswith("github.")}
    assert len(owned)==17,owned
    print("GITHUB_DISCOVERY_17_OK",flush=True)

    if not (os.environ.get("GH_TOKEN") or os.environ.get("GITHUB_TOKEN")
            or os.name=="nt"):
        print("GITHUB_NO_LOCAL_AUTH_CI_SKIP",flush=True)
    else:
        auth=tool("github.auth")
        assert auth["authenticated"] and auth["login"].lower()=="lvlaksim1",auth
        print("GITHUB_AUTH_OK",auth["login"],flush=True)
        meta=api_data(tool("github.repositories",action="get",repository=repo))
        assert meta["full_name"].lower()==repo.lower(),meta
        print("GITHUB_REPO_READ_OK",flush=True)
        contents=tool("github.files",action="get",repository=repo,path="README.md")
        assert "ChatGPT" in contents["text"] and contents["size"]>300,contents
        print("GITHUB_FILE_READ_OK",flush=True)
        refs=api_data(tool("github.branches",action="get",repository=repo,branch=meta["default_branch"]))
        head=refs["commit"]["sha"]
        print("GITHUB_BRANCH_READ_OK",head[:8],flush=True)
        file_list=tool("github.files",action="list",repository=repo,path="tests")
        assert file_list["data"] or "data" in file_list,file_list
        print("GITHUB_DIRECTORY_LIST_OK",flush=True)
        queries=[
            ("github.api",dict(method="GET",endpoint="/repos/"+repo)),
            ("github.graphql",dict(query="query { viewer { login } }")),
            ("github.issues",dict(action="list",repository=repo)),
            ("github.pulls",dict(action="list",repository=repo)),
            ("github.actions",dict(action="workflows",repository=repo)),
            ("github.actions",dict(action="runs",repository=repo)),
            ("github.releases",dict(action="list",repository=repo)),
            ("github.commits",dict(action="list",repository=repo)),
            ("github.search",dict(kind="repositories",q="repo:"+repo)),
        ]
        for name,args in queries:
            result=tool(name,**args)
            assert "data" in result, result
            print("GITHUB_READ_OK",name,args.get("action",""),flush=True)
        status=tool("github.git",action="status",local_path=str(root))
        if os.name=="nt":
            assert status["exit_code"]==0,status
        print("GITHUB_LOCAL_GIT_STATUS_OK",flush=True)

        if writes:
            branch="mcp-github-e2e-"+uuid.uuid4().hex[:9]
            created=tool("github.branches",action="create",repository=repo,
                         branch=branch,base_ref=meta["default_branch"])
            assert api_data(created)["ref"]=="refs/heads/"+branch
            print("GITHUB_CREATE_TEST_BRANCH_OK",branch,flush=True)
            testpath=".mcp-github-e2e/check.txt"
            written=tool("github.files",action="put",repository=repo,path=testpath,
                         branch=branch,content="Проверка нового GitHub MCP\n",
                         message="test: create own GitHub connector test file")
            sha=api_data(written)["content"]["sha"]
            read=tool("github.files",action="get",repository=repo,path=testpath,branch=branch)
            assert "Проверка" in read["text"]
            print("GITHUB_FILE_CREATE_AND_READ_OK",flush=True)
            updated=tool("github.files",action="put",repository=repo,path=testpath,
                         branch=branch,expected_sha=sha,content="Изменено через MCP\n",
                         message="test: update own GitHub connector test file")
            sha2=api_data(updated)["content"]["sha"]
            assert sha2!=sha
            print("GITHUB_FILE_UPDATE_OK",flush=True)
            with tempfile.TemporaryDirectory(prefix="mcp-github-local-") as temporary:
                checkout = str(Path(temporary) / "checkout")
                cloned = tool("github.git",action="clone",repository=repo,local_path=checkout)
                assert cloned["exit_code"]==0,cloned
                switched = tool("github.git",action="checkout",local_path=checkout,branch=branch)
                assert switched["exit_code"]==0,switched
                local_file=Path(checkout)/".mcp-github-e2e"/"local-git.txt"
                local_file.parent.mkdir(parents=True,exist_ok=True)
                local_file.write_text("GitHub own connector local git push",encoding="utf-8")
                pushed=tool("github.git",action="commit_push",local_path=checkout,
                            message="test: local git publish via own MCP",
                            paths_json=json.dumps([".mcp-github-e2e/local-git.txt"]))
                assert pushed["exit_code"]==0,pushed
                confirmed=tool("github.files",action="get",repository=repo,
                               path=".mcp-github-e2e/local-git.txt",branch=branch)
                assert confirmed["text"]=="GitHub own connector local git push"
                print("GITHUB_LOCAL_GIT_CLONE_COMMIT_PUSH_OK",flush=True)
            batch=tool("github.batch_commit",repository=repo,branch=branch,
                       message="test: atomic two-file GitHub commit",
                       changes_json=json.dumps([{"path":".mcp-github-e2e/first.txt","content":"first"},
                                                {"path":".mcp-github-e2e/second.txt","content":"second"}]))
            assert batch["success"] and batch["files_changed"]==2,batch
            for filename in ("first.txt","second.txt"):
                check=tool("github.files",action="get",repository=repo,path=".mcp-github-e2e/"+filename,branch=branch)
                assert check["text"]==filename.split(".")[0],check
            print("GITHUB_BATCH_ATOMIC_COMMIT_OK",flush=True)
            removed=tool("github.files",action="delete",repository=repo,path=testpath,branch=branch,
                         expected_sha=sha2,message="test: remove test file")
            assert "data" in removed,removed
            print("GITHUB_FILE_DELETE_OK",flush=True)
            compared=tool("github.commits",action="compare",repository=repo,
                          base=meta["default_branch"],head=branch)
            assert "data" in compared
            print("GITHUB_COMMIT_COMPARE_OK",flush=True)
            print("GITHUB_WRITE_E2E_PASS",flush=True)
    print("GITHUB_E2E_ALL_PASS",flush=True)
finally:
    if branch:
        try:
            result=tool("github.branches",action="delete",repository=repo,branch=branch)
            print("GITHUB_TEMP_BRANCH_REMOVED",flush=True)
        except Exception as exc:
            print("GITHUB_TEMP_BRANCH_CLEANUP_FAILED",branch,str(exc)[:500],flush=True)
            raise
    proc.terminate()
    try:proc.wait(timeout=5)
    except subprocess.TimeoutExpired:proc.kill();proc.wait(timeout=5)

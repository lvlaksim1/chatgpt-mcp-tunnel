import os,sys,json,subprocess,time,tempfile
from pathlib import Path
root=Path(__file__).resolve().parents[1]
dll=root/"src/ChatGptMcpTunnel/bin/Release/net8.0/ChatGptMcpTunnel.dll"
with tempfile.TemporaryDirectory(prefix="mcp-uia-") as td:
 env=os.environ.copy()
 env["CHATGPT_MCP_CONTROL_TEST_ROOT"]=str(Path(td)/"control")
 env["CHATGPT_MCP_CHAT_TEST_ROOT"]=str(Path(td)/"chat")
 proc=subprocess.Popen(["dotnet",str(dll)],cwd=root,env=env,
     stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.PIPE,
     text=True,encoding="utf-8",bufsize=1)
 count=0
 def request(method,params={}):
  global count
  count+=1
  proc.stdin.write(json.dumps(dict(jsonrpc="2.0",id=count,method=method,params=params))+"\n")
  proc.stdin.flush()
  while True:
   line=proc.stdout.readline()
   assert line,"MCP server ended"
   obj=json.loads(line)
   if obj.get("id")==count:
    if "error" in obj:raise RuntimeError(str(obj["error"]))
    return obj["result"]
 def call(tool,**kwargs):
  t=time.monotonic()
  r=request("tools/call",{"name":tool,"arguments":kwargs})
  assert not r.get("isError"),r
  return json.loads(r["content"][0]["text"]),round((time.monotonic()-t)*1000)
 try:
  request("initialize",{"protocolVersion":"2025-11-25","capabilities":{},"clientInfo":{"name":"uia-reuse","version":"1"}})
  proc.stdin.write('{"jsonrpc":"2.0","method":"notifications/initialized"}\n');proc.stdin.flush()
  windows,elapsed=call("desktop.window.list",title_filter="")
  hwnd=windows["windows"][0]["hwnd"] if windows.get("windows") else "0x0"
  first,t1=call("desktop.uia.tree",hwnd=hwnd,max_depth=1,max_results=8)
  second,t2=call("desktop.uia.tree",hwnd=hwnd,max_depth=1,max_results=8)
  if windows.get("windows"):
   assert "elements" in first and "elements" in second,(first,second)
  else:
   assert first.get("success") is False and second.get("success") is False,(first,second)
  print("UIA_PERSISTENT_WORKER_TWO_REQUESTS_OK",t1,t2,"ms",flush=True)
 finally:
  proc.terminate()
  try:proc.wait(timeout=4)
  except subprocess.TimeoutExpired:proc.kill()

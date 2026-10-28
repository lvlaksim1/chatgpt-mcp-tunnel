"""Read-only UIA ControlView/RawView compatibility checks for real desktop apps."""
import json,os,subprocess,time
from pathlib import Path
if os.name != "nt": raise SystemExit("Windows only")
root=Path(__file__).resolve().parents[1]
server=subprocess.Popen(["dotnet",str(Path(os.environ.get("CHATGPT_MCP_TEST_DLL", str(root/"src/ChatGptMcpTunnel/bin/Release/net8.0/ChatGptMcpTunnel.dll"))))],cwd=root,stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.PIPE,encoding="utf-8",text=True,bufsize=1)
seq=0
def call(method,params):
 global seq
 seq+=1
 server.stdin.write(json.dumps({"jsonrpc":"2.0","id":seq,"method":method,"params":params},ensure_ascii=False)+"\n");server.stdin.flush()
 while True:
  packet=json.loads(server.stdout.readline())
  if packet.get("id")==seq:
   if packet.get("error"):raise RuntimeError(packet["error"])
   return packet["result"]
def tool(name,**args):
 r=call("tools/call",{"name":name,"arguments":args})
 texts=[x["text"] for x in r.get("content",[]) if x["type"]=="text"]
 if not texts: raise RuntimeError(r)
 o=json.loads(texts[0])
 if o.get("success") is False: raise RuntimeError(o)
 return o
try:
 call("initialize",{"protocolVersion":"2025-11-25","capabilities":{},"clientInfo":{"name":"uia-readonly-probe","version":"1"}})
 server.stdin.write('{"jsonrpc":"2.0","method":"notifications/initialized"}\n');server.stdin.flush()
 windows=tool("desktop.window.list")["windows"]
 for group,token in [("Calculator","CalculatorApp"),("CalculatorHost","ApplicationFrameHost"),("Excel","EXCEL")]:
  target=next((w for w in windows if w.get("process")==token and (group=="Excel" or "Калькулятор" in w.get("title",""))),None)
  if not target:
   print("APP_NOT_OPEN",group,flush=True);continue
  print("APP",group,"TITLE",target["title"],"HWND",target["hwnd"],flush=True)
  for view in ("control","raw"):
   begin=time.monotonic()
   try:
    tree=tool("desktop.uia.tree",hwnd=target["hwnd"],view=view,max_depth=3,max_results=75)
    items=tree["elements"]
    print("APP_TREE",group,view,"COUNT",tree["count"],"TRUNCATED",tree["truncated"],"SECONDS",round(time.monotonic()-begin,2),"SAMPLE",[(e["type"],e["name"][:45],e["type_source"]) for e in items[:12]],flush=True)
    assert tree["count"]>=1
   except Exception as ex:
    print("APP_TREE_ERROR",group,view,str(ex)[:500],flush=True)
    raise
 print("REAL_APP_READONLY_PROBES_PASS",flush=True)
finally:
 server.terminate()
 try:server.wait(timeout=5)
 except subprocess.TimeoutExpired:server.kill();server.wait(timeout=5)

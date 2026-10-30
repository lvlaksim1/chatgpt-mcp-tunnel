"""Non-destructive Thread Entrypoint contract and resource test."""
import os,json,subprocess,tempfile
from pathlib import Path
r=Path(__file__).resolve().parents[1]
dll=Path(os.environ.get("CHATGPT_MCP_TEST_DLL",str(r/"src/ChatGptMcpTunnel/bin/Release/net8.0/ChatGptMcpTunnel.dll")))
with tempfile.TemporaryDirectory(prefix="mcp-thread-relay-") as temp:
 env={**os.environ,"CHATGPT_MCP_CHAT_TEST_ROOT":temp}
 proc=subprocess.Popen(["dotnet",str(dll)],cwd=r,env=env,stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.PIPE,encoding="utf8",errors="replace",bufsize=1)
 seq=0
 def request(method,params=None):
  global seq
  seq+=1
  proc.stdin.write(json.dumps({"jsonrpc":"2.0","id":seq,"method":method,"params":params or {}})+"\n")
  proc.stdin.flush()
  while True:
   line=proc.stdout.readline()
   assert line, "Server stopped"
   obj=json.loads(line)
   if obj.get("id")==seq:
    assert "error" not in obj,obj
    return obj["result"]
 def tool(name,arguments=None):
  result=request("tools/call",{"name":name,"arguments":arguments or {}})
  assert not result.get("isError"),result
  return json.loads(result["content"][0]["text"])
 try:
  request("initialize",{"protocolVersion":"2025-11-25","capabilities":{},"clientInfo":{"name":"test","version":"1"}})
  proc.stdin.write('{"jsonrpc":"2.0","method":"notifications/initialized"}\n')
  proc.stdin.flush()
  tools={t["name"]:t for t in request("tools/list")["tools"]}
  assert len(tools)==59,len(tools)
  item=tools["chat.thread.relay.probe"]
  uri="ui://chatgpt-mcp-tunnel/thread-relay-probe-v0239"
  assert item["_meta"]["ui"]["resourceUri"]==uri
  assert item["_meta"]["openai/ui"]["entrypoints"]==[{"type":"thread"}]
  assert "chat_id" in item["inputSchema"].get("required",[])
  assert not tool("chat.thread.relay.probe",{"chat_id":"not-a-chat"})["ok"]
  chat_id="a"*32
  opened=tool("chat.thread.relay.probe",{"chat_id":chat_id})
  assert opened["ok"] and opened["chat_id"]==chat_id
  res=request("resources/read",{"uri":uri})["contents"][0]
  assert res["mimeType"]=="text/html;profile=mcp-app"
  html=res["text"]
  assert "chat.ui.pending" in html
  assert "ui/message" in html
  assert "chat.lifecycle.event" in html
  assert "discover:true" not in html, "Canary must never discover other chats"
  assert "relay-begin" in html and "thread-relay-probe" in html
  state=tool("chat.lifecycle.status")
  kinds=[e["kind"] for e in state["events"]]
  assert "resource-read" in kinds and "tool-open" in kinds,kinds
  assert not (Path(temp)/"ui-forwarded").exists()
  print("THREAD_RELAY_EXPERIMENT_CONTRACT_AND_ISOLATION_OK")
 finally:
  proc.terminate()
  try:proc.communicate(timeout=5)
  except subprocess.TimeoutExpired:proc.kill();proc.communicate()

"""Isolated contract test of v0.2.19 MCP Apps global/thread entrypoints."""
import os,json,tempfile,subprocess
from pathlib import Path
r=Path(__file__).resolve().parents[1]
dll=Path(os.environ.get("CHATGPT_MCP_TEST_DLL",str(r/"src/ChatGptMcpTunnel/bin/Release/net8.0/ChatGptMcpTunnel.dll")))
assert dll.is_file()
with tempfile.TemporaryDirectory(prefix="mcp-lifecycle-probe-") as temp:
 env=os.environ.copy()
 env["CHATGPT_MCP_CHAT_TEST_ROOT"]=temp
 env["CHATGPT_MCP_CYCLE_TEST_CHAT_ROOT"]=temp
 env["CHATGPT_MCP_CONTROL_TEST_ROOT"]=str(Path(temp)/"control")
 proc=subprocess.Popen(["dotnet",str(dll)],cwd=r,env=env,
  stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.PIPE,
  encoding="utf-8",errors="replace",bufsize=1)
 counter=0
 def request(method,params=None):
  global counter
  counter+=1
  proc.stdin.write(json.dumps({"jsonrpc":"2.0","id":counter,"method":method,
    "params":params or {}},ensure_ascii=False)+"\n")
  proc.stdin.flush()
  while True:
   line=proc.stdout.readline()
   assert line,"Server terminated"
   reply=json.loads(line)
   if reply.get("id")==counter:
    assert "error" not in reply,reply
    return reply["result"]
 def tool(name,args=None):
  result=request("tools/call",{"name":name,"arguments":args or {}})
  assert not result.get("isError"),result
  return json.loads(result["content"][0]["text"])
 try:
  request("initialize",{"protocolVersion":"2025-11-25","capabilities":{},
   "clientInfo":{"name":"mcp-lifecycle-test","version":"1"}})
  proc.stdin.write('{"jsonrpc":"2.0","method":"notifications/initialized"}\n')
  proc.stdin.flush()
  tools={x["name"]:x for x in request("tools/list")["tools"]}
  assert len(tools)==59,len(tools)
  for name,mode,uri in (
   ("chat.hub.open","global","ui://chatgpt-mcp-tunnel/hub-observer-v0229"),
   ("chat.thread.open","thread","ui://chatgpt-mcp-tunnel/lifecycle-thread-v0220")
  ):
   meta=tools[name]["_meta"]
   assert meta["ui"]["resourceUri"]==uri,meta
   assert meta["ui"]["visibility"]==["model","app"],meta
   assert meta["openai/ui"]["entrypoints"]==[{"type":mode}],meta
   assert not tools[name]["inputSchema"].get("required")
   obj=tool(name,{})
   assert obj["ok"] and obj["mode"]==mode and obj["resource"]==uri,obj
   content=request("resources/read",{"uri":uri})["contents"][0]
   assert content["mimeType"]=="text/html;profile=mcp-app"
   assert len(content["text"])>1500
   assert 'const mode = "'+mode+'"' in content["text"]
   assert "ui/initialize" in content["text"]
   assert "ui/notifications/host-context-changed" in content["text"]
   assert "ui/resource-teardown" in content["text"]
   if mode=="global":
    assert 'chat.ui.pending' not in content["text"]
    assert '"ui/message"' not in content["text"]
    assert 'chat.ui.ack' not in content["text"]
   else:
    assert 'chat.ui.pending' not in content["text"]
  # The only auto-dispatching bridge must be invoked from an ordinary
  # ChatGPT conversation, not registered as a Global/Work entrypoint.
  boot=tools["chat.bootstrap.open"]
  boot_meta=boot["_meta"]
  assert "openai/ui" not in boot_meta,boot_meta
  assert boot_meta["ui"]["visibility"]==["model","app"]
  assert boot_meta["ui"]["resourceUri"]=="ui://chatgpt-mcp-tunnel/chat-bootstrap-v0229"
  boot_result=tool("chat.bootstrap.open",{})
  assert boot_result["ok"] and boot_result["mode"]=="mcp_apps_multi_chat",boot_result
  boot_html=request("resources/read",{"uri":boot_meta["ui"]["resourceUri"]})["contents"][0]
  assert boot_html["mimeType"]=="text/html;profile=mcp-app"
  for legacy in ("ui://chatgpt-mcp-tunnel/chat-bootstrap-v0224",
                 "ui://chatgpt-mcp-tunnel/chat-bootstrap-v0225"):
   cached=request("resources/read",{"uri":legacy})["contents"][0]
   assert cached["mimeType"]=="text/html;profile=mcp-app",cached
   assert cached["text"]==boot_html["text"],"Legacy URI must return safe v0229+ bootstrap"
  print("LEGACY_TEMPLATE_COMPAT_V0224_V0225_OK")
  assert "const chatTransport = false;" in boot_html["text"]
  assert '"ui/message"' in boot_html["text"]
  assert 'chat.ui.pending' in boot_html["text"]
  global_html=request("resources/read",{"uri":tools["chat.hub.open"]["_meta"]["ui"]["resourceUri"]})["contents"][0]
  assert 'ui/message' not in global_html["text"]
  assert 'chat.ui.pending' not in global_html["text"]
  assert 'const mode = "chat"' in boot_html["text"]
  assert tools["chat.lifecycle.event"]["_meta"]["ui"]["visibility"]==["app"]
  good=tool("chat.lifecycle.event",{"mode":"global","kind":"ui-initialize",
   "instance_id":"ui-test-01","details_json":json.dumps({
    "host":"ChatGPT","hasMessage":True,"hasDeepLink":True,
    "secret":"DO_NOT_RECORD","source":"host"
   })})
  assert good["ok"],good
  bad=tool("chat.lifecycle.event",{"mode":"global","kind":"ui-error",
    "instance_id":"../../../secrets","details_json":"{}"})
  assert not bad["ok"] and bad["error"]=="invalid_event",bad
  invalid_json=tool("chat.lifecycle.event",{"mode":"thread","kind":"heartbeat",
    "instance_id":"ui-test-02","details_json":"<not-json>"})
  assert not invalid_json["ok"],invalid_json
  records=tool("chat.lifecycle.status")
  assert records["ok"],records
  items=records["events"]
  assert len(items)>=5,items
  assert set(x["mode"] for x in items)>={"global","thread"}
  assert any(x["kind"]=="tool-open" for x in items),items
  assert any(x["kind"]=="resource-read" for x in items),items
  assert not any("DO_NOT_RECORD" in json.dumps(x) for x in items),items
  assert (Path(temp)/"lifecycle-probe/events.jsonl").exists()
  assert not (Path(temp)/"ui-forwarded").exists()
  print("LIFECYCLE_GLOBAL_THREAD_ENTRYPOINTS_ISOLATED_OK")
  print("LIFECYCLE_UI_RESOURCES_PROBED_OK")
  print("LIFECYCLE_DIAGNOSTICS_SANITIZED_AND_NONDESTRUCTIVE_OK")
 finally:
  proc.terminate()
  try:proc.communicate(timeout=5)
  except subprocess.TimeoutExpired:
   proc.kill();proc.communicate()

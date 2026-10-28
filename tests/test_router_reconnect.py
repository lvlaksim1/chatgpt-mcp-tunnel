"""Router Lite reconnection regression: restart a disposable router, keep one agent alive."""
import json
import os
from pathlib import Path
import secrets
import socket
import subprocess
import time

root=Path(__file__).resolve().parents[1]
dll=Path(os.environ.get("CHATGPT_MCP_TEST_DLL",str(root/"src/ChatGptMcpTunnel/bin/Release/net8.0/ChatGptMcpTunnel.dll")))
with socket.socket() as probe:
    probe.bind(("127.0.0.1",0))
    port=probe.getsockname()[1]
secret=secrets.token_urlsafe(32)
router_env={**os.environ,"CHATGPT_MCP_ROUTER_ENABLED":"1",
    "CHATGPT_MCP_AGENT_TOKEN":secret,
    "CHATGPT_MCP_ROUTER_LISTEN":f"http://127.0.0.1:{port}"}
agent_env={**os.environ,"CHATGPT_MCP_AGENT_TOKEN":secret,
    "CHATGPT_MCP_COMPUTER_ID":"RECOVERY-AGENT",
    "CHATGPT_MCP_ROUTER_URL":f"ws://127.0.0.1:{port}/agent"}
router_log=(root/".tools/reconnect-router-stderr.log").open("w",encoding="utf-8")
agent_log=(root/".tools/reconnect-agent-stderr.log").open("w",encoding="utf-8")
agent=None
routers=[]
seq=0
def start_router():
    proc=subprocess.Popen(["dotnet",str(dll)],cwd=root,env=router_env,
        stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=router_log,
        encoding="utf-8",text=True,bufsize=1)
    routers.append(proc)
    global seq
    seq+=1
    exchange(proc,"initialize",{"protocolVersion":"2025-11-25","capabilities":{},
        "clientInfo":{"name":"router-reconnect-test","version":"1"}},seq)
    proc.stdin.write('{"jsonrpc":"2.0","method":"notifications/initialized"}\n')
    proc.stdin.flush()
    return proc
def exchange(proc,method,params,id):
    proc.stdin.write(json.dumps({"jsonrpc":"2.0","id":id,"method":method,"params":params})+"\n")
    proc.stdin.flush()
    while True:
        line=proc.stdout.readline()
        if not line: raise RuntimeError("Disposable router closed its MCP stdout")
        msg=json.loads(line)
        if msg.get("id")==id:
            assert not msg.get("error"),msg
            return msg["result"]
def call(proc,name,args):
    global seq
    seq+=1
    return exchange(proc,"tools/call",{"name":name,"arguments":args},seq)
def wait_connected(proc):
    for _ in range(85):
        data=json.loads(call(proc,"computers.list",{})["content"][0]["text"])
        if any(x["computer"]=="RECOVERY-AGENT" for x in data["computers"]):
            return
        time.sleep(.2)
    raise AssertionError("Agent did not reconnect within test window")
try:
    router1=start_router()
    agent=subprocess.Popen(["dotnet",str(dll),"--agent"],cwd=root,env=agent_env,
        stdin=subprocess.DEVNULL,stdout=subprocess.DEVNULL,stderr=agent_log)
    wait_connected(router1)
    print("AGENT_INITIAL_CONNECTION_OK",flush=True)
    router1.terminate()
    router1.wait(timeout=5)
    print("ROUTER_INTERRUPTED_OK",flush=True)
    router2=start_router()
    wait_connected(router2)
    response=call(router2,"system.info",{"computer":"RECOVERY-AGENT"})
    assert not response.get("isError"),response
    print("AGENT_RECONNECTED_AND_COMMAND_OK",flush=True)
finally:
    for proc in [agent]+routers:
        if proc is None or proc.poll() is not None: continue
        proc.terminate()
        try: proc.wait(timeout=5)
        except subprocess.TimeoutExpired:
            proc.kill()
            proc.wait(timeout=5)
    agent_log.close()
    router_log.close()

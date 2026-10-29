"""Test isolated MCP local-chat routing and handoff without real ChatGPT."""
import json
import os
import subprocess
import tempfile
import uuid
from datetime import datetime, timezone
from pathlib import Path

root = Path(__file__).resolve().parents[1]
dll = Path(os.environ.get("CHATGPT_MCP_TEST_DLL",
    str(root/"src"/"ChatGptMcpTunnel"/"bin"/"Release"/"net8.0"/"ChatGptMcpTunnel.dll")))
legacy = "0"*32
new_id = uuid.uuid4().hex

with tempfile.TemporaryDirectory(prefix="multi-chat-routes-") as tmp:
    data = Path(tmp)
    (data/"messages").mkdir()
    (data/"sessions").mkdir()
    def message(id, chat_id, text):
        payload = {"Id":id, "CreatedUtc":datetime.now(timezone.utc).isoformat(),
            "Text":text, "Status":"pending", "Attachments":[]}
        if chat_id:
            payload["ChatId"] = chat_id
        (data/"messages"/(id+".json")).write_text(json.dumps(payload),encoding="utf-8")
    legacy_msg = uuid.uuid4().hex
    new_msg = uuid.uuid4().hex
    message(legacy_msg, None, "old chat")
    message(new_msg, new_id, "new local chat")
    answered_id = uuid.uuid4().hex
    (data/"messages"/(answered_id+".json")).write_text(json.dumps({
        "Id":answered_id, "ChatId":new_id,
        "CreatedUtc":"2026-10-28T12:00:00+00:00",
        "Text":"Первый вопрос пользователя", "Status":"answered",
        "Reply":"АВТОЗАПУСК-21", "Attachments":[]
    },ensure_ascii=False),encoding="utf-8")
    (data/"sessions"/(new_id+".json")).write_text(
        json.dumps({"Id":new_id,"Title":"РЎРїРµС†РёР°Р»СЊРЅС‹Р№ С‡Р°С‚"}),
        encoding="utf-8")
    env = os.environ.copy()
    env["CHATGPT_MCP_CHAT_TEST_ROOT"] = str(data)
    process = subprocess.Popen(["dotnet",str(dll)],cwd=root,env=env,
        stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.PIPE,
        encoding="utf-8",errors="replace",bufsize=1)
    n=0
    def req(method,params=None):
        global n
        n+=1
        process.stdin.write(json.dumps({"jsonrpc":"2.0","id":n,"method":method,
            "params":params or {}},ensure_ascii=False)+"\n")
        process.stdin.flush()
        while True:
            line=process.stdout.readline()
            assert line, "MCP unexpectedly exited"
            obj=json.loads(line)
            if obj.get("id")==n:
                assert "error" not in obj,obj
                return obj["result"]
    def call(name, args=None):
        result=req("tools/call",{"name":name,"arguments":args or {}})
        return json.loads(result["content"][0]["text"])
    try:
        req("initialize",{"protocolVersion":"2025-11-25","capabilities":{},
             "clientInfo":{"name":"multichat-routing-test","version":"1"}})
        process.stdin.write('{"jsonrpc":"2.0","method":"notifications/initialized"}\n')
        process.stdin.flush()
        tools=req("tools/list")["tools"]
        assert len(tools)==56
        current=call("chat.ui.open")
        assert current["chat_id"]==legacy,current
        own=call("chat.ui.pending",{"chat_id":legacy})
        assert own["message"]["id"]==legacy_msg,own
        other=call("chat.ui.pending",{"chat_id":legacy,"discover":True})
        assert other["message"]["chat_id"]==new_id,other
        assert "АВТОЗАПУСК-21" in other["context"],other
        assert "Первый вопрос пользователя" in other["context"],other
        assert other["message"]["chat_title"]=="РЎРїРµС†РёР°Р»СЊРЅС‹Р№ С‡Р°С‚",other
        ack=call("chat.ui.ack",{"message_id":new_msg,"new_chat":True})
        assert ack["ok"],ack
        waiting=json.loads((data/"ui-routes"/(new_id+".json")).read_text())
        assert waiting["Status"]=="waiting"
        no_more=call("chat.ui.pending",{"chat_id":legacy,"discover":True})
        assert no_more["count"]==0,no_more
        ready=call("chat.ui.open",{"chat_id":new_id})
        assert ready["chat_id"]==new_id,ready
        route=json.loads((data/"ui-routes"/(new_id+".json")).read_text())
        assert route["Status"]=="ready"
        own_new=call("chat.ui.pending",{"chat_id":new_id})
        assert own_new["count"]==0,own_new
        again=call("chat.ui.ack",{"message_id":new_msg,"new_chat":True})
        assert again["already_acknowledged"] is True,again
        invalid=call("chat.ui.pending",{"chat_id":"../no"})
        assert invalid["error"]=="invalid_chat_id",invalid
        print("MULTICHAT_ROUTE_OPEN_DISCOVER_HANDOFF_ACK_OK")
    finally:
        process.terminate()
        try:process.communicate(timeout=5)
        except subprocess.TimeoutExpired:
            process.kill();process.communicate()


"""MCP Apps contract tests, wholly isolated from real ChatGPT messages."""
import json
import os
import subprocess
import tempfile
import uuid
from datetime import datetime, timezone
from pathlib import Path

repo = Path(__file__).resolve().parents[1]
dll = Path(os.environ.get("CHATGPT_MCP_TEST_DLL", str(repo/"src"/"ChatGptMcpTunnel"/"bin"/"Release"/"net8.0"/"ChatGptMcpTunnel.dll")))
assert dll.is_file(), dll
resource_uri = "ui://chatgpt-mcp-tunnel/relay-v0218"

with tempfile.TemporaryDirectory(prefix="mcp-chat-ui-contract-") as temp:
    root = Path(temp)
    (root/"messages").mkdir()
    mid = uuid.uuid4().hex
    msg = root/"messages"/(mid+".json")
    msg.write_text(json.dumps({
        "Id":mid,"CreatedUtc":datetime.now(timezone.utc).isoformat(),
        "Text":"TEST_UI_MESSAGE_FROM_LOCAL_WINDOW","Status":"pending",
        "Attachments":[{"Id":uuid.uuid4().hex,"Name":"test.png","SizeBytes":33,
                        "MediaType":"image/png","Sha256":"a"*64}]
    }),encoding="utf-8")
    env = os.environ.copy()
    env["CHATGPT_MCP_CHAT_TEST_ROOT"] = str(root)
    env["CHATGPT_MCP_CYCLE_TEST_CHAT_ROOT"] = str(root)
    proc = subprocess.Popen(["dotnet",str(dll)], cwd=repo, env=env,
            stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.PIPE,
            encoding="utf-8",errors="replace",text=True,bufsize=1)
    assert proc.stdin and proc.stdout
    serial = 0

    def request(method, params=None):
        global serial
        serial += 1
        proc.stdin.write(json.dumps({"jsonrpc":"2.0","id":serial,"method":method,
                                      "params":params or {}},ensure_ascii=False)+"\n")
        proc.stdin.flush()
        while True:
            line = proc.stdout.readline()
            assert line, "Unexpected MCP exit"
            payload = json.loads(line)
            if payload.get("id")==serial:
                assert "error" not in payload,payload
                return payload["result"]

    def call(name, args=None):
        out = request("tools/call",{"name":name,"arguments":args or {}})
        return json.loads(out["content"][0]["text"])

    try:
        request("initialize",{"protocolVersion":"2025-11-25","capabilities":{},
                "clientInfo":{"name":"relay-test","version":"1"}})
        proc.stdin.write('{"jsonrpc":"2.0","method":"notifications/initialized"}\n')
        proc.stdin.flush()
        tools = {t["name"]:t for t in request("tools/list")["tools"]}
        assert len(tools)==56,len(tools)
        item = tools["chat.ui.open"]
        assert item["_meta"]["ui"]["resourceUri"]==resource_uri,item
        assert "openai/ui" not in item["_meta"],item
        assert tools["chat.ui.pending"]["_meta"]["ui"]["visibility"]==["app"]
        assert tools["chat.ui.ack"]["_meta"]["ui"]["visibility"]==["app"]
        print("MCP_APP_TOOL_META_AND_APP_ONLY_VISIBILITY_OK")

        resources = request("resources/list")["resources"]
        assert any(r["uri"]==resource_uri for r in resources),resources
        content = request("resources/read",{"uri":resource_uri})
        contents = content["contents"]
        assert len(contents)==1,content
        html = contents[0]["text"]
        assert contents[0]["mimeType"]=="text/html;profile=mcp-app",contents
        assert "ui/initialize" in html and "ui/message" in html and "chat.ui.pending" in html,html[:300]
        assert "chat.ui.ack" in html and "ui/resource-teardown" in html
        print("MCP_APP_HTML_RESOURCE_READ_OK")

        opened = call("chat.ui.open")
        assert opened["ok"] and opened["resource"]==resource_uri
        pending=call("chat.ui.pending")
        assert pending["count"]==1 and pending["message"]["id"]==mid,pending
        assert pending["message"]["attachments"][0]["name"]=="test.png"
        assert not (root/"ui-forwarded"/(mid+".json")).exists()
        print("PENDING_MESSAGE_WITH_ATTACHMENTS_NO_PREEMPTIVE_ACK_OK")

        invalid=call("chat.ui.ack",{"message_id":"../bad"})
        assert not invalid["ok"] and invalid["error"]=="invalid_message_id",invalid
        nonexistent=call("chat.ui.ack",{"message_id":uuid.uuid4().hex})
        assert not nonexistent["ok"] and nonexistent["error"]=="message_not_found",nonexistent
        ack=call("chat.ui.ack",{"message_id":mid})
        assert ack["ok"],ack
        repeat=call("chat.ui.ack",{"message_id":mid})
        assert repeat["already_acknowledged"],repeat
        no_pending=call("chat.ui.pending")
        assert no_pending["count"]==0,no_pending
        assert json.loads(msg.read_text(encoding="utf-8"))["Status"]=="pending"
        print("ACK_DURABLE_IDEMPOTENT_NONDESTRUCTIVE_OK")
    finally:
        proc.terminate()
        try: proc.communicate(timeout=5)
        except subprocess.TimeoutExpired:
            proc.kill();proc.communicate()

"""Isolated end-to-end MCP test for local chat attachments. Never uses the live chat queue."""
import hashlib
import json
import os
import shutil
import struct
import subprocess
import tempfile
import uuid
import zlib
from datetime import datetime, timezone
from pathlib import Path

root = Path(__file__).resolve().parents[1]
dll = root / "src" / "ChatGptMcpTunnel" / "bin" / "Release" / "net8.0" / "ChatGptMcpTunnel.dll"


def png_rgba(width=3, height=2):
    row = b"\x00" + b"\x34\x89\xaa\xff" * width
    def chunk(tag, data):
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data))
    return (b"\x89PNG\r\n\x1a\n" +
            chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)) +
            chunk(b"IDAT", zlib.compress(row * height)) +
            chunk(b"IEND", b""))


with tempfile.TemporaryDirectory(prefix="mcp-chat-attachment-e2e-") as temporary:
    chat_root = Path(temporary)
    messages = chat_root / "messages"
    attachments = chat_root / "attachments"
    messages.mkdir()
    mid = uuid.uuid4().hex
    txtid = uuid.uuid4().hex
    imgid = uuid.uuid4().hex
    text_content = "Первое вложение: пример файла\n".encode("utf-8")
    png = png_rgba()
    for aid, content in [(txtid, text_content), (imgid, png)]:
        folder = attachments / mid
        folder.mkdir(parents=True, exist_ok=True)
        (folder / aid).write_bytes(content)
    record = {
        "Id": mid,
        "CreatedUtc": datetime.now(timezone.utc).isoformat(),
        "Text": "",
        "Status": "pending",
        "Attachments": [
            {"Id": txtid, "Name": "пример.txt", "MediaType": "text/plain",
             "SizeBytes": len(text_content), "Sha256": hashlib.sha256(text_content).hexdigest()},
            {"Id": imgid, "Name": "скриншот.png", "MediaType": "image/png",
             "SizeBytes": len(png), "Sha256": hashlib.sha256(png).hexdigest()},
        ],
    }
    msg_path = messages / (mid + ".json")
    msg_path.write_text(json.dumps(record, ensure_ascii=False), encoding="utf-8")

    env = os.environ.copy()
    env["CHATGPT_MCP_CHAT_TEST_ROOT"] = str(chat_root)
    env["CHATGPT_MCP_CYCLE_TEST_CHAT_ROOT"] = str(chat_root)
    proc = subprocess.Popen(
        ["dotnet", str(dll)], cwd=root, env=env,
        stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
        text=True, encoding="utf-8", errors="replace", bufsize=1
    )
    assert proc.stdin and proc.stdout
    number = 1
    def req(method, params):
        global number
        number += 1
        request = {"jsonrpc": "2.0", "id": number, "method": method, "params": params}
        proc.stdin.write(json.dumps(request, ensure_ascii=False) + "\n")
        proc.stdin.flush()
        while True:
            line = proc.stdout.readline()
            if not line:
                raise RuntimeError("MCP exited: " + proc.stderr.read()[:1500])
            response = json.loads(line)
            if response.get("id") == number:
                assert "error" not in response, response
                return response["result"]

    def call(name, **arguments):
        return req("tools/call", {"name": name, "arguments": arguments})["content"]

    try:
        init = req("initialize", {"protocolVersion": "2025-11-25",
                                  "capabilities": {}, "clientInfo": {"name":"attachments-test", "version":"1"}})
        proc.stdin.write(json.dumps({"jsonrpc":"2.0", "method":"notifications/initialized"}) + "\n")
        proc.stdin.flush()
        catalog = req("tools/list", {})["tools"]
        assert "chat.attachment.read" in {tool["name"] for tool in catalog}, "Tool absent"
        result = call("chat.receive", wait_seconds=0, max_messages=10)
        inbox = json.loads(result[0]["text"])
        found = next(x for x in inbox["messages"] if x["id"] == mid)
        assert found["text"] == "" and len(found["attachments"]) == 2, found
        assert found["attachments"][0]["name"] == "пример.txt", found
        print("RECEIVE_METADATA_OK")

        content = call("chat.attachment.read", message_id=mid, attachment_id=txtid, max_chars=2000)
        metadata = json.loads(content[0]["text"])
        body = json.loads(content[1]["text"])
        assert metadata["ok"] and metadata["local_path"].endswith(txtid), metadata
        assert "Первое вложение" in body["text"] and not body["truncated"], body
        print("READ_UTF8_TEXT_OK")

        image = call("chat.attachment.read", message_id=mid, attachment_id=imgid)
        assert json.loads(image[0]["text"])["media_type"] == "image/png"
        assert image[1]["type"] == "image" and image[1]["mimeType"] == "image/png", image
        assert len(image[1]["data"]) > 50, image[1]
        print("READ_REAL_IMAGE_CONTENT_BLOCK_OK")

        invalid = call("chat.attachment.read", message_id=mid, attachment_id="../invalid")
        assert json.loads(invalid[0]["text"])["error"] == "invalid_id"
        (attachments / mid / txtid).write_bytes(b"x" * len(text_content))
        corrupted = call("chat.attachment.read", message_id=mid, attachment_id=txtid)
        assert json.loads(corrupted[0]["text"])["error"] == "attachment_sha256_mismatch"
        print("SECURITY_AND_SHA256_CHECKS_OK")

        answer = call("chat.reply", message_id=mid, reply="Вложения получены")
        assert json.loads(answer[0]["text"])["ok"]
        after = json.loads(msg_path.read_text(encoding="utf-8"))
        assert len(after["Attachments"]) == 2 and after["Status"] == "answered"
        print("REPLY_PRESERVES_ATTACHMENTS_OK")
    finally:
        proc.terminate()
        try: proc.communicate(timeout=5)
        except subprocess.TimeoutExpired: proc.kill(); proc.communicate()

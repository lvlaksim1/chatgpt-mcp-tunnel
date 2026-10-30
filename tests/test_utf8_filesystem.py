"""UTF-8/Unicode and PowerShell output end-to-end regression over MCP stdio."""
import hashlib
import json
import os
import subprocess
import tempfile
from pathlib import Path

repo = Path(__file__).resolve().parents[1]
dll = Path(os.environ.get("CHATGPT_MCP_TEST_DLL",
    str(repo / "src" / "ChatGptMcpTunnel" / "bin" / "Release" / "net8.0" / "ChatGptMcpTunnel.dll")))
with tempfile.TemporaryDirectory(prefix="mcp-utf8-") as root:
    root = Path(root)
    env = dict(os.environ)
    env["CHATGPT_MCP_CONTROL_TEST_ROOT"] = str(root / "control")
    proc = subprocess.Popen(
        ["dotnet", str(dll)], cwd=repo, env=env, stdin=subprocess.PIPE,
        stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, encoding="utf-8", bufsize=1)
    seq = 0
    def req(method, params):
        global seq
        seq += 1
        proc.stdin.write(json.dumps({"jsonrpc":"2.0","id":seq,"method":method,"params":params},
                                    ensure_ascii=False)+"\n")
        proc.stdin.flush()
        while True:
            line=proc.stdout.readline()
            if not line:
                raise RuntimeError("MCP server exited. STDERR="+proc.stderr.read())
            item=json.loads(line)
            if item.get("id")==seq:
                return item
    def call(tool, **params):
        result=req("tools/call",{"name":tool,"arguments":params})
        if "error" in result: raise RuntimeError(result["error"])
        value=result["result"]
        if value.get("isError"): raise RuntimeError(value)
        return json.loads(value["content"][0]["text"])
    try:
        init=req("initialize",{"protocolVersion":"2025-11-25",
            "capabilities":{},"clientInfo":{"name":"utf8-tests","version":"1"}})
        assert "result" in init,init
        proc.stdin.write('{"jsonrpc":"2.0","method":"notifications/initialized"}\n')
        proc.stdin.flush()
        sample="\u041f\u0440\u0438\u0432\u0435\u0442 \u0401\u0436, \u0449\u0443\u043a\u0430, \u2116 \u2014 \U0001f680"
        filename=root/("\u0422\u0435\u0441\u0442-\u0401\u0436\u0438\u043a.txt")
        b=sample.encode("utf-8")
        digest=hashlib.sha256(b).hexdigest()
        wrote=call("fs.write",path=str(filename),content=sample,expected_sha256=digest)
        assert wrote["sha256"]==digest and wrote["verified"], wrote
        assert filename.read_bytes()==b,filename.read_bytes()
        read=call("fs.read",path=str(filename))
        assert read["format"]=="text" and read["text"]==sample and read["encoding"]=="utf-8",read
        print("UTF8_CYRILLIC_PATH_AND_TEXT_BYTE_EXACT_OK",flush=True)

        try:
            call("fs.write",path=str(filename),content="BAD",expected_sha256=digest)
            raise AssertionError("Mismatched checksum accepted")
        except RuntimeError: pass
        assert filename.read_bytes()==b
        print("CHECKSUM_REJECTS_WITHOUT_CHANGING_DESTINATION_OK",flush=True)

        utf16=root/"utf16.txt"
        utf16.write_bytes(b"\xff\xfe"+sample.encode("utf-16le"))
        read=call("fs.read",path=str(utf16))
        assert read["text"]==sample and read["encoding"].startswith("utf-16"),read
        print("UTF16_BOM_AUTO_OK",flush=True)

        legacy=root/"cp1251.txt"
        legacy.write_bytes("\u041f\u0440\u0438\u0432\u0435\u0442, \u0401\u0436\u0438\u043a".encode("cp1251"))
        try:
            call("fs.read",path=str(legacy))
            raise AssertionError("Invalid UTF-8 silently decoded")
        except RuntimeError: pass
        read=call("fs.read",path=str(legacy),encoding="windows-1251")
        assert read["text"]=="\u041f\u0440\u0438\u0432\u0435\u0442, \u0401\u0436\u0438\u043a",read
        print("WINDOWS1251_EXPLICIT_NO_SILENT_REPLACEMENT_OK",flush=True)

        bad=root/"invalid.txt"
        bad.write_bytes(b"\xef\xbb\xbf"+b"\xffnot_utf8")
        try:
            call("fs.read",path=str(bad))
            raise AssertionError("BOM text decoded with replacement")
        except RuntimeError: pass
        print("INVALID_UTF8_BOM_REJECTED_OK",flush=True)

        sliced=call("fs.read",path=str(filename),max_chars=len(sample)-1)
        assert sliced["truncated"] and not sliced["text"].endswith("\ud83d"),sliced
        print("TRUNCATION_NO_HALF_SURROGATE_OK",flush=True)

        command="Write-Output '"+sample+"'"
        shell=call("shell.exec",command=command,shell="powershell",working_directory=str(root))
        assert shell["exit_code"]==0 and sample in shell["stdout"],shell
        print("POWERSHELL_UNICODE_OUTPUT_BYTE_EXACT_OK",flush=True)
        process=call("shell.exec",command="python -c \"print('\\\\u041f\\\\u0440\\\\u0438\\\\u0432\\\\u0435\\\\u0442')\"",
                     shell="powershell",working_directory=str(root))
        assert process["exit_code"]==0,process
        print("ALL_UTF8_FILESYSTEM_AND_SHELL_REGRESSIONS_PASS",flush=True)
    finally:
        proc.terminate()
        try: proc.wait(timeout=5)
        except subprocess.TimeoutExpired: proc.kill()

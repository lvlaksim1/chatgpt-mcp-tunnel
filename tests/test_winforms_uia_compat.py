"""Compatibility diagnostics for a disposable Windows Forms window. Read-only UIA/MSAA checks."""
import json
import os
import subprocess
import tempfile
import time
from pathlib import Path

if os.name != "nt":
    raise SystemExit("UI Automation tests need interactive Windows.")
root = Path(__file__).resolve().parents[1]
dll = Path(os.environ.get("CHATGPT_MCP_TEST_DLL", str(root / "src/ChatGptMcpTunnel/bin/Release/net8.0/ChatGptMcpTunnel.dll")))
server = subprocess.Popen(["dotnet", str(dll)], cwd=root, stdin=subprocess.PIPE,
                          stdout=subprocess.PIPE, stderr=subprocess.PIPE, encoding="utf-8",text=True,bufsize=1)
assert server.stdin and server.stdout
counter = 0

def request(method, params=None):
    global counter
    counter += 1
    rid = counter
    server.stdin.write(json.dumps({"jsonrpc":"2.0","id":rid,"method":method,"params":params or {}},ensure_ascii=False)+"\n")
    server.stdin.flush()
    while True:
        line=server.stdout.readline()
        if not line: raise RuntimeError("MCP server exited")
        data=json.loads(line)
        if data.get("id")==rid:
            if data.get("error"): raise RuntimeError(data["error"])
            return data["result"]

def call(tool_name, **params):
    data=request("tools/call",{"name":tool_name,"arguments":params})
    if data.get("isError"): raise RuntimeError(str(data))
    texts=[b["text"] for b in data.get("content",[]) if b.get("type")=="text"]
    if not texts: raise RuntimeError("Empty UI Automation response: "+str(data))
    obj=json.loads(texts[0])
    if obj.get("success") is False: raise RuntimeError(str(obj))
    return obj

fixture = r"""
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$global:stateFile=$args[0]
$form = New-Object System.Windows.Forms.Form
$form.Text='ChatGPT MCP UIA TEST WINDOW'
$form.StartPosition='Manual'
$form.Location=New-Object System.Drawing.Point(120,120)
$form.Size=New-Object System.Drawing.Size(600,310)
$box=New-Object System.Windows.Forms.TextBox
$box.Name='McpTestEdit'
$box.Text='ORIGINAL'
$box.Location=New-Object System.Drawing.Point(25,35)
$box.Size=New-Object System.Drawing.Size(420,30)
$form.Controls.Add($box)
$button=New-Object System.Windows.Forms.Button
$button.Name='McpTestAction'
$button.Text='Apply MCP'
$button.Location=New-Object System.Drawing.Point(25,110)
$button.Size=New-Object System.Drawing.Size(140,35)
$button.Add_Click({[IO.File]::WriteAllText($global:stateFile,'CLICKED')})
$form.Controls.Add($button)
$check=New-Object System.Windows.Forms.CheckBox
$check.Name='McpTestOption'
$check.Text='Enable MCP'
$check.Location=New-Object System.Drawing.Point(25,170)
$form.Controls.Add($check)
[System.Windows.Forms.Application]::Run($form)
"""

gui=None
try:
    request("initialize",{"protocolVersion":"2025-11-25","capabilities":{},"clientInfo":{"name":"uia-test","version":"1"}})
    server.stdin.write('{"jsonrpc":"2.0","method":"notifications/initialized"}\n')
    server.stdin.flush()
    with tempfile.TemporaryDirectory(prefix="mcp-uia-test-") as temp:
        tmp=Path(temp)
        script=tmp/"fixture.ps1"
        script.write_text(fixture,encoding="utf-8-sig")
        marker=tmp/"clicked.txt"
        gui=subprocess.Popen(["powershell.exe","-NoProfile","-STA","-ExecutionPolicy","Bypass","-File",str(script),str(marker)],creationflags=subprocess.CREATE_NO_WINDOW)
        window=None
        for _ in range(70):
            matches=call("desktop.window.list",title_filter="ChatGPT MCP UIA TEST WINDOW")["windows"]
            window=next((w for w in matches if w["pid"]==gui.pid),None)
            if window:break
            if gui.poll() is not None: raise RuntimeError(f"Fixture died {gui.returncode}")
            time.sleep(.15)
        assert window, "Fixture test window not found"
        hwnd=window["hwnd"]
        print("UIA_FIXTURE_WINDOW",hwnd,flush=True)
        for view in ("control","raw"):
            tree=call("desktop.uia.tree",hwnd=hwnd,max_depth=6,max_results=120,view=view)
            print("WINFORMS_TREE",view,json.dumps(tree,ensure_ascii=False)[:7000],flush=True)
            assert tree["count"]>=4,tree
            for item in tree["elements"]:
                if item["depth"]==1:
                    details=call("desktop.uia.get",hwnd=hwnd,path=item["path"],view=view)
                    print("WINFORMS_ITEM",view,json.dumps(details,ensure_ascii=False)[:3500],flush=True)
            search=call("desktop.uia.find",hwnd=hwnd,name="Apply MCP",view=view)
            assert search["count"]>0,search
            print("WINFORMS_FIND",view,search["count"],flush=True)
            edit=call("desktop.uia.find",hwnd=hwnd,control_type="Edit",view=view)
            button=call("desktop.uia.find",hwnd=hwnd,control_type="Button",view=view)
            checkbox=call("desktop.uia.find",hwnd=hwnd,control_type="CheckBox",view=view)
            assert edit["count"]>=1 and button["count"]>=1 and checkbox["count"]>=1,(edit,button,checkbox)
            if view=="control":
                changed=call("desktop.uia.action",hwnd=hwnd,path=edit["matches"][0]["path"],action="set_value",value="Тест WindowsForms",view=view)
                after_edit=call("desktop.uia.get",hwnd=hwnd,path=edit["matches"][0]["path"],view=view)
                assert after_edit["msaa"]["value"]=="Тест WindowsForms",after_edit
                print("WINFORMS_SET_VALUE_OK",changed["backend"],flush=True)
                invoked=call("desktop.uia.action",hwnd=hwnd,path=button["matches"][0]["path"],action="invoke",view=view)
                for _ in range(20):
                    if marker.exists():break
                    time.sleep(.1)
                assert marker.exists() and marker.read_text()=="CLICKED",invoked
                print("WINFORMS_INVOKE_OK",invoked["backend"],flush=True)
                check_before=call("desktop.uia.get",hwnd=hwnd,path=checkbox["matches"][0]["path"],view=view)
                toggled=call("desktop.uia.action",hwnd=hwnd,path=checkbox["matches"][0]["path"],action="toggle",view=view)
                check_after=call("desktop.uia.get",hwnd=hwnd,path=checkbox["matches"][0]["path"],view=view)
                assert (check_before["msaa"]["state"] & 16)!=(check_after["msaa"]["state"] & 16),(check_before,check_after)
                print("WINFORMS_TOGGLE_OK",toggled["backend"],flush=True)
        call("desktop.window.close",hwnd=hwnd)
        gui.wait(timeout=6)
        print("WINFORMS_COMPAT_DIAGNOSTIC_PASS",flush=True)
finally:
    if gui and gui.poll() is None: gui.kill();gui.wait(timeout=5)
    server.terminate()
    try:server.wait(timeout=5)
    except subprocess.TimeoutExpired:server.kill();server.wait(timeout=5)

"""End-to-end UI Automation test with a disposable Windows Forms window."""
import json
import os
import subprocess
import tempfile
import time
from pathlib import Path

if os.name != "nt":
    raise SystemExit("UI Automation tests need interactive Windows.")
root = Path(__file__).resolve().parents[1]
dll = root / "src/ChatGptMcpTunnel/bin/Release/net8.0/ChatGptMcpTunnel.dll"
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
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName PresentationCore
$global:stateFile=$args[0]
$window=New-Object System.Windows.Window
$window.Title='ChatGPT MCP UIA TEST WINDOW'
$window.Width=600
$window.Height=310
$window.WindowStartupLocation='Manual'
$window.Left=120
$window.Top=120
$panel=New-Object System.Windows.Controls.StackPanel
$panel.Margin=New-Object System.Windows.Thickness(25)
$box=New-Object System.Windows.Controls.TextBox
$box.Name='McpTestEdit'
$box.Text='ORIGINAL'
$box.Height=30
[System.Windows.Automation.AutomationProperties]::SetAutomationId($box,'McpTestEdit')
[void]$panel.Children.Add($box)
$button=New-Object System.Windows.Controls.Button
$button.Name='McpTestAction'
$button.Content='Apply MCP'
$button.Height=35
$button.Margin=New-Object System.Windows.Thickness(0,20,0,0)
[System.Windows.Automation.AutomationProperties]::SetAutomationId($button,'McpTestAction')
$button.Add_Click({ [IO.File]::WriteAllText($global:stateFile, 'CLICKED') })
[void]$panel.Children.Add($button)
$check=New-Object System.Windows.Controls.CheckBox
$check.Name='McpTestOption'
$check.Content='Enable MCP'
$check.Margin=New-Object System.Windows.Thickness(0,20,0,0)
[System.Windows.Automation.AutomationProperties]::SetAutomationId($check,'McpTestOption')
[void]$panel.Children.Add($check)
$window.Content=$panel
$null=$window.ShowDialog()
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
        tree=call("desktop.uia.tree",hwnd=hwnd,max_depth=5,max_results=100)
        print("UIA_TREE",json.dumps(tree,ensure_ascii=False)[:3500],flush=True)
        assert tree["count"] >= 3,tree
        matches=call("desktop.uia.find",hwnd=hwnd,name="Apply MCP",max_depth=10)
        print("UIA_FIND",json.dumps(matches,ensure_ascii=False)[:1500],flush=True)
        assert matches["count"]>0,matches
        button=matches["matches"][0]
        got=call("desktop.uia.get",hwnd=hwnd,path=button["path"])
        print("UIA_GET_BUTTON",json.dumps(got,ensure_ascii=False)[:1600],flush=True)
        assert "invoke" in got["patterns"],got
        invoked=call("desktop.uia.action",hwnd=hwnd,path=button["path"],action="invoke")
        assert invoked["success"]
        for _ in range(25):
            if marker.exists():break
            time.sleep(.1)
        assert marker.exists() and marker.read_text()=="CLICKED", "Button InvokePattern did not run click handler"
        print("UIA_INVOKE_BUTTON_OK",flush=True)
        edits=call("desktop.uia.find",hwnd=hwnd,control_type="Edit")
        assert edits["count"]>0,edits
        edit=edits["matches"][0]
        details=call("desktop.uia.get",hwnd=hwnd,path=edit["path"])
        print("UIA_GET_EDIT",json.dumps(details,ensure_ascii=False)[:1800],flush=True)
        assert "value" in details["patterns"],details
        new_text="Тест MCP UI Automation 2026"
        call("desktop.uia.action",hwnd=hwnd,path=edit["path"],action="set_value",value=new_text)
        updated=call("desktop.uia.get",hwnd=hwnd,path=edit["path"])
        assert updated.get("value")==new_text,updated
        print("UIA_SET_VALUE_OK",flush=True)
        checks=call("desktop.uia.find",hwnd=hwnd,control_type="CheckBox")
        assert checks["count"]>0,checks
        check=checks["matches"][0]
        before=call("desktop.uia.get",hwnd=hwnd,path=check["path"])
        assert "toggle" in before["patterns"],before
        call("desktop.uia.action",hwnd=hwnd,path=check["path"],action="toggle")
        after=call("desktop.uia.get",hwnd=hwnd,path=check["path"])
        assert before["toggle_state"]!=after["toggle_state"],(before,after)
        print("UIA_TOGGLE_OK",flush=True)
        call("desktop.window.close",hwnd=hwnd)
        gui.wait(timeout=6)
        print("UIA_ALL_E2E_PASS",flush=True)
finally:
    if gui and gui.poll() is None: gui.kill();gui.wait(timeout=5)
    server.terminate()
    try:server.wait(timeout=5)
    except subprocess.TimeoutExpired:server.kill();server.wait(timeout=5)

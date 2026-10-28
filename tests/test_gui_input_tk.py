"""Real MCP GUI controls test using disposable Python Tk window on Windows."""
import json
import os
import subprocess
import tempfile
import time
from pathlib import Path
import base64

if os.name != "nt":
    raise SystemExit("This GUI test requires interactive Windows.")

root = Path(__file__).resolve().parents[1]
dll = Path(os.environ.get("CHATGPT_MCP_TEST_DLL", str(root / "src/ChatGptMcpTunnel/bin/Release/net8.0/ChatGptMcpTunnel.dll")))
seq = 0
mcp = subprocess.Popen(["dotnet", str(dll)], cwd=root, stdin=subprocess.PIPE,
                       stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                       encoding="utf-8", text=True, bufsize=1)
assert mcp.stdin and mcp.stdout

def request(method, params=None):
    global seq
    seq += 1
    rid = seq
    mcp.stdin.write(json.dumps({"jsonrpc":"2.0", "id":rid, "method":method, "params": params or {}}, ensure_ascii=False)+"\n")
    mcp.stdin.flush()
    while True:
        line = mcp.stdout.readline()
        if not line: raise RuntimeError("MCP server closed")
        packet = json.loads(line)
        if packet.get("id") == rid:
            if packet.get("error"): raise RuntimeError(packet["error"])
            return packet["result"]

def tool(name, **args):
    result=request("tools/call",{"name":name,"arguments":args})
    if result.get("isError"):raise RuntimeError(f"{name}: {result}")
    return result["content"]

def obj(name, **args):
    return json.loads(next(b["text"] for b in tool(name,**args) if b["type"]=="text"))

gui_script = '''
import json,sys,tkinter as tk
from pathlib import Path
state=Path(sys.argv[1])
app=tk.Tk()
app.title("MCP GUI Input Verification")
app.geometry("560x280+120+120")
field=tk.Entry(app,font=("Arial",16))
field.place(x=30,y=70,width=470,height=35)
field.insert(0,"ORIGINAL")
def write(*args):
    app.update_idletasks()
    state.write_text(json.dumps({"text":field.get(),"x":field.winfo_rootx()+field.winfo_width()//2,"y":field.winfo_rooty()+field.winfo_height()//2}),encoding="utf-8")
app.after(200,write)
field.bind("<KeyRelease>",write)
field.bind("<ButtonRelease>",write)
app.after(300,field.focus_set)
app.mainloop()
'''

gui = None
try:
    request("initialize",{"protocolVersion":"2025-11-25","capabilities":{},"clientInfo":{"name":"tk-gui-e2e","version":"1"}})
    mcp.stdin.write('{"jsonrpc":"2.0","method":"notifications/initialized"}\n')
    mcp.stdin.flush()
    info=obj("desktop.screen.info")
    print("SCREEN_INFO_OK",info["virtual_screen"],flush=True)
    with tempfile.TemporaryDirectory(prefix="mcp-gui-tk-") as temp:
        state=Path(temp)/"state.json"
        ps=Path(temp)/"app.py"
        ps.write_text(gui_script,encoding="utf-8")
        gui=subprocess.Popen(["python",str(ps),str(state)],creationflags=subprocess.CREATE_NO_WINDOW)
        window=None
        for _ in range(80):
            matches=obj("desktop.window.list",title_filter="MCP GUI Input Verification")["windows"]
            if matches and state.exists():
                window=next((x for x in matches if x["pid"]==gui.pid),None)
                if window: break
            time.sleep(.1)
        assert window, "Tk window not detected"
        hwnd=window["hwnd"]
        print("WINDOW_LIST_OK",hwnd,flush=True)
        foreground=obj("desktop.window.activate",hwnd=hwnd)
        assert foreground["foreground"], foreground
        print("WINDOW_ACTIVATE_OK",flush=True)
        data=json.loads(state.read_text(encoding="utf-8"))
        saved=info["cursor"]
        try:
            obj("desktop.mouse.move",x=data["x"],y=data["y"])
            obj("desktop.mouse.click",x=data["x"],y=data["y"])
            print("MOUSE_CLICK_OK",flush=True)
            time.sleep(.25)
            obj("desktop.mouse.scroll",notches=-1,x=data["x"],y=data["y"])
            print("MOUSE_SCROLL_OK",flush=True)
            obj("desktop.keyboard.hotkey",keys="BACKSPACE")
            time.sleep(.25)
            after_backspace=json.loads(state.read_text(encoding="utf-8"))
            assert after_backspace["text"]=="ORIGINA", after_backspace
            print("KEYBOARD_HOTKEY_VERIFIED",flush=True)
            obj("desktop.keyboard.type",text="MCP WIN INPUT")
            time.sleep(.45)
            output=json.loads(state.read_text(encoding="utf-8"))
            print("TEXTBOX_RESULT",repr(output["text"]),flush=True)
            assert output["text"]=="ORIGINAMCP WIN INPUT", output
            print("KEYBOARD_TYPE_VERIFIED",flush=True)
        finally:
            obj("desktop.mouse.move",x=saved["x"],y=saved["y"])
        moved=obj("desktop.window.move",hwnd=hwnd,x=150,y=150,width=560,height=280)
        assert abs(moved["bounds"]["x"]-150)<=10, moved
        print("WINDOW_MOVE_OK",flush=True)
        capture=tool("desktop.screen.capture",x=150,y=150,width=560,height=280,max_width=560)
        img=next(c for c in capture if c["type"]=="image")
        raw=base64.b64decode(img["data"])
        assert raw[:8]==b"\x89PNG\r\n\x1a\n",len(raw)
        print("SCREEN_PNG_OK",len(raw),flush=True)
        obj("desktop.window.close",hwnd=hwnd)
        gui.wait(timeout=5)
        print("WINDOW_CLOSE_OK",flush=True)
    print("DESKTOP_TK_E2E_ALL_PASS",flush=True)
finally:
    if gui and gui.poll() is None: gui.kill();gui.wait(timeout=5)
    mcp.terminate()
    try:mcp.wait(timeout=5)
    except subprocess.TimeoutExpired:mcp.kill();mcp.wait(timeout=5)

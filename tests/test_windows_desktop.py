"""Interactive Windows GUI MCP integration test.

Opens a disposable Windows Forms window, exercises all desktop tool types,
checks typed text by Win32 GetWindowText, and closes the window afterwards.
Never touches pre-existing application documents.
"""
import base64
import ctypes
import json
import os
from pathlib import Path
import struct
import subprocess
import tempfile
import time

if os.name != "nt":
    raise SystemExit("Run only on an interactive Windows desktop.")

root = Path(__file__).resolve().parents[1]
dll = root / "src" / "ChatGptMcpTunnel" / "bin" / "Release" / "net8.0" / "ChatGptMcpTunnel.dll"

server = subprocess.Popen(
    ["dotnet", str(dll)], cwd=root, stdin=subprocess.PIPE, stdout=subprocess.PIPE,
    stderr=subprocess.PIPE, encoding="utf-8", text=True, bufsize=1,
)
assert server.stdin and server.stdout
seq = 0

def request(method, params=None):
    global seq
    seq += 1
    rid = seq
    server.stdin.write(json.dumps({"jsonrpc": "2.0", "id": rid, "method": method, "params": params or {}},
                                  ensure_ascii=False) + "\n")
    server.stdin.flush()
    while True:
        line = server.stdout.readline()
        if not line:
            raise RuntimeError(f"MCP server unexpectedly closed: {server.poll()}")
        result = json.loads(line)
        if result.get("id") == rid:
            if "error" in result:
                raise RuntimeError(str(result["error"]))
            return result["result"]

def call(name, **arguments):
    response = request("tools/call", {"name": name, "arguments": arguments})
    if response.get("isError"):
        raise RuntimeError(f"{name}: {response}")
    return response["content"]

def value(name, **arguments):
    blocks = call(name, **arguments)
    for block in blocks:
        if block.get("type") == "text":
            return json.loads(block["text"])
    raise RuntimeError(f"No JSON for {name}")

script = r"""
Add-Type -AssemblyName System.Windows.Forms
[System.Windows.Forms.Application]::EnableVisualStyles()
$form = New-Object System.Windows.Forms.Form
$form.Text = 'ChatGPT MCP GUI E2E TEST'
$form.StartPosition = 'Manual'
$form.Location = New-Object System.Drawing.Point(80,80)
$form.Size = New-Object System.Drawing.Size(520,260)
$box = New-Object System.Windows.Forms.TextBox
$box.Location = New-Object System.Drawing.Point(26,45)
$box.Size = New-Object System.Drawing.Size(430,30)
$box.Text = 'ORIGINAL'
$form.Controls.Add($box)
$form.Add_Shown({ $box.Focus() })
[System.Windows.Forms.Application]::Run($form)
"""
gui = None
try:
    request("initialize", {"protocolVersion": "2025-11-25", "capabilities": {},
                           "clientInfo": {"name": "desktop-e2e-test", "version": "1"}})
    server.stdin.write('{"jsonrpc":"2.0","method":"notifications/initialized"}\n')
    server.stdin.flush()
    screen = value("desktop.screen.info")
    bounds = screen["virtual_screen"]
    assert bounds["width"] >= 600 and bounds["height"] >= 400, screen
    print("SCREEN_INFO_OK", flush=True)

    with tempfile.TemporaryDirectory(prefix="chatgpt-mcp-gui-") as tmp:
        file = Path(tmp) / "gui-test.ps1"
        file.write_text(script, encoding="utf-8-sig")
        gui = subprocess.Popen(
            ["powershell.exe", "-NoProfile", "-STA", "-ExecutionPolicy", "Bypass", "-File", str(file)],
            creationflags=subprocess.CREATE_NO_WINDOW,
        )
        target = None
        for _ in range(40):
            windows = value("desktop.window.list", title_filter="ChatGPT MCP GUI E2E TEST")["windows"]
            matches = [w for w in windows if w["pid"] == gui.pid]
            if matches:
                target = matches[0]
                break
            if gui.poll() is not None:
                raise RuntimeError(f"Test window terminated: {gui.returncode}")
            time.sleep(0.25)
        if target is None:
            raise RuntimeError("Disposable test window did not appear")
        print("WINDOW_LIST_OK", target["hwnd"], flush=True)

        hwnd = target["hwnd"]
        activation = value("desktop.window.activate", hwnd=hwnd)
        print("WINDOW_ACTIVATE", activation, flush=True)
        assert activation["foreground"] is True, "Test window must be foreground before keyboard or pointer input"
        moved = value("desktop.window.move", hwnd=hwnd, x=100, y=100, width=520, height=260)
        assert abs(moved["bounds"]["x"] - 100) <= 10, moved
        print("WINDOW_MOVE_OK", flush=True)

        capture = call("desktop.screen.capture", x=100, y=100, width=520, height=260, max_width=520)
        pngs = [b for b in capture if b.get("type") == "image"]
        assert len(pngs) == 1, [b.get("type") for b in capture]
        png = base64.b64decode(pngs[0]["data"])
        assert png.startswith(b"\x89PNG\r\n\x1a\n") and len(png) > 1000, (len(png), png[:20])
        w, h = struct.unpack(">II", png[16:24])
        assert (w, h) == (520, 260), (w, h)
        print("SCREEN_CAPTURE_PNG_OK", len(png), flush=True)

        original = screen["cursor"]
        try:
            moved_pointer = value("desktop.mouse.move", x=166, y=206)
            assert moved_pointer["moved"], moved_pointer
            print("MOUSE_MOVE_OK", flush=True)
            click = value("desktop.mouse.click", x=166, y=206, button="left")
            assert click["clicks"] == 1, click
            print("MOUSE_CLICK_OK", flush=True)
            scroll = value("desktop.mouse.scroll", notches=-1, x=166, y=206)
            assert scroll["scrolled"], scroll
            print("MOUSE_SCROLL_OK", flush=True)
            hotkey = value("desktop.keyboard.hotkey", keys="CTRL+A")
            assert hotkey["sent"], hotkey
            print("KEYBOARD_HOTKEY_OK", flush=True)
            typed = value("desktop.keyboard.type", text="Привет MCP 2026")
            assert typed["typed"] and typed["characters"] == len("Привет MCP 2026"), typed
            print("KEYBOARD_TYPE_SENT_OK", flush=True)
        finally:
            value("desktop.mouse.move", x=original["x"], y=original["y"])

        user32 = ctypes.windll.user32
        enum = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.c_void_p, ctypes.c_void_p)
        child_windows = []
        @enum
        def collect(child, _):
            child_windows.append(child)
            return True
        user32.EnumChildWindows(int(hwnd, 16), collect, None)
        found = []
        for child in child_windows:
            text = ctypes.create_unicode_buffer(256)
            user32.GetWindowTextW(child, text, len(text))
            if "Привет MCP 2026" in text.value:
                found.append(text.value)
        if not found:
            raise AssertionError(f"Keyboard input not observed in test textbox (children={len(child_windows)})")
        print("KEYBOARD_TEXT_VERIFIED", found[0], flush=True)
        closed = value("desktop.window.close", hwnd=hwnd)
        assert closed["close_requested"], closed
        gui.wait(timeout=8)
        print("WINDOW_CLOSE_OK", flush=True)
    print("WINDOWS_GUI_E2E_ALL_PASS", flush=True)
finally:
    if gui is not None and gui.poll() is None:
        gui.kill()
        gui.wait(timeout=5)
    server.terminate()
    try:
        server.wait(timeout=5)
    except subprocess.TimeoutExpired:
        server.kill()
        server.wait(timeout=5)

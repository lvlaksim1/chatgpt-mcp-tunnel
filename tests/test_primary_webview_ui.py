from pathlib import Path
root=Path(__file__).resolve().parents[1]
program=(root/"src/ChatGptMcpAgent/Program.cs").read_text(encoding="utf-8")
main=(root/"src/ChatGptMcpAgent/ChatGptMainForm.cs").read_text(encoding="utf-8")
proj=(root/"src/ChatGptMcpAgent/ChatGptMcpAgent.csproj").read_text(encoding="utf-8")
assert "var chat = new ChatGptMainForm()" in program
assert "WebViewChatHost.Start()" not in program
assert "new ChatWindow()" not in program
assert "<Compile Remove=\"ChatWindow.cs\" />" in proj
assert "<Compile Remove=\"WebViewChatHost.cs\" />" in proj
assert "Controls.Add(_browser)" in main
assert "CoreWebView2Environment.CreateAsync" in main
assert "CoreWebView2PreferredColorScheme.Dark" in main
assert "CallDevToolsProtocolMethodAsync" not in main
assert "RichTextBox" not in main and "SnapshotScript" not in main
assert "ChatGptWebViewAuthForm" not in main
assert "https://chatgpt.com/" in main
assert "<Version>0.2.53</Version>" in proj
print("CHATGPT_WEBVIEW_ONLY_UI_OK")
assert "mcp-graphite-palette" in main
assert "#252523" in main and "ReadWarmCss" in main
css=(root/"src/ChatGptMcpAgent/Themes/ChatGPT-Warm-Dark.css").read_text(encoding="utf-8")
assert "#252523" in css and "#E5D9BF" in css
assert "FileSystemWatcher" in main and "ReadWarmCss" in main

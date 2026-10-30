from pathlib import Path
root=Path(__file__).resolve().parents[1]
p=root/"src/ChatGptMcpAgent/ChatGptWebViewAuthForm.cs"
s=p.read_text(encoding="utf-8")
must=["CoreWebView2Environment.CreateAsync","userDataFolder: ProfileFolder",
      "EnsureCoreWebView2Async","NewWindowRequested","Navigate(",
      "ChatGptMcpAgent","WebView2", "GetDeferral"]
for term in must:
    assert term in s,term
for forbidden in ["ExecuteScriptAsync","AddScriptToExecuteOnDocumentCreated",
    "Input.insertText","MutationObserver","CallDevToolsProtocolMethodAsync",
    "GetCookiesAsync","CookieManager"]:
    assert forbidden not in s,"WebView2 authentication must not use chat scripting: "+forbidden
program=(root/"src/ChatGptMcpAgent/Program.cs").read_text(encoding="utf-8")
assert "BrowserChatWorker" not in program
assert "chat.ui.open" not in s
build=(root/"scripts/Build-LocalRelease.ps1").read_text(encoding="utf-8")
assert "OpenAI\\node.exe" not in build
assert "BrowserChatBridge.js" not in build
cs=(root/"src/ChatGptMcpAgent/ChatGptMcpAgent.csproj").read_text(encoding="utf-8")
assert "Microsoft.Web.WebView2" in cs and "<Version>" in cs
html=(root/"src/ChatGptMcpTunnel/ChatRelayApp.html").read_text(encoding="utf-8")
assert 'annotations:{audience:["assistant"]}' in html
assert 'formatMessage(message)' in html
assert '"ui/message"' in html
assert 'autoStart = true;' in html
assert '"chat.ui.ack"' in html
print("WEBVIEW2_AUTH_ONLY_MCP_MESSAGE_BOUNDARY_CONFIRMED")

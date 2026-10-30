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
    assert forbidden not in s,"Legacy WebView2 auth UI must remain script-free: "+forbidden
program=(root/"src/ChatGptMcpAgent/Program.cs").read_text(encoding="utf-8")
assert "WebViewChatHost.Start()" in program and "EnsureRunning()" not in program
assert "chat.ui.open" not in s
build=(root/"scripts/Build-LocalRelease.ps1").read_text(encoding="utf-8")
assert "OpenAI\\node.exe" not in build
assert "BrowserChatBridge.js" in build
cs=(root/"src/ChatGptMcpAgent/ChatGptMcpAgent.csproj").read_text(encoding="utf-8")
assert "Microsoft.Web.WebView2" in cs and "<Version>0.2.49</Version>" in cs
bridge=(root/"src/ChatGptMcpAgent/BrowserChatBridge.js").read_text(encoding="utf-8")
assert 'Input.insertText' in bridge and 'Runtime.evaluate' in bridge
assert 'api.openai.com' not in bridge
print("WEBVIEW_AUTH_SEPARATE_FROM_BROWSER_TRANSPORT_OK")

host=(root/"src/ChatGptMcpAgent/WebViewChatHost.cs").read_text(encoding="utf-8")
assert "CoreWebView2Environment.CreateAsync" in host
assert "CallDevToolsProtocolMethodAsync" in host
assert "ChatGptWebViewAuthForm.ProfileFolder" in host
assert "NavigateAsync" in host

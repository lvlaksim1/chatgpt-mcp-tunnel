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
assert "_content.Controls.Add(tab.View)" in main
assert "CoreWebView2Environment.CreateAsync" in main
assert "CoreWebView2PreferredColorScheme.Dark" in main
assert "CallDevToolsProtocolMethodAsync(\"Page.reload\"" in main
assert "RichTextBox" not in main and "SnapshotScript" not in main
assert "ChatGptWebViewAuthForm" not in main
assert "https://chatgpt.com/" in main
assert "<Version>0.2.65</Version>" in proj
print("CHATGPT_WEBVIEW_ONLY_UI_OK")
assert "mcp-graphite-palette" in main
assert "#252523" in main and "ReadWarmCss" in main
css=(root/"src/ChatGptMcpAgent/Themes/ChatGPT-Warm-Dark.css").read_text(encoding="utf-8")
assert "#252523" in css and "#E5D9BF" in css
assert "FileSystemWatcher" in main and "ReadWarmCss" in main
toolbar=(root/"src/ChatGptMcpAgent/Themes/ChatGPT-Toolbar.js").read_text(encoding="utf-8")
assert "mcpConversationToolbar" in toolbar and "showShare" in toolbar
assert "Chat actions" in toolbar and "data-mcp-share-action" in toolbar
assert "RegisterToolbarAsync" in main and "WatchFiles" in main
context=(root/"src/ChatGptMcpAgent/Themes/ChatGPT-Context.js").read_text(encoding="utf-8")
assert "mcp_kind:'clickable_context'" in context
assert "OnClickableContext" in main and "HasCustomCaption" in main
assert "NewWindowRequested" in main and "SmokeTabsAsync" in main
assert "SetChildIndex(_newTab, _tabs.Controls.Count - 1)" in main
assert "_tabs.Controls.Add(_newTab)" in main
assert "_addressShell.Controls.Add(_address)" in main
controls=(root/"src/ChatGptMcpAgent/WarmChromeControls.cs").read_text(encoding="utf-8")
assert "WarmChromeButton" in controls and "WarmChromePanel" in controls
chrome=(root/"src/ChatGptMcpAgent/Themes/BrowserChrome.html").read_text(encoding="utf-8")
assert 'id="tabs-scroll"' in chrome
assert "plus.id='add-tab'" in chrome
assert "tabRow.append(plus)" in chrome
assert 'id="address"' in chrome and 'id="reload"' in chrome
assert "hard-reload" in chrome and "browser_state" in chrome
assert "#252523" in chrome and "#E5D9BF" in chrome
assert "ChromeInitialized" in main and "SmokeChromeAsync" in main
assert "_layout.Controls.Add(_chrome, 0, 0)" in main
assert "_layout.Controls.Add(_content, 0, 1)" in main
assert 'CoreWebView2HostResourceAccessKind.DenyCors' in main
voice_file=(root/"src/ChatGptMcpAgent/Themes/ChatGPT-Voice.js").read_text(encoding="utf-8")
assert "setComposer" in voice_file and "voice_reply" in voice_file
assert "voice-start" in chrome and "voice-stop" in chrome
assert "voice-settings" in chrome and "voice-speaker" in chrome
assert "voice-mic" in chrome and "autoSpeak" in chrome
voice_cs=(root/"src/ChatGptMcpAgent/ChatGptMainForm.Voice.cs").read_text(encoding="utf-8")
assert "HandleVoiceChromeCommandAsync" in voice_cs
assert "HandleVoiceWebMessageAsync" in voice_cs
assert "InsertVoiceTextAsync" in voice_cs
assert "ShutdownVoice()" in main

# Chat in ChatGPT MCP Tunnel (v0.2.12)

## Scope
The tray application has a new **Чат** command (and tray double click).
The independent Windows Forms window supports typing messages, Enter to send
(Shift+Enter for newline), and an on-disk conversation log.

This is not a web browser, Codex, Work, or a paid model API. A connected ChatGPT
session must read the queue through MCP and write responses through MCP.
Messages do **not** initiate a new ChatGPT turn by themselves.

## MCP protocol
- `chat.receive(wait_seconds=0..45, max_messages=1..30)`: read pending
  messages, optionally long poll, return `count` and `messages`.
- `chat.reply(message_id, reply)`: save the reply under the supplied message ID.
  A second reply to an answered message is idempotent.

On the DOMKAT server, the tray application and MCP runtime share the queue
in `%LOCALAPPDATA%\ChatGPTMCPAgent\Chat\messages`.
Each user-written message is persisted atomically as a separate JSON file.
Messages not yet answered remain pending after restarts. Responses are
saved in the same record. This first version addresses the local server
window, not remote-agent windows.

## Hourly automation
An hourly ChatGPT automation can invoke `chat.receive` and `chat.reply`
for as long as each execution is permitted. It cannot guarantee a
continuously active 60-minute model turn; tasks are bounded by ChatGPT
runtime limits. When the next run starts, the outstanding messages remain
available in the queue.

## Security
Message content and replies are local plaintext in the current Windows
user profile; they are not stored in Git. Do not paste passwords or
secrets in the chat window. No browser sign-in is copied or reused.

## Verification
- `dotnet build src/ChatGptMcpAgent/ChatGptMcpAgent.csproj -c Release`
- `dotnet build src/ChatGptMcpTunnel/ChatGptMcpTunnel.csproj -c Release`
- `ChatGptMcpAgent.exe --test-chat-window`
- `python tests/test_mcp_stdio.py` (includes queued-message exchange)

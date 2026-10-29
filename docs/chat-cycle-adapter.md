# ChatGPT MCP Tunnel: fallback for an old ChatGPT tool catalog

As of v0.2.13, the server exposes 48 MCP tools including `chat.cycle`,
`chat.receive` and `chat.reply`. ChatGPT may continue using the tool
definitions captured when the plugin was first connected. If the new tools
are missing but `shell.exec` is still available, invoke exactly the same
MCP operations through the short-lived, stdio-only adapter.

On DOMKAT the adapter is deployed at:

  D:\ChatGPTMCPAgent\ChatCycleAdapter.py

The source is tracked as `scripts/ChatCycleAdapter.py`. A release update
preserves this extra local file but does not install it automatically.

## Commands

  py "D:\ChatGPTMCPAgent\ChatCycleAdapter.py" hello
  py "D:\ChatGPTMCPAgent\ChatCycleAdapter.py" step SESSION_ID TASK_ID 35
  py "D:\ChatGPTMCPAgent\ChatCycleAdapter.py" status
  py "D:\ChatGPTMCPAgent\ChatCycleAdapter.py" receive 0

For final reports or text replies, encode UTF-8 text with base64 to avoid
shell quoting and pass it as the final argument:

  $report = 'За пять минут: ...'
  $b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($report))
  py "D:\ChatGPTMCPAgent\ChatCycleAdapter.py" complete SESSION_ID TASK_ID $b64

  $reply = 'Ответ пользователю'
  $b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($reply))
  py "D:\ChatGPTMCPAgent\ChatCycleAdapter.py" reply MESSAGE_ID $b64

The model should use the adapter only when native `chat.cycle` tools are not
listed. The adapter starts a separate **stdio MCP tool host** per call and
removes Router Lite and token environment variables before launching it.
It never starts a second OpenAI tunnel client or triggers a new ChatGPT turn.

The hourly ChatGPT task remains the only starter. The application cannot
force another ChatGPT task or guarantee a full 60-minute active session.

# Experiment: MCP Apps ui/message — 29.10.2026

Authority: direct user instruction, ordinary ChatGPT only; no API, Codex, Work, browser automation, scheduler.
Architecture: tool chat.ui.open associates a read-only MCP Apps HTML resource; user opens app in ChatGPT and enables sending. Live iframe polls a separate compact local queue through MCP tools, invokes host ui/message, and only then confirms durable acknowledgment in a separate file. No changes to source message, file contents, existing network routes or tunnel ID.
Resource tool metadata: _meta.ui.resourceUri and OpenAI global/thread entrypoints. Other tools use ui.visibility=["app"]. HTML no external scripts/network, full lifecycle and host rejection handling.
Tests: .NET builds clean; server tools/list 52; resources/list/read, pending and ack idempotency; simulated host ui/message accepted/rejected; existing chat, file attachments, Router Lite tests pass.
Limits: MCP Apps ui/message is View-to-Host from an active iframe. Live ChatGPT must be tested for automatic submission after timer and whether host tears down iframe after a turn. Until accepted there is no guaranteed instant chat when ordinary ChatGPT is closed.

## 29.10.2026: REAL ordinary ChatGPT ui/message acceptance 19:36-19:42 MSK
Real host PASS, DOMKAT 0.2.16, ordinary ChatGPT with active MCP Apps iframe. Manual nonce 58ewppk5 and automated queue 43830fd3, local WinForms chat GUI8C42, inactive browser tab BGTAB42 all prompted matching ChatGPT turns. Auto queue and GUI responses persisted via chat.reply, ACK true. Tests did not involve hourly planner. No guarantee of working after iframe/browser closure or suspension; old GUI footer misreports offline status and should be fixed.

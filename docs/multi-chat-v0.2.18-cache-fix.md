# ChatGPT MCP Tunnel v0.2.18 — MCP Apps cache refresh

The v0.2.17 executable and server worked. Live acceptance in ordinary ChatGPT exposed that the existing `ui://chatgpt-mcp-tunnel/relay` resource could still render the 0.2.16 HTML even after the plugin tools refresh and new ChatGPT conversation. The installed server's `ChatRelayApp.html` contained the new `target:needsNew` implementation, but the displayed widget kept the old status wording.

This patch assigns a new, versioned resource URI `ui://chatgpt-mcp-tunnel/relay-v0218`, updates the MCP Apps resource registration, package version and protocol test. The stable resource URI for each future UI revision should be versioned; altering only the server HTML does not guarantee that ChatGPT will refresh a cached iframe.

Acceptance: new `chat.ui.open` tool UI metadata and `resources/read` must advertise `relay-v0218`, then in ordinary ChatGPT open the app, start relay and send a first message from a new local chat. Confirm `ui/message` with target `new` creates a new ChatGPT thread, the new thread calls `chat.ui.open(chat_id=...)`, and replies reach the correct local chat via `chat.reply`. Both normal and new-chat sending remain MCP-only.

## Live acceptance: PASS (29 Oct 2026)
- DOMKAT verified on v0.2.18.0 after controlled update; GitHub Actions Windows release 40926899682 SUCCESS; readyz=200, ChatGPT MCP Tunnel bridge_ping OK, computers.list DOMKAT and MKATUSHONOK connected, Windows Recovery watchdog Ready, all prior 142 message files retained.
- Authenticated Yandex Browser: ChatGPT MCP Tunnel > Manage > Refresh tools performed. In a new ordinary ChatGPT conversation the model invoked chat.ui.open. Model output and rendered app confirmed new resource URI ui://chatgpt-mcp-tunnel/relay-v0218 (cache fixed).
- Clicked Begin Forwarding in real MCP Apps, then started installed 0.2.18 GUI in preview-chat-window mode against real local Chat data and clicked New Chat. New local chat_id c8d881eb8f0f4ff1aac24dcfe21a0a0c persisted. Submitted test message marker 42F6, message_id 28d970531bc3438991bfb08816c94358. Sent via ui/message to new ChatGPT thread, iframe route state ready, ui-forwarded durable ACK created. chat.reply wrote Status=answered, Reply=ПРИНЯТО into correct local message.
- Second follow-up in same local chat marker C2A7, message_id 29d0c5d2b59b42bda27b3caf913ccbed: Reply=ПРОДОЛЖЕНИЕ, Status=answered, ui-forwarded present, same chat_id as first; verifies continuous relay after new-thread handoff.
- Via UIA selected 'История до обновления' and observed historical diagnostic messages; selected new chat and read only the two new tests (ПРИНЯТО, ПРОДОЛЖЕНИЕ), no legacy content. Classic local new chat/list/switch works in real GUI.
- Old per-hour ChatGPT automation stayed disabled, no Work/Codex/API used for transport. Active MCP Apps iframe still required; browser-closed recovery, remote MKATUSHONOK own local chats, multi-iframe robust lease remain future tasks.

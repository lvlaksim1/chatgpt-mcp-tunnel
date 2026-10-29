# ChatGPT MCP Tunnel v0.2.18 — MCP Apps cache refresh

The v0.2.17 executable and server worked. Live acceptance in ordinary ChatGPT exposed that the existing `ui://chatgpt-mcp-tunnel/relay` resource could still render the 0.2.16 HTML even after the plugin tools refresh and new ChatGPT conversation. The installed server's `ChatRelayApp.html` contained the new `target:needsNew` implementation, but the displayed widget kept the old status wording.

This patch assigns a new, versioned resource URI `ui://chatgpt-mcp-tunnel/relay-v0218`, updates the MCP Apps resource registration, package version and protocol test. The stable resource URI for each future UI revision should be versioned; altering only the server HTML does not guarantee that ChatGPT will refresh a cached iframe.

Acceptance: new `chat.ui.open` tool UI metadata and `resources/read` must advertise `relay-v0218`, then in ordinary ChatGPT open the app, start relay and send a first message from a new local chat. Confirm `ui/message` with target `new` creates a new ChatGPT thread, the new thread calls `chat.ui.open(chat_id=...)`, and replies reach the correct local chat via `chat.reply`. Both normal and new-chat sending remain MCP-only.

# Chat attachments and clipboard images — 29.10.2026

Authority: direct owner directive in ordinary ChatGPT, 29.10.2026.
Goal: attach multiple files and clipboard screenshots to existing chat window, read them through MCP without adding a new OpenAI tunnel or paid API.

Decision: preserve existing user-session chat queue and existing WSS Router Lite. Copy files under the current user's LocalAppData to per-message/per-attachment opaque IDs. Only after all copies and hashes finish, atomically publish the JSON message with names, media type, size, and SHA-256. No binary payload in message JSON.

GUI: attach picker with multi-selection, drag-and-drop from Explorer, Ctrl+V image into PNG, preview cards with remove, text-or-attachments-only send; 20 files and 2 GiB per file; async copy outside UI thread.

MCP: chat.receive now lists attachment metadata; chat.attachment.read validates GUID IDs, stored size, SHA-256 and returns actual ImageContentBlock for images scaled to 1600 pixels, bounded text for plain UTF-8, or locally resolved path for binary formats. chat.reply preserves attachment metadata.

Security: local plaintext owned by the Windows user. No external file server or cloud model API; the model receives a file only on an active authorized MCP tool call. Existing TLS, client key, and OpenAI tunnel unaffected.

Evidence: GUI isolated selftest, 49-tool MCP stdio test, attachment E2E PNG and text/integrity tests, Router Lite all 23 remote tools and reconnect PASS on development source. Release CI and physical installed UI acceptance remain separate pending gates. The ChatGPT plugin tool schema can remain stale and require refreshing.

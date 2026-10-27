# Project architecture

Current verified deployment (28 October 2026):
`ChatGPT custom MCP plugin → OpenAI Secure MCP Tunnel → official tunnel-client v0.0.18 → ChatGptMcpTunnel.dll (.NET 8, stdio MCP)`.

- Source: `src/ChatGptMcpTunnel/` (official ModelContextProtocol C# SDK).
- Entry: `scripts/Start-Tunnel.ps1`, optional `.cmd` launcher.
- Windows deployment root: `D:\ChatGPT-MCP-Tunnel`.
- Volatile client binaries and profiles: repository-local `.tools/`, ignored by Git.
- User-scoped encrypted credential and diagnostic state: `%LOCALAPPDATA%\ChatGptMcpTunnel`.
- First built-in tool: `bridge_ping`, read-only and without computer access.
- ChatGPT tool invocation confirmed with `CHATGPT_MCP_TUNNEL_PING_OK`, with local `/readyz` = `ready`.
- Do not import or depend on any code from a separate desktop project.

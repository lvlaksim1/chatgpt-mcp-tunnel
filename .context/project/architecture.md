# Project architecture

Verified deployment (28 October 2026):
`ChatGPT custom MCP plugin → OpenAI Secure MCP Tunnel → official tunnel-client v0.0.18 → ChatGptMcpTunnel.dll (.NET 8, stdio MCP)`.

- C# MCP server: `src/ChatGptMcpTunnel/Program.cs` (official ModelContextProtocol 2.2.0 SDK).
- Windows MCP tools: `src/ChatGptMcpTunnel/ComputerTools.cs` with `system.info`, `fs.list`, `fs.read`, `fs.write`, `fs.search`, `shell.exec`, `process.list` and `process.kill`; plus `bridge_ping`.
- Windows deployment root: `D:\ChatGPT-MCP-Tunnel`; `runner/start.cmd`, `runner/stop.cmd`, `runner/enable-autostart.cmd`, `runner/disable-autostart.cmd`.
- Scheduled task `ChatGPT MCP Tunnel`: starts at Windows sign-in after 20 seconds under DOMKAT\\Maksim (no administrator privilege required).
- Local service dashboard and probes: `http://127.0.0.1:55198/ui`, `/healthz`, `/readyz`.
- Volatile client binaries/profiles: repository-local `.tools/`, ignored by Git. Secret stored encrypted using Windows DPAPI under `%LOCALAPPDATA%\ChatGptMcpTunnel`.
- Windows E2E: `tests/test_windows_tools.py` passed on DOMKAT for all eight new tools. MCP stdio smoke `tests/test_mcp_stdio.py` passed (9 tools).
- After server restart ChatGPT custom plugin metadata may still be cached. Refresh the tool listing in ChatGPT plugin management and begin a new conversation for direct calls; do not mistake the prior-cached `bridge_ping`-only catalog for a server defect.
- Never import or depend on code from the separate ChatGPT Desktop Local Bridge project.
- The owner explicitly chose not to add a separate owner-authentication or approval framework at this stage. Do not silently reintroduce one.

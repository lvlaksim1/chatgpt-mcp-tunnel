# ChatGPT MCP Tunnel v0.2.63 — DOMKAT server MCP VPN switching

31 October 2026. Project-manager. Owner confirmed: only the DOMKAT **server** needs a ChatGPT control-plane MCP outbound VPN selector. Remote Router Lite **clients** connect to DOMKAT with WebSockets and do not run their own tunnel-client.

## Native capability discovered

The installed official `tunnel-client.exe` v0.0.18 supports:
- `run --http-proxy <url>`: one outbound HTTP proxy for control-plane, MCP and Harpoon.
- `--control-plane.http-proxy`, `--mcp.http-proxy`, `--harpoon.http-proxy`, and `--proxy.check-interval`.

Rather than repeatedly restart tunnel-client, v0.2.63 points its `run` operation to a **stable local proxy** at `http://127.0.0.1:11827` and allows the operator to change the upstream without creating an additional MCP tunnel.

## Implementation

- `src/ChatGptMcpProxyBridge/`: self-contained .NET 8 Windows x64 companion, bound only to loopback `127.0.0.1:11827`. It forwards opaque HTTP CONNECT tunnels without terminating their TLS or logging their destinations/keys. Non-CONNECT requests are rejected with HTTP 405, oversize headers 431.
- On each connection, reads `%LOCALAPPDATA%\ChatGPTMCPAgent\Vpn\network-mode.json`:
  - `happ` (default): forwards to Happ local HTTP proxy `127.0.0.1:10809`.
  - `free`: forwards to the embedded Xray HTTP proxy `127.0.0.1:11821` and can locally launch the DPAPI-protected Free-Reality-Auto Xray profile if needed, after SHA256 validation of the bundled Xray.
  - Invalid/unavailable: fail closed (502/503), **no direct bypass**.
- An internal 500ms monitor terminates old persistent bridge sockets on mode change so Go's client reconnects over the selected new route without restarting its process.
- User-local `%LOCALAPPDATA%\ChatGPTMCPAgent\Vpn\bridge-health.json` records only nonsecret local connection counts, latest mode and timestamp. No hostnames from control-plane traffic, URI credentials or TLS payloads are recorded.
- `installer/ServerStart.ps1` is used exclusively for the **server** role: starts or safely adopts its own bridge on port 11827, refuses a foreign listener, and launches **the same official tunnel-client** with `--http-proxy http://127.0.0.1:11827`.
- The remote `ChatGptMcpTunnel.exe --agent` client role and RouterLite WebSocket implementation are **unchanged**.
- `scripts/Build-LocalRelease.ps1` packages bridge binary with existing GUI v0.2.63 and Xray v26.10.29 SHA256/license, building both local Setup and Update installers. No GitHub Actions.

## Lab acceptance

- Isolated bridge `11837` HAPP → FREE → HAPP while retaining a single bridge PID:
  1. Happ HTTPS request example.com → HTTP 200.
  2. Free Xray VLESS/REALITY → example.com HTTP 200.
  3. Return to Happ → example.com HTTP 200.
- Compiled .NET bridge 0 errors; v0.2.63 bridge `--selftest` exit 0.
- `test_vpn_mode_switch.py`, `test_vpn_pilot_isolation.py`, `test_tts_without_microphone.py` and `test_primary_webview_ui.py` pass.
- Final v0.2.63 complete Setup/Update packages rebuilt with newest bridge and SHA256 manifest at `.tools/local-release/v0.2.63/dist`.

## Live DOMKAT activation — successful

- Detached rollback-ready cutover: `.tools/activate-server-mcp-proxy-v0263.ps1`.
- Status: `.tools/activate-server-mcp-proxy-v0263-status.json` = `verified`.
- Installed GUI v0.2.63.0; bridge PID **28024**, tunnel-client PID **8624**, MCP runtime PID **33264** (parent 8624), GUI PID **5060**. Happ PID 9908 was preserved.
- Official live client command contains `--http-proxy http://127.0.0.1:11827`.
- Live bridge metric after cutover: `mode=happ`, `successfulUpstreamConnections=5`, one simultaneous established proxy socket at sample time. This proves actual native MCP outbound traffic used the bridge, not merely that the flag was present.
- The connector returned `CHATGPT_MCP_TUNNEL_PING_OK` after reconnecting.
- Router Lite `computers.list`: DOMKAT (local) and MKATUSHONOK (connected remote client). No client-side changes.
- Scheduler `ChatGPTMCP-FreeReality-Refresh` still runs every 3h; next 31 Oct 19:28 MSK, last result 0.

## Backup, limitations and next steps

- Backup pre-cutover `.tools/deployment-backups/pre-v0263-mcp-proxy` contains former GUI, theme, ServerStart. The detached cutover script restores the original launcher on failed client readiness.
- **Current mode remains Happ**, not a forced migration of the user's authenticated ChatGPT session to a public unknown-operator proxy.
- In the installed GUI, choosing Free with the explicit trust warning sets the same preference that the standalone bridge reads and routes to Free Xray; switching back chooses Happ. Changes to an existing TCP CONNECT session trigger reconnect.
- Real user-authenticated MCP tools in Free mode were **not** exercised against an unknown public VPN provider. The full server routing mechanism and free outbound were tested independently, but full Free-mode authenticated ChatGPT acceptance remains outstanding.
- A scheduled free-node update changes the encrypted next-use profile; a running Xray subprocess is not yet dynamically hot-reloaded when the selected free node changes.
- Automatic failback to Happ after consecutive FREE remote failures (with user notification) is not yet implemented. Current mode fails closed; user can manually return to Happ via GUI or tray.
- Do not claim the entire Windows OS is on embedded VPN. The browser and official MCP outbound path are controlled, while Router Lite client WebSockets keep their direct LAN/WAN connections to DOMKAT.
- No extra MCP tunnel, no Happ shutdown, no VPN system adapter/routing changes, no reset/commit of existing uncommitted source.

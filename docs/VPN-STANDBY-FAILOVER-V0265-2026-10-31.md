# ChatGPT MCP Tunnel v0.2.65 — automatic free-node failover and UI notifications

31.10.2026, DOMKAT. This change builds on installed v0.2.64 and preserves **one original signed-in WebView2 profile**, one official MCP tunnel-client, the Router Lite network of client PCs, and Happ as fallback.

## Implemented

- `scripts/Vpn/free_reality_updater.py` now seeks up to **three independently validated** VLESS/TCP/REALITY nodes per refresh (one primary, up to two backups), up to the configured 16 probes. Only nodes that pass HTTPS via Xray are retained. Backups are encrypted with Windows CurrentUser DPAPI in the same user-local private directory as the primary; only nonsecret public metadata enters `status.json`.
- Files: `%LOCALAPPDATA%\ChatGPTMCPAgent\Vpn\profiles\Free-Reality-Auto.dpapi`, `Free-Reality-Standby-1.dpapi`, and `Free-Reality-Standby-2.dpapi`. Never commit or disclose these files, browser auth data or raw `vless://` links.
- `src/ChatGptMcpProxyBridge/Program.cs`: after three failed reachability checks of the active free exit, independently test a DPAPI-encrypted standby at loopback port **11822**. If it passes, replace the failed owned Xray on **11821** with the selected standby and reconnect existing TCP tunnels without restarting the official MCP `tunnel-client.exe`. Remain in `free`. If no available backup passes, atomically revert to Happ, drop stale transport connections and record a user notice. No silent direct route.
- If the three-hour updater supplies a newly validated primary while the bridge is running on a backup, it may be prevalidated and selected under the existing Xray profile hot-reload path.
- `src/ChatGptMcpAgent/Program.cs`: TrayContext polls `network-notice.json` and displays a Windows balloon for `free-node-rotated` and `free-route-health-failed`. Notices are limited to fresh events and do not expose VPN provider credentials or endpoint URLs. Existing GUI browser-mode synchronization handles external return to Happ.
- No changes to system VPN, Happ, official MCP server endpoint/credentials, or Router Lite remote agent role. Browser user data stays at `%LOCALAPPDATA%\ChatGPTMCPAgent\WebView2\ChatGPT`.

## Tests before installation

- Python offline updater DPAPI test: **6/6 PASS**, including encrypted standby file and survival of profile after feed outage.
- DOMKAT Windows Scheduled Task `ChatGPTMCP-FreeReality-Refresh` triggered manually: exit **0**; **three healthy nodes** found after eight tested candidates, selected primary plus two DPAPI-protected backup files.
- Isolated Xray/MCP-proxy fault simulation using one test bridge (`--port 11838`) with forced health failure of primary: **PASS**. Bridge emitted `free-node-rotated`, stayed in mode `free`, switched to pretested backup and `api.openai.com/v1/models` returned expected unauthenticated HTTP 401. Production official MCP PID **8624** unchanged and production mode remained Happ.
- Five-cycle isolated network stress test: **15/15** HTTPS requests Happ->Free->Happ through one test bridge process. Production `tunnel-client.exe` and primary server bridge processes never restarted.
- .NET Bridge and GUI source compile pass; 7 Python regression scripts pass.
- Release bundles include verified Xray SHA256 and its license.

## Acceptance boundaries

- Testing a controlled **simulation** of a failed primary is not identical to an actual third-party provider outage lasting hours. Long-term free-server reliability is not guaranteed.
- Public free nodes remain untrusted network intermediaries; GUI consent warning remains mandatory. TLS encrypts ChatGPT payloads, but destination metadata can be observed by operator.
- The first movement from Happ to Free still uses GUI explicit consent and the **same authenticated ChatGPT browser profile**. On rescue to a free standby, proxy port remains 11821 and no browser profile is changed.
- Existing user browser backup should stay local and never be committed; remove only obsolete laboratory folders after acceptance.
- GitHub publication to be made only after local install, live MCP ping, GUI/auth check, and sensitive-file audit.

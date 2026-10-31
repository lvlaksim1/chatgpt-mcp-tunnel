# DOMKAT VPN E2E acceptance — v0.2.63, 31 October 2026

## Scope and authorization

Owner requested the remaining verification of switching Happ versus automatically discovered public VLESS/REALITY on **DOMKAT**. The official MCP client and the built-in ChatGPT WebView2 are server-side; Router Lite remote clients keep existing direct WebSocket access to DOMKAT. The public exit is of unknown operator; no credentials were shown or copied into Git.

## Observed results

### Real server MCP transport through free Xray

- Local selected free VLESS/REALITY node was fresh; loopback Xray HTTP port 11821 produced HTTP 200 for `https://example.com`.
- A dedicated detached rollback guard temporarily wrote the free network mode and subsequently restored the original Happ mode.
- A first autonomous trial (18:44 MSK) captured `bridgeObservedFree=true`, one live `tunnel-client.exe` TCP socket into bridge port 11827, `api.openai.com/v1/models` HTTP 401 through free proxy (expected for unauthenticated request), `/readyz` HTTP 200, unchanged MCP client PID 8624 and bridge PID 28024, restored Happ example.com HTTP 200. One initial free `example.com` request failed transiently, so first trial did not pass all checks.
- Second **autonomous** trial (18:47 MSK) PASS all criteria: mode `free` reached in actual bridge, `tunnel-client.exe` socket established via 11827, free HTTPS example.com HTTP 200, free API OpenAI HTTP 401, official local MCP /readyz HTTP 200, unchanged bridge/client PIDs, Happ restored to HTTP 200, no errors. Raw evidence: `.tools/vpn-mcp-autonomous-acceptance-report.json`.
- The user's live ChatGPT MCP connector also returned `CHATGPT_MCP_TUNNEL_PING_OK` and `computers_list` during the free UI switch. Concurrent bridge metric showed `mode=free`, `successfulUpstreamConnections=67`, official client PID 8624, with DOMKAT and remote MKATUSHONOK connected.

### Real GUI and WebView2

- Isolated installed WebView2 test `--test-vpn-webview` on Xray 11821 returned exit **0**, loaded `example.com`.
- Through actual GUI v0.2.63 topbar selection **VPN: Happ -> Бесплатные серверы — Xray**, accepted the unknown-provider warning. The window switched to **VPN: Бесплатные** and loaded `chatgpt.com` showing its guest login screen (separate browser user-data folder).
- Thus the free browser route is functional, but **authenticated ChatGPT login within the separate free browser profile was not performed**. No user password or OAuth tokens were requested.
- The external rollback guard briefly caused a UI mismatch: it changed disk mode back to Happ while the open WebView2 still showed `VPN: Бесплатные`. The GUI's event handler currently compares the requested mode to the persisted file and does not react to external changes. Restored properly by triggering native GUI switch with a synchronized mode value; screenshot confirmed **VPN: Happ**, already authenticated ChatGPT Plus account with original chats.
- At 18:57 MSK, actual mode `happ`, bridge metric `happ`, version 0.2.63.0, GUI PID 5060, official client PID 8624, runtime PID 33264, bridge PID 28024. Free 11821 listener count zero; scheduler task remains active, LastTaskResult 0.

### Fail-closed test

- Separate bridge instance on **11839**, leaving production 11827 unchanged. Invalid mode caused HTTP CONNECT rejection **503** (no silent direct bypass). Re-selecting Happ on the same isolated process then yielded HTTPS **200**. Script: `.tools/test-bridge-failclosed-isolated.ps1`. Result `invalidModeRejected=true, happWorksAfter=true`.

## Conclusion

**ACCEPTED**: server-side mode switch Happ vs Free Reality, real outbound MCP through the bridge, unchanged official tunnel-client and Router Lite connected client, HTTPS reachability, WebView2 free guest page, native GUI switch Happ -> Free -> Happ, and fail-closed unknown-mode behavior.

**NOT YET ACCEPTED / NOT YET IMPLEMENTED**:
1. Authenticated chat conversation with the separate **Free** WebView2 user profile. It requires user authorization/login in the isolated profile; no secrets transferred.
2. **Automatic** switch from a failed public Free exit back to Happ with user-facing status. Current bridge fails closed on missing/invalid exit. The isolated test validates failure isolation, **not** recovery automation.
3. Hot-reload or active switching of already-running Xray when scheduled FreeReality node changes. Scheduler updates DPAPI on disk; running Xray is not yet automatically replaced.
4. Browser-MCP synchronization for an externally triggered failover. After a file-only rollback, an open WebView2 retains its original proxy/browser environment and may show stale mode until a GUI-managed window recreation.
5. Long-term uptime, privacy and trust of free community nodes are not established. The initial transient example.com failure is evidence of volatility.

No GitHub release published by this test. Installed version and existing MCP service preserved throughout; latest state Happ restored.

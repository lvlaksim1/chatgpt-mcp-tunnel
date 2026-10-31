# DOMKAT ChatGPT MCP Tunnel v0.2.64 — unified authorized WebView2 profile and automatic VPN recovery

31 October 2026, project-manager. Owner requirement: in Free Reality VPN mode use **the one existing signed-in ChatGPT browser profile**; do not create a separate browser user data folder. Continue work on failback and active VLESS node rotation.

## Delivered and installed

- `src/ChatGptMcpAgent/VpnModeController.cs`: `BrowserProfileFolder(string existingProfile)` returns the *unchanged original* profile directory, always `%LOCALAPPDATA%\ChatGPTMCPAgent\WebView2\ChatGPT`.
- No new `WebView2-FreeReality` folder is created. The legacy v0.2.63 free profile folder, if present, remains untouched and is **not used**. No credentials or cookies copied or rewritten.
- A single original user data folder is compatible with both Happ and Free Reality as long as WebView2 is **fully closed** between different environment proxy parameters. `ChatGptMainForm.cs` subscribes to `CoreWebView2Environment.BrowserProcessExited`; `Program.cs` awaits the event before recreating the GUI with the changed proxy settings. This replaces unsafe fixed 900ms delay.
- Explicit GUI free-mode selection validates the selected node by issuing an unauthenticated HTTPS request through the already-running MCP outbound bridge. Only after success will the browser be recreated. `VpnModeController.IsTransitioning` suppresses races with the GUI timer during this asynchronous check.
- Server `ChatGptMcpProxyBridge.exe`, unchanged official single `tunnel-client.exe`, and Router Lite clients are separate from WebView2.

## Automatic recovery and active free node replacement

- The DOMKAT proxy bridge polls active mode, then checks authenticated *transport reachability* every approximately 20s while Free Reality is selected (HTTPS to `api.openai.com/v1/models` expecting 401/403/200 without an API key).
- On three consecutive check failures, it atomically selects Happ, closes old proxy connections so official tunnel-client reconnects, stops only its own free Xray, and writes `%LOCALAPPDATA%\ChatGPTMCPAgent\Vpn\network-notice.json`.
- The GUI periodically compares its active browser mode with the saved mode; if an external watchdog has changed it, the GUI closes the browser, waits for the WebView2 browser process to exit, and reopens on the original authorized profile with Happ. The visible selector is updated.
- When the three-hourly free node subscription refresh changes the encrypted DPAPI Xray profile, the bridge first validates the new candidate on separate loopback test port **11822** and performs an actual HTTPS probe. Only after this passes does it stop the old owned Xray and start the new selected profile on the standard free port **11821**. If the test fails, the previously working node stays active rather than being silently replaced by a broken one.
- No OS-wide VPN settings changed; no second tunnel-client process; no changes to Router Lite client roles.

## Verified tests

### Pre-install isolated tests

- GUI source compile .NET8 Windows x64 PASS; bridge compile PASS.
- Python regressions PASS: `test_vpn_mode_switch.py`, `test_primary_webview_ui.py`, `test_vpn_pilot_isolation.py`, `test_free_reality_updater.py` (six cases), `test_tts_without_microphone.py`, and `test_vpn_unified_profile_and_watchdog.py`.
- GUI packaged tests `--selftest`, `--test-main-chat-ui`, `--test-toolbar-dom`, `--test-webview-network`, `--test-voice-dom` all exit 0; bridge `--selftest` exit 0.
- Isolated proxy Happ -> Free Xray -> Happ, three real HTTPS 200 results with one bridge PID.
- Forced test-only Free health failure: automatic Happ fallback succeeded, user notice persisted, HTTPS 200 through Happ after failback, production bridge untouched.
- DPAPI profile update test: test-only Xray child PID changed `35640 -> 26492`, new node prevalidated, `api.openai.com` returned expected HTTP401 after hot reload, existing official tunnel-client PID8624 untouched.

### Local deployment

- First install attempt failed because local PowerShell deploy script used the automatic read-only variable `$PID` as a function parameter. Prior GUI v0.2.63 and prior bridge were restored manually and MCP ping succeeded; **no user-auth data was lost**. Corrected script tested before reapplying.
- Second, corrected autonomous deployment ended status `verified`, `.tools/deploy-v0264-unified-vpn-retry-status.json`.
- Current `D:\ChatGPTMCPAgent\ChatGptMcpAgent.exe` v0.2.64.0, GUI PID7752, bridge PID38032, existing `tunnel-client.exe` PID8624, runtime PID33264.
- Browser auth profile backup retained at `.tools/deployment-backups/pre-v0264-single-profile-retry/AuthorizedWebView2ChatGPT` (and the first-backup directory), around 219 MB, not in Git. Original user data folder stays active.
- Local Setup and Update installers rebuilt after final race fix and all three SHA256 verified: `.tools/local-release/v0.2.64/dist`.

### Installed, real user GUI E2E

1. GUI opened in Happ with ChatGPT **signed in Plus**, original chats/history visible.
2. Selected Free Reality from the titlebar and accepted the public proxy trust warning.
3. New GUI environment loaded `chatgpt.com` **already signed in Plus with the same chat history**. No second login or separate browser UDF. Current mode and bridge metric both showed `free`.
4. `bridge_ping` returned `CHATGPT_MCP_TUNNEL_PING_OK`; Router Lite computers list continued to show DOMKAT local + MKATUSHONOK remote connected; the original tunnel-client PID8624 persisted.
5. Selected Happ from the same titlebar; GUI again showed signed-in ChatGPT Plus with original history, mode and bridge both `happ`, free Xray listener 11821 stopped.
6. Repeated free selection and simulated an **external watchdog fallback** by atomically changing only the mode file to Happ. The installed GUI **automatically** recreated WebView2, displayed `VPN: Happ`, and retained the same signed-in ChatGPT Plus account and history. Same client/bridge/runtime PIDs.

## Final state and boundaries

- DOMKAT in Happ mode with authorized original WebView2 profile and active MCP transport.
- Free mode supported with trusted-to-operator public node only after warning; TLS protects HTTPS contents but proxy provider can observe destination/metadata.
- Real catastrophic/public-VPN outage was **not deliberately induced on the production path**; failback logic was exercised by isolated forced-failure test and the GUI's external-mode sync by a controlled real-app test.
- There is a short reconnect interval when swapping free nodes. Continuous-zero-packet-loss VPN is not claimed.
- Profile backup contains sensitive browser data; do not upload to GitHub or share publicly. Existing unused v0.2.63 separate free browser folder was intentionally not deleted without explicit request.
- GitHub release not published as part of acceptance; local installer is available.

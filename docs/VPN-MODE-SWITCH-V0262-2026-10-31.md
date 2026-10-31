# VPN mode switching — ChatGPT MCP Tunnel v0.2.62

31.10.2026. Owner requested UI choice between the existing Happ connection and automatically discovered free VLESS/REALITY servers.

## Implemented and installed

- Two-choice switch in the BrowserChrome title bar: VPN: Happ / VPN: Бесплатные. The tray menu also exposes both choices.
- Happ remains the default; original WebView2 profile, settings and system VPN are left unchanged.
- Free mode uses the most recently verified profile maintained by the existing three-hourly Windows Scheduler, from a CurrentUser DPAPI-encrypted profile under LocalAppData.
- A free-mode switch requires an explicit consent dialog because public nodes are operated by unknown third parties.
- Source: src/ChatGptMcpAgent/VpnModeController.cs and ChatGptMainForm.Vpn.cs; wired into ChatGptMainForm.cs, Program.cs, BrowserChrome.html.
- Binary: bundled official Xray-core v26.10.29 with SHA256 check and license under the installed Vpn subfolder.
- Free proxy binds only 127.0.0.1:11821. If the Xray binary, valid recent DPAPI profile or local port is unavailable, the free switch fails, rather than silently rerouting direct.
- New mode requires recreating the WebView2 GUI form; TrayContext keeps the same MCP tunnel-client and runtime running. Separate WebView2 browser profiles avoid conflicting Chromium proxy flags. First free-mode login may need separate authorization.
- The tray mode menu allows returning to Happ even if the in-page ChatGPT browser cannot load.
- Free Xray is stopped on switching back to Happ; no global Windows route is changed and Happ is not turned off.
- IMPORTANT: switching currently affects ChatGPT WebView2; the official MCP client transport has NOT been switched to free Xray. This needs further investigation of control plane/QUIC behavior. Existing transport is deliberately preserved.
- Updating the list every three hours refreshes the encrypted selection, but it does not yet hot-reload an already-running Xray subprocess. The new selection applies when the GUI mode is restarted.

## Deployment and verification

- Installed GUI is v0.2.62.0 on DOMKAT.
- Deployment script: .tools/deploy-v0262-vpn-safe.ps1; result verified. Backup: .tools/deployment-backups/pre-v0262-network-mode.
- MCP runtime original PID 28372 and client PID 24884 were both preserved.
- Setup, Update and compatibility Update installers plus verified SHA256 manifest: .tools/local-release/v0.2.62/dist.
- GUI compile PASS, packaged tests selftest, main-chat-ui, toolbar-dom, voice-dom, webview-network all PASS with exit 0.
- Regression tests: tests/test_vpn_mode_switch.py, test_vpn_pilot_isolation.py, test_tts_without_microphone.py, test_primary_webview_ui.py PASS.
- Installed inline BrowserChrome JavaScript parsed with node --check PASS.
- Additional isolated live ChatGPT smoke test returned exit code 103 and remains NOT ACCEPTED. Real authenticated switching between Happ and free WebView2 is a remaining manual acceptance test. Do not claim full end-to-end success yet.

## Further action

Confirm visible UI selector, test free login and actual chat only after explicit provider trust decision, test return to Happ and consecutive switches, and separately research MCP client's network transport and secure failover. Do not use --shutdown, stop Happ, modify OS routing or launch a duplicate MCP tunnel. Keep local sources without discarding existing uncommitted changes.

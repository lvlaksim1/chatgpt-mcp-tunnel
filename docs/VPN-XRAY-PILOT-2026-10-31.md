# ChatGPT MCP Tunnel — isolated Xray-core VPN pilot

Date: 31 October 2026. Owner requirement: work toward independence from paid Happ, without resetting Windows proxy, breaking the active ordinary ChatGPT, or creating a second MCP tunnel.

## Installed pilot assets

- **Engine:** Official XTLS/Xray-core **v26.10.29**, GitHub release tagged latest / prerelease=false in API on 31 October.
- **Asset:** `Xray-windows-64.zip`
- **Verified archive SHA-256:** `fce33e81cccf50e7aab572b175519a50b5e0f9d2f5c8d1ed73012ea5c9ad4d58`.
- **Binary SHA-256:** `07a332b714a3c0eee047a22a9c0ea749b64df33e4e1ff6d28a8c7f9ef943ce10`.
- **Location:** `D:\Dev\ChatGPT-MCP-Tunnel\.tools\vpn-pilot\xray-v26.10.29\xray.exe`
- **Publisher/licensing:** XTLS/Xray-core, MPL-2.0. Package as separately attributed component if a release is prepared.
- This directory is an isolated developer pilot, **not** an installed production VPN.

## Network test matrix

| Test | Route | Result |
|---|---|---|
| Xray local HTTP listener | `127.0.0.1:11809` | OK |
| curl HTTPS example.com | Xray → direct | HTTP 200 |
| curl HTTPS chatgpt.com | Xray → direct | timeout |
| Isolated WebView2 example.com | Xray 11809 → direct | 27s navigation timeout (not passed) |
| Xray chained listener | `127.0.0.1:11810` → Happ SOCKS 10808 | OK |
| curl HTTPS example.com | Xray 11810 → Happ | HTTP 200 |
| curl HTTPS GitHub API | Xray 11810 → Happ | HTTP 200 |
| curl HEAD chatgpt.com | Xray 11810 → Happ | HTTP 403 challenge; TLS/HTTP round-trip works, not a ChatGPT login test |
| **Actual isolated WebView2 example.com** | Xray 11810 → Happ | **PASS, exit 0** |
| Actual isolated WebView2 with no listener | port 11999 | Correct failure, exit 81 |
| Loopback client/server VLESS HTTP-to-HTTPS | local Xray :11813 → VLESS to local Xray server :11812 → public example.com | HTTP 200, process pair terminated by test |
| Tunnel-client via Xray | not switched | **NOT TESTED / NOT ACCEPTED** |
| Independent remote VLESS+REALITY to ChatGPT | no remote server yet | **NOT TESTED / NOT ACCEPTED** |

**Important:** A local VLESS loopback test uses a trusted localhost link with no transport encryption. It does **not** prove secure remote REALITY operation or independence from Happ's operating-system routing.

## Changes

- `scripts/Vpn/Install-XrayPilot.ps1`: official binary retrieval and checksum verification; no installs or service registration.
- `scripts/Vpn/Start-XrayPilot.ps1` / `Stop-XrayPilot.ps1`: controlled port-bound isolated child processes, per-port state and verified executable ownership, safe to re-run.
- `scripts/Vpn/Test-XrayVlessLoopback.ps1`: self-contained local VLESS client/server test; removes child processes.
- `src/ChatGptMcpAgent/VpnWebViewPilotTests.cs`: isolated user-data directory for true WebView2 network test; does not reuse the real user's WebView2 profile.
- `src/ChatGptMcpAgent/Program.cs`: `--test-vpn-webview` entry point.
- `src/ChatGptMcpAgent/WebViewNetworkProxy.cs`: **explicit opt-in only** for the new process via `CHATGPT_MCP_WEBVIEW_PROXY_PORT`; missing explicit proxy rejects rather than silently reverting to Happ/direct. Default Happ selection unchanged.
- `installer/ServerStart.ps1`: future **opt-in** `CHATGPT_MCP_TUNNEL_PROXY_PORT` sets process-local HTTP(S)_PROXY and NO_PROXY for the official tunnel-client. Its full routing behavior is **not verified**, especially possible QUIC/UDP.
- `tests/test_vpn_pilot_isolation.py`: non-invasive static regression test.

No installed `D:\ChatGPTMCPAgent\ChatGptMcpAgent.exe` or live `ServerStart.ps1` was changed. No VPN profiles/keys were extracted from Happ. No system routes, proxy settings, Windows Firewall, VPN/TUN adapters, UAC, or services were modified.

## Reproducing the pilot

From the source root:

```powershell
.\scripts\Vpn\Install-XrayPilot.ps1
.\scripts\Vpn\Start-XrayPilot.ps1 -Mode direct -Port 11809
.\scripts\Vpn\Start-XrayPilot.ps1 -Mode via-happ -Port 11810
.\scripts\Vpn\Test-XrayVlessLoopback.ps1
.\scripts\Vpn\Stop-XrayPilot.ps1 -Port 11810
.\scripts\Vpn\Stop-XrayPilot.ps1 -Port 11809
```

The test GUI is compiled from source and may be run from an isolated build output:

```powershell
$env:CHATGPT_MCP_VPN_PILOT_PORT='11810'
& '.\src\ChatGptMcpAgent\bin\Release\net8.0-windows\win-x64\ChatGptMcpAgent.exe' --test-vpn-webview
Remove-Item Env:CHATGPT_MCP_VPN_PILOT_PORT
```

The currently installed production GUI must not be restarted just to test a different proxy.

## Exit criteria for next release

1. Obtain a trusted remote endpoint (owned VPS or approved compatible server); no unknown public proxy or free shared keys.
2. Produce a **secured** VLESS/REALITY configuration using current Xray `realitySettings.password` (not the older `publicKey` alias) and keep credentials outside Git in protected per-user settings.
3. Show true ChatGPT authenticated WebView2 login and chats with Happ **not serving the app's traffic** and controlled rollback; do not turn off the existing Happ while testing.
4. Independently confirm the official `tunnel-client.exe` network path (including any Cloudflare/QUIC companion) really traverses the chosen outbound. Environment proxy hints alone are insufficient.
5. Implement a user-facing mode selector `Happ / Embedded Xray / Direct` with health checks, protected profiles, clear error states, no silent direct fallback; only then package an official v0.2.62+ update.
6. Preserve Windows 10, the one existing MCP tunnel, ordinary ChatGPT mode without Work/Codex/API, and all uncommitted source changes.

## Official references
- https://github.com/XTLS/Xray-core/releases
- https://xtls.github.io/en/config/inbound.html
- https://xtls.github.io/en/config/outbounds/vless.html
- https://xtls.github.io/en/config/transports/reality.html
- https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/webview-features-flags

Microsoft cautions that WebView2 Chromium flags are for testing and are not guaranteed for long-term production. Revisit the app-scoped proxy approach during production hardening.

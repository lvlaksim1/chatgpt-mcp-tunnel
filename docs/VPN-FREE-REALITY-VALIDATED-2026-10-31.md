# Free VLESS/REALITY exit — isolated test, 31 October 2026

Owner request: find a free VLESS/REALITY link as an alternative to paid LamaVPN and the Happ app. This is a **public, unknown-operator experimental exit**. Do not silently route an authenticated ChatGPT session, passwords or sensitive user traffic to this node.

## Source

Public GitHub subscription repository:
https://github.com/flaafix/AetrisVPN-white-list-lite

Current public subscription document:
https://raw.githubusercontent.com/flaafix/AetrisVPN-white-list-lite/main/AetrisVPN.txt

Another source checked but not accepted:
https://raw.githubusercontent.com/0xRadikal/Free-v2ray-Configs/main/verified/configs.txt

## Local verified candidate

- Full public VLESS URI (not committed into repository):
  `D:\Dev\ChatGPT-MCP-Tunnel\.tools\vpn-pilot\free-public-reality-verified.txt`
- Protocol VLESS / TCP / REALITY, XTLS Vision.
- Target remote endpoint: `147.45.245.177:443`. Egress observed `77.110.116.110`.
- Date observed: 31 October 2026, approximately 14:30 MSK.
- Source scan: 43 usable VLESS/TCP/REALITY configs in AetrisVPN feed, seventh tested candidate succeeded. The first six returned curl timeouts or SSL errors. An earlier 16 candidates from 0xRadikal did not pass this PC's HTTPS test.
- Xray-core v26.10.29; ephemeral local proxy port 11834.
- Xray HTTP proxy -> `https://example.com`: HTTP 200, curl exit 0.
- External IP from `https://api.ipify.org`: `77.110.116.110`.
- Compared to Happ local proxy HTTP 127.0.0.1:10809; **egress IP is different**.
- Modified importer to accept omitted standard URI defaults `type=tcp`, `encryption=none`. No change to current installed GUI.
- End-to-end test using source `scripts/Vpn/Import-VlessReality.ps1` into test-only DPAPI folder, then `Start-ProtectedXrayProfile.ps1` at port 11826 succeeded. Separate compiled GUI executed `--test-vpn-webview` with isolated user profile and returned exit 0.
- Test Xray stopped through `Stop-ProtectedXrayProfile.ps1`, temporary private test profile removed. Active Happ, MCP tunnel-client and GUI remain untouched.

## Security and next stages

Public nodes often expire and have unknown operators. TLS to example.com protects website payload but the proxy provider still observes destination/metadata. Using this server for an authenticated ChatGPT session is **not recommended** without a trust decision. There is no long-term stability or privacy guarantee, and a single successful HTTPS test cannot prove reliability.

The Xray process is excluded from Happ TUN according to the local Happ process-routing rules. An independent exit IP, distinct from Happ, was observed. This confirms a separate public exit but not every aspect of route isolation.

If user opts into using public free nodes, introduce explicit experimental-only profile selection and never default-route sensitive browsing without notice. Maintain a dedicated fallback to existing trusted Happ and avoid live MCP-client restarts.

Use a trusted remote server for stable independent production VPN. Treat the public URL as an ephemeral trial candidate, not a permanent provider subscription.

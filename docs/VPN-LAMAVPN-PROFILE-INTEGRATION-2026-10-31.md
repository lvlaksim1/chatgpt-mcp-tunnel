# LamaVPN -> Embedded Xray-core: integration status (31 October 2026)

## Owner authorization and safety

Owner asked to reuse the currently paid LamaVPN access in the embedded ChatGPT MCP Tunnel Xray engine, keeping Happ and the existing MCP connection active. No secret subscription URLs, UUIDs, REALITY passwords, access tokens, or decrypted profiles should ever be placed in GitHub or chat transcripts.

### What we found

- Happ 4.6.1 (630) active on DOMKAT, subscription **LamaVPN**, selected country **Germany**, profile label **VLESS / TCP / REALITY / JSON**, connected with TUN - Xray TUN. Subscription shown valid until 2027-01-05.
- Happ's local `%LOCALAPPDATA%\Happ\subs.db` contains one subscription record with a high-entropy binary payload encoded as base64; this is not a standard plain-JSON Xray profile.
- The log `%LOCALAPPDATA%\Happ\logs\subscription_log.txt` contains a historical `https://sub.thelama.ru/<redacted>` URL (October 17). We fetched it **without publishing the path or user credential**. It currently responds HTTP 200 with **one decoy node "App not supported"**. Response includes `Hide-Settings: 1`.
- Official typical client UAs `Happ`, `v2rayNG`, `sing-box`, and `ClashMeta` do not yield the selected Germany VLESS/REALITY node from this historical URL; ClashMeta returned a decoy YAML.
- The unrelated `tshbotpopup.ru` log entries identify another provider `Toshibu VPN`. **Do not assume or import it as LamaVPN.**
- Therefore, the log URL is **not usable** to import the current paid German profile; its identity as the CURRENT subscription is not established.
- Official Happ documentation: encrypted subscription links can hide URI and node settings; standard server sharing is only available for nonhidden profiles:
  https://www.happ.su/main/ru/faq/adding-configuration-subscription
  https://www.happ.su/main/faq/share-configuration

### Implementation added in current source tree

- `scripts/Vpn/Import-VlessReality.ps1`: imports **exactly one** ordinary provider-authorized VLESS/TCP/REALITY URI from a local text file (never a CLI parameter), validates required UUID/REALITY parameters, builds current Xray `realitySettings.password` format and validates configuration with verified Xray v26.10.29.
- Saves encrypted profile via Windows DPAPI CurrentUser in `%LOCALAPPDATA%\ChatGPTMCPAgent\Vpn\profiles`. Source file remains user-owned; source should be stored privately and deleted by user after verified import if no longer needed.
- `scripts/Vpn/Start-ProtectedXrayProfile.ps1`: decrypts DPAPI profile locally, starts a separate loopback Xray HTTP proxy, checks actual bound port/PID, removes temporary plaintext config immediately after startup. It does not change Windows settings.
- `scripts/Vpn/Stop-ProtectedXrayProfile.ps1`: checks exact PID and path ownership before terminating the experimental process.
- `tests/test_vpn_protected_import.ps1`: creates random synthetic UUID and REALITY x25519 key, imports via DPAPI, verifies process starts and local port listens, checks that the encrypted profile doesn't contain the plaintext key, stops process and removes test files. **PASS**.
- Existing pilot already verifies isolated Xray HTTP, chained WebView2 via Happ, local VLESS loopback and fail-closed missing port behavior. None of these prove actual LamaVPN remote authentication.

### Why independent routing remains unverified

- Happ's local sing-box TUN config has a direct-routing process-name exception for `xray.exe` (and `sing-box.exe`). This suggests an embedded Xray process may bypass the Happ TUN automatically. It is **not** final evidence of outbound independence or a successful external REALITY handshake.
- The paid LamaVPN Germany server connection has not been replicated outside Happ, because the provider has not yet supplied a standard working profile.
- No MCP client, existing WebView2 user profile, service, or installed GUI was changed.

### Next action for actual LamaVPN E2E acceptance

Obtain **a provider-authorized standard Xray/VLESS subscription or individual Germany `vless://` link** from LamaVPN's subscription management/support channel, ideally with full REALITY parameters (UUID, host, port, SNI, pbk/password, optional short ID, fingerprint and flow). Do not try to use the `App not supported` decoy. If the provider deliberately supports only encrypted Happ-only profiles, ask the provider to enable a standard client connection or provide another compatible access method.

Then import the URL **on DOMKAT from a private local file**, start the dedicated Xray HTTP proxy, test authenticated ChatGPT WebView2 and MCP outbound traffic, verify the connection does not traverse Happ, and only then promote the embedded VPN settings into an officially released GUI.

No real LamaVPN credentials were added to project files or Git. No Happ or current MCP transport reboot was performed.

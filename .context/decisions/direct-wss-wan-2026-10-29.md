# Decision — Direct encrypted WAN transport via DOMKAT's static public IPv4

Date: 29.10.2026 MSK
Status: approved by owner; implementation/release completed in v0.2.8; real cross-network acceptance pending.
Scope: lvlaksim1/chatgpt-mcp-tunnel
Authority: direct owner instruction in this conversation.

DOMKAT's router has a publicly addressed WAN interface; owner states the IP is static, but permanence was not independently measured over time. DOMKAT and MKATUSHONOK occupy separate LANs. The selected main design directly connects remote agents over WSS/TLS to DOMKAT's router public address, avoiding mandatory Tailscale, VPS or cloud relay, while preserving the one existing OpenAI tunnel.

Client connects to wss://<public-IP>:55202/agent. Router forwards TCP 55202 to DOMKAT's local TCP 55202 only after explicit owner action. DOMKAT keeps local address and existing plaintext LAN-only TCP 55201; never port-forward TCP 55201. Kestrel hosts two endpoints concurrently; the WSS certificate is generated on server, with private key PFX and DPAPI-encrypted password, and is pinned by client using SHA256 fingerprint received over verified channel. Strong existing shared agent key remains required; incorrect pin rejected. UI provides mode switch and server fingerprint copy. WAN firewall rule may be installed only by explicit settings-button Windows UAC and is limited to local server address, port and executable, not auto-opened by release.

Source commits: 2b504600a62d1151ead2edf4fe14e01feed6cfde and f8cc66848a475030b63ad5c15e25ec6c68e76308.
Verified: local TLS handshake; invalid certificate refused; WSS client runs remote Windows tool; legacy LAN unaffected; 42-tool MCP/23 Windows schemas; reconnect; Windows CI 40781905752 success and Release v0.2.8 full/update installers.
Not verified: actual deployment on DOMKAT or physical MKATUSHONOK, WAN inbound reachability and router port forwarding. Live DOMKAT was not reconfigured by the manager. Tailscale remains an available fallback but is not required by this architecture.

Practical deployment and acceptance instructions: docs/direct-wan.md.

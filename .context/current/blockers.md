# Current blockers and open risks

## WAN direct option: remaining acceptance gates
- Public IPv4 confirmed on DOMKAT router; direct Internet transport is possible in principle, but inbound port forwarding and real Internet reachability were not validated. UPnP TCP 55201/443 mapping absent (714) is not evidence about manual mappings.
- Current Router Lite ws:// port 55201 permits only the LAN subnet through existing Windows Firewall. Directly opening it publicly would expose unencrypted agent credentials and command transport; secure authenticated WSS/TLS is a hard prerequisite.
- Owner has not chosen whether to replace/extend the v0.2.7 Tailscale path with direct WSS. Nothing deployed and no two-computer real acceptance.
- source: verified RT-GM2-3 WAN and Windows firewall 28.10.2026.


## Remaining after v0.2.7 successful release
- Source, CI, release and Windows installer acceptance completed. Network acceptance remains: Tailscale client must be installed/authenticated on both DOMKAT and MKATUSHONOK in the same accessible tailnet.
- No current direct access to MKATUSHONOK has been proven. No end-to-end remote instrument execution across physical networks has been observed.
- DOMKAT live installed v0.2.6 is deliberately untouched; v0.2.7 release does not mean it has been deployed. Tailscale link, server IP binding and per-peer firewall rule require actual owner-authorized Windows/account interactions and tests.
- Firewall UAC operation has only been statically and locally validated without an actual Tailscale interface/admin privileges, not yet exercised on the physical VPN network.
- source: source and release acceptance 28.10.2026.


## Критический блокер: разные физические сети (28.10.2026)
- Владелец подтвердил, что DOMKAT и MKATUSHONOK находятся НЕ в общей локальной сети. Прямой путь к частному IP DOMKAT 192.168.0.3 с MKATUSHONOK не доказан; прежние инструкции по LAN-подключению считать недействующими.
- Способ безопасной маршрутизации через интернет не утверждён. Требуется либо защищённая виртуальная сеть, либо общедоступный WSS-сервис/ретранслятор; прямое открытие ws:// в интернет исключается архитектурными ограничениями проекта.
- Для рекомендованного к оценке Tailscale не подходит существующая кнопка LAN-брандмауэра: она не поддерживает 100.64.0.0/10. Требуются отдельная проверка привязки серверного интерфейса и ограниченного разрешения сетевого доступа.
- Факт незавершённой приёмки на MKATUSHONOK не следует приписывать ошибке ключа или локального брандмауэра без проверки маршрута.
- source: поправка владельца и проверенный код v0.2.6 28.10.2026.


## Latest blocker assessment — 28.10.2026 22:20 MSK
- DOMKAT v0.2.6 is already installed and running: older installation-pending notes below are historical and superseded.
- Real MKATUSHONOK-to-DOMKAT Router Lite connection remains NOT PROVEN. No established remote client socket was present at observation; this is not conclusive evidence of a firewall issue.
- Authorized LAN-only firewall allowance and client-key synchronization have not been verified. 41 chat-visible connector tools vs 42 locally advertised remain an exposure/metadata check.
- Maintain existing OpenAI Tunnel ID, IP 192.168.0.3 and credentials. Do not perform remote upgrades or unilateral firewall changes.
- source: verified runtime observations 28.10.2026.


1. No second physical PC tested: cross-network operation, real remote Windows sessions and deployment remain **not proven**.
2. Router Lite is not activated in the live DOMKAT tunnel; isolated test build only. Deployment and safe agent configuration/autostart remain open.
3. Agent authentication currently uses one shared secret supplied through environment variables. Next phase should support protected per-agent credentials and straightforward onboarding without added database/infrastructure.
4. 16 MiB wire message limit exists. Oversized responses fail explicitly; high-resolution screenshot size and network latency on a second PC need empirical validation.
5. Remote GUI actions require a user session on that PC. Server or locked-session behavior has not been tested.

The local routing, controls and reconnection tests passed; these risks do not invalidate that narrower evidence.

6. Agent installer Setup/Update v0.1.0 was compiled/tested/published on Windows GitHub Actions; next blocker is real deployment on second physical Windows PC.
7. Protected per-user DPAPI secret is functional for the client; Router Lite still requires the same manually configured shared secret server-side. A second physical PC has not been onboarded.

## Same-tunnel transition risk
- Current MCP channel is the old DOMKAT tunnel. Cutover will temporarily disconnect ChatGPT; independent local script and verified installer must be staged first.
- Owner's replacement runtime key is not available and must be entered directly into new server GUI.
- Keep the Git source repository even when removing installed old executable/profile and scheduled task.

## Post-recovery follow-up
- No current DOMKAT outage: original ChatGPT Tunnel connection and Router Lite are working.
- Published v0.2.1 installer still has the native stderr redirection bug. v0.2.2 release is needed before next update.
- Client MKATUSHONOK has not yet been verified through the new live Router Lite; tool metadata refresh may be needed for newly added computers.list tool in ChatGPT.

## Status after v0.2.2
- Old native stderr failure resolved in published binary and installed DOMKAT. No remaining local outage.
- Client MKATUSHONOK has not yet been validated against 192.168.0.3:55201 across the physical network.
- ChatGPT connector may still cache 41 tool surface even though local MCP server advertises 42 (computers.list addition); plugin metadata refresh could be required.

## Current UI state
- Live installed DOMKAT remains v0.2.2; original OpenAI tunnel itself is healthy but tray status and Settings menu are faulty.
- Source fixes in v0.2.3 are local-tested. Need Windows CI build and in-place upgrade before verifying actual GUI. No need to replace existing Tunnel ID or credentials.

## Last GUI acceptance gate
- Running v0.2.3 needs explicit Win32 SW_SHOW to display Server Settings on this Windows session; temporary native activation worked. Permanent v0.2.4 source fix compiled and tested, pending Inno release and in-place update.
- Existing OpenAI tunnel, DPAPI credentials and Router Lite remain healthy and unchanged.

## v0.2.5 release acceptance
- Source and local build complete, Windows CI and binary release/asset digests pending.
- Owner will self-update DOMKAT; no automated in-place install authorized. 192.168.0.3 stays unchanged, DHCP reservation postponed.

## v0.2.5 owner acceptance
- No release/build blocker; v0.2.5 binaries published and CI success.
- User will self-update DOMKAT via built-in updater. Actual post-install UI/connection acceptance remains unverified and should only be performed after owner reports update; do not remote-upgrade.

## v0.2.6 release and migration
- Source fix tested; Windows installer CI and release pending.
- v0.2.5 built-in updater is itself defective; user must manually launch v0.2.6 update installer once, no remote install.

## Owner acceptance pending after v0.2.6
- Release itself successful. User must manually install v0.2.6 Update because existing v0.2.5 built-in updater has file-lock error.
- Windows Firewall exception must be explicitly approved by owner (server Settings/UAC) before external LAN testing. Actual MKATUSHONOK success not yet proven, no separate remote device access.

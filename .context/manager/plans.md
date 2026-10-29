# Manager plans

## v0.2.15 passed Windows CI — next host experiment
1. Stable v0.2.15 verified with Setup/Update installer and SHA-256; no production installation during development.
2. Refresh the ChatGPT MCP connector catalog (now 52 runtime tools), open chat.ui.open, enable relay in the embedded component, send message from local GUI.
3. Record whether ui/message starts a model turn in ordinary ChatGPT, whether chat.reply sends answer to desktop window, and whether iframe remains alive. If host rejects, record concrete error and keep queued message.

## Plan: ui/message experiment v0.2.15
1. DONE: MCP ui resource, global/thread entrypoints, chat.ui.open/pending/ack, user opt-in, window-side pending messages, ui/message request and durable ack.
2. DONE: .NET build (zero warnings/errors), MCP 52 tools/resource tests, simulated host positive/denial, all 23 remotely routable tools and reconnect.
3. NEXT: CI Windows Setup/Update v0.2.15 and stable artifact verification; document this as an experiment, not confirmed ChatGPT activation.
4. NEXT: owner-controlled update and ChatGPT tool-refresh, invoke chat.ui.open IN ordinary ChatGPT, click Start, send message in local window, watch whether model starts and uses chat.reply.
5. If host denies automatic ui/message or component lifecycle is too short, record observation and redesign, without introducing any forbidden platform.

## v0.2.14 published — next physical acceptance
- Completed: 49 runtime tools, GUI attachment, MCP text/PNG, all 23 remote Windows tools/reconnect local tests, Windows CI installer full/update/uninstall and stable release.
- Next: owner manually updates local DOMKAT when appropriate; if using the chat UI on another computer, update that installation as separately authorized.
- Refresh current ChatGPT plugin snapshot (45 tools) to expose chat.attachment.read; verify actual message from the user's GUI with file and pasted image; read through tool; verify historical persistence and no regression.
- Do not claim installed UI works before the owner completes physical test.

## Plan: Chat attachment release v0.2.14 — 29.10.2026
1. DONE: UI picker/multi-select/drag-drop/Ctrl+V/preview/remove/attachments-only, isolated storage and SHA-256, new chat.attachment.read.
2. DONE: isolated GUI persistence selftest, MCP real image/text/integrity tests, 49 tools and 23 Windows remote tools/reconnect regression.
3. NEXT: publish source and sealed generation in one Git commit; run GitHub Actions Windows v0.2.14 release Setup/Update tests.
4. NEXT: validate release assets/hashes; owner-managed update after explicit approval, do not modify current DOMKAT/MKATUSHONOK v0.2.13 during development.
5. NEXT: live Ctrl+V and attached-file UI test and ChatGPT new tool refresh; mark complete only after physical evidence.

## Physical transfer acceptance evidence, 29.10.2026
- DONE: full original MSI copy MKATUSHONOK to DOMKAT 324820992 bytes, SHA-256 14A528C373B19B35B3C9E77B5FAEE88DC34A073F15D8801673DDB7C040B2D48F, valid signature, reconnect recovery.
- FOLLOW-UP: CLI status parser false exit code; plugin schema refresh; GUI progress window visual smoke test.


## Large file transfer development, 29.10.2026
- Code and local tests: DONE. 90 MiB integrity PASS, 15 MiB two-way, cancellation/resume, GUI.
- v0.2.9 CI publication and two-endpoint deployment: IN PROGRESS.
- Original MAX.msi copy from MKATUSHONOK to DOMKAT and hash + progress check: PENDING.


## Active priority: direct WSS static WAN, 29.10.2026
1. Source v0.2.8 is built, locally tested and released by Windows CI; keep installed DOMKAT v0.2.7 and existing OpenAI tunnel unchanged until owner-controlled deployment.
2. Keep DOMKAT local 192.168.0.3; external IPv4 belongs to the router. Preserve TCP 55201 only for LAN; prepare WSS TCP 55202 on local address in updated application.
3. On explicit owner-controlled steps install Update v0.2.8, enter public WAN address, generate certificate, verify SHA-256 fingerprint, authorize Windows WAN rule and configure router WAN TCP 55202 -> DOMKAT local TCP 55202. Do not expose plain 55201.
4. On physical MKATUSHONOK in another LAN configure WSS endpoint and independently checked pin and key; verify real cross-internet connection, remote Windows commands, GUI/PNG and recovery. If inbound reachability fails, inspect router mapping/provider policy and certificate diagnostics before architecture changes.
5. Record observed evidence; close physical acceptance only upon successful end-to-end tests. Tailscale v0.2.7 remains an optional fallback, not a prerequisite.

## Public WAN direct-connection investigation (28.10.2026)
1. Verified: router RT-GM2-3 reports globally routable IPv4 as its WAN address, matching public egress, without CG-NAT at its interface. Owner reports IP is static; only current presence was independently checked.
2. Verified current Router Lite 55201 listens only on local DOMKAT 192.168.0.3. Windows firewall entry allows only LAN /24, and UPnP reports no dynamic mapping of TCP 55201/443 (fault 714). Other manually configured router forwards and external inbound reachability still unverified.
3. Propose Owner choose direct secure WAN path instead of requiring embedded Tailscale: add TLS/WSS server or trusted reverse proxy, secure enrollment/authenticated client, strict limits/logs; then configure explicit router forward and firewall rule with user authorization.
4. Test from actual external physical network MKATUSHONOK. Do not call direct path operational without external proof. Keep released v0.2.7 as an alternative; do not change current running tunnel.
- source: router UPnP and read-only Windows check, 28.10.2026.


## Post-release v0.2.7 remote computer plan
1. Retain the working DOMKAT OpenAI tunnel; do not remotely update the installed v0.2.6, change Tunnel ID, keys, or 192.168.0.3 without separate approval.
2. Install/sign in to Tailscale on DOMKAT and MKATUSHONOK under authorized Windows/account interaction. Both must be members of one permitted tailnet; verify direct or encrypted relay and exact 100.x IP addresses.
3. Update installed ChatGPT MCP Agent to published v0.2.7 by owner-controlled method, configure the DOMKAT Router Lite listening IP as its own Tailscale address; check listening/health.
4. In server Settings grant one remote MKATUSHONOK Tailscale IP access via the new dialog with explicit UAC approval (not whole 100.64.0.0/10). On client set DOMKAT Tailscale IP and protected shared agent key.
5. Verify Tailscale connectivity, then router authentication, computer targeting, 23 Windows tools, PNG/UIA and reconnect; only after evidence close physical acceptance gate. Review whether multiple listening interfaces are needed without disturbing existing cloud tunnel.
- source: approved cross-network design and verified v0.2.7 Windows CI/Release, 28.10.2026.


## Исправленный план: разные сети — 28.10.2026
1. Прекратить сценарий прямого подключения MKATUSHONOK к локальному IP DOMKAT 192.168.0.3; отдельного маршрута между сетями нет в подтверждённых данных.
2. Согласовать способ защищённого транспорта между компьютерами: предпочтительный вариант испытания — Tailscale/WireGuard, альтернатива — отдельный WSS-посредник. Не разворачивать инфраструктуру без решения владельца.
3. Если выбран защищённый VPN, определить выделенный адрес DOMKAT на виртуальном интерфейсе, допустимость привязки Router Lite, авторизацию/политику доступа, правила брандмауэра только для виртуальной сети. Кнопка текущего LanFirewall для 100.64.0.0/10 не подходит; сначала отдельная безопасная техническая проверка/исправление.
4. После настройки межсетевого транспорта проверить соединение с реальным MKATUSHONOK, ошибки и авторизацию; затем 23 инструмента Windows, UIA, PNG и переподключение.
5. Не менять работающий DOMKAT, Tunnel ID, секреты и IP 192.168.0.3 локальной сети в рамках одного уточнения; вести отдельные проверяемые изменения.
- source: уточнение владельца 28.10.2026, текущая архитектура и код v0.2.6; authority: owner-directive, verified-repository.


## Active plan after verified v0.2.6 installation (28.10.2026 22:20 MSK)
1. Treat DOMKAT v0.2.6 installation as already observed complete: running PID 30196, FileVersion 0.2.6.0. Do not install again.
2. Check current server Settings, authorized LAN-only firewall rule and client-key coordination without revealing secrets.
3. Verify actual physical MKATUSHONOK connection; distinguish network timeout, 401 incorrect key and 409 duplicate name.
4. Test physical remote file/shell/screenshot/UIA and reconnect, check 42nd tool metadata visibility, then record evidence and close acceptance only after success.
- source: original MCP Tunnel live runtime observation and verified GitHub v0.2.6 release. Preserve current DOMKAT tunnel identity, IP and program files.


## Router Lite — active implementation plan
1. Preserve 41 existing tools and live tunnel; `computers.list` is tool 42. All 23 Windows tools have `computer` parameter. [complete, locally verified]
2. One executable with router/agent mode, outgoing WebSocket agents and request IDs without database. [complete, locally verified]
3. Extend routing to the remaining 22 Windows tools while preserving MCP text/image blocks and Windows interactive-session behavior. [complete, local E2E verified]
4. Connect two distinct physical PCs through a protected network; on DOMKAT reconnect, wrong key, duplicate ID, unknown target and GUI actions already passed. [next]
5. Add safe configuration/startup of agents and protected local secret storage; avoid new plugins or repositories.
6. After verified release of a version, update context and record the exact tests and commit.

Implementation references reviewed: MeshCentral agent networking, Supergateway process/JSON-RPC forwarding, Docker MCP Gateway collision and security rules. They are design references, not dependencies.

Source: accepted Owner scope, inspected repository code and verified eight test suites on DOMKAT 28.10.2026.


## Agent packaging — active implementation plan
1. Inspect MailRu Desktop Inno Setup, release automation and updater (completed 28.10.2026).
2. Create standalone Windows tray agent hosting `ChatGptMcpTunnel.exe --agent`, with DPAPI settings, autostart, reconnect and update helper (source and local build completed).
3. Create first-install and silent-update Inno scripts and Windows GitHub Actions release workflow; do not track binaries. [complete, CI verified]
4. Verify on Windows CI: self-contained publish, agent selftest, MCP smoke, first install, settings survival under update, uninstall hygiene. Published stable numeric v0.1.0. [complete]
5. Connect a second physical computer over protected private networking to DOMKAT's one-tunnel router and verify real GUI/shell and recovery (next after installer release).
- source: direct Owner mandate 28.10.2026, examined MailRu Desktop repository, local build and smoke evidence.
- authority: owner-directive, verified-runtime.

## Role-based installation paths
1. Expose Inno Setup SelectDir page after role selection. [source complete]
2. Suggest D:\ChatGPTMCPAgent for server if D exists; per-user C: program directory for client. [source complete]
3. Preserve manually chosen InstallDir in registry and reuse on update/uninstall. [source complete]
4. Verify defaults and custom folder in Windows CI. [next]
- source: owner directive 28.10.2026, Inno Setup code.
- authority: owner-directive.

## Same-existing-tunnel rollout
1. Remove new OpenAI tunnel creation and require existing Tunnel ID. [implemented]
2. Fix Windows PowerShell 5.1 server launcher and verify local WinForms x64 selftest. [complete]
3. Release/test universal installer v0.2.1 and verify existing-only workflow in Windows CI. [next]
4. Pre-stage SHA256-verified installer on DOMKAT while old MCP channel is live; preserve only Tunnel ID metadata. [next]
5. Execute independent local cutover, erase old runtime/auth and launch server UI for Owner-entered token. Keep source Git repository. [pending]
6. Verify 42 tools and MKATUSHONOK on same plugin. [pending]
- source: Owner instructions and observed local tests 28.10.2026.
- authority: owner-directive, verified-runtime.

## Live DOMKAT repaired
1. Reproduce seven-second restart and identify PowerShell native stderr error handling. [complete]
2. Hotfix installed server launcher with direct native call and verify actual original plugin, 42 tools, local health ports. [complete]
3. Update source installer launcher to permanently prevent the error, build stable v0.2.2. [source complete, release next]
4. Connect MKATUSHONOK through the original DOMKAT Router Lite. [next]
- source: DOMKAT observed 28.10.2026.
- authority: verified-runtime, owner-directive.

## v0.2.2 rollout closed
- Permanent source fix committed; CI run 40712500651 completed success, binary release v0.2.2 published and downloaded with verified SHA256.
- On DOMKAT updated installed server v0.2.1 -> v0.2.2 in-place, role/ID/DPAPI unchanged; restored original plugin connectivity (ping, system.info, healthz/readyz), verified single tunnel-client and Router Lite port 55201.
- Next: physical MKATUSHONOK client onboarding and permissions check only.
- source: verified GitHub release and live DOMKAT inspection 28.10.2026.
- authority: verified-runtime.

## v0.2.3 visible UI repair
1. Verify actual original OpenAI tunnel despite false offline label and inspect hidden Server Settings window. [complete]
2. Replace client stderr heuristics for server with independent bounded readyz and Router Lite check. [source complete]
3. Replace blocking ShowDialog with one modeless reusable and focusable Settings window, plus named-event --settings. [source complete]
4. Embed custom original Windows ICO and green/amber/red tray dot into application and both installers. [source complete]
5. Run Windows Inno CI, publish v0.2.3 and update DOMKAT; verify actual visible settings window and status. [next]
- source: Owner requests, observed live DOMKAT, local tests on 28.10.2026.
- authority: owner-directive, verified-runtime.

## Native Settings window acceptance v0.2.4
1. Confirm modeless v0.2.3 Settings form is created but native HWND lacks WS_VISIBLE on DOMKAT. [complete]
2. Manually apply user32 ShowWindow SW_SHOW to exact Settings HWND; confirm foreground window visible via desktop.window.list. [complete]
3. In program, ensure native IsWindowVisible and apply ShowWindow(SW_SHOW) after Form.Show and when reopening; keep no-new-tunnel policy. [source complete]
4. Publish v0.2.4 after Windows installer CI, update DOMKAT in place, verify actual Settings window via --settings and app tray menu. [next]
- source: verified native Windows runtime 28.10.2026, Owner directives.
- authority: verified-runtime, owner-directive.

## Approved Settings forms — two installers
1. Replace permanently visible secret inputs by on-demand SecretInputDialog and status labels, server only copy/generate, client input-only. [source complete]
2. Preserve immutable configured OpenAI Tunnel ID, local address 192.168.0.3, automatic port 55201, DPAPI keys and native Win32 settings visibility. [source complete]
3. Verify self-contained binary, icon/health/forms tests. [local complete]
4. Run Windows Inno CI first/update/preservation/uninstall and publish full+update v0.2.5. [next]
5. Report release links and SHA256; owner performs self-update using existing application UI. DO NOT install on DOMKAT. [owner-controlled]
- source: owner approval and delivery instruction 28.10.2026.
- authority: owner-directive.

## Approved Settings installers — release closed
1. Secret-free settings forms and temporary key entry compiled and tested. [complete]
2. Windows installer CI: full, update, preservation and uninstall. [success: 40732281198]
3. GitHub stable release v0.2.5 binary assets and SHA256. [published]
4. Do NOT upgrade DOMKAT through remote tools; owner will update via application UI. [owner-controlled]
5. Await owner installation results for visual acceptance; retain 192.168.0.3. [future]
- source: verified GitHub release and owner directive 28.10.2026.
- authority: verified-repository, owner-directive.

## Corrective v0.2.6 plan
1. Root cause user screenshot and runtime log, identify FileStream lock. [done]
2. Dispose download output before rename; fail safely on incomplete download. [done]
3. Local --test-updater-download, --selftest, --test-icons, --test-secret-forms, --probe-server pass. [done]
4. Context validate, commit, Windows installer CI, publish both installers and verify SHA256. [next]
5. Owner manually launches v0.2.6 update EXE once; do not remote-install. [owner-controlled]
- source: owner incident, verified-runtime 28.10.2026.

## Physical client connectivity acceptance
1. Verify server readiness, bound interface, firewall profile and rule [done: 192.168.0.3:55201, readyz 200, Public inbound blocked, no allow rule].
2. Add explicit user-confirmed UAC action to permit inbound TCP 55201 only for Router Lite executable from same local subnet [code/test done in v0.2.6].
3. Add visible client state for HTTP 401 wrong key, 409 duplicate name, other errors [code/test done].
4. Publish v0.2.6 with updater staging fix and these diagnostics [pending Windows CI].
5. User manually installs v0.2.6, opens server Settings, clicks LAN access button/accepts UAC, copies latest server client key and saves it in client Settings, verifies connection from MKATUSHONOK [user-controlled; not yet verified].
- authority: owner-incident, verified-runtime.

## v0.2.6 release completed — owner follows up
- GitHub stable release https://github.com/lvlaksim1/chatgpt-mcp-tunnel/releases/tag/v0.2.6 built/tested successfully on Windows CI run 40736310056.
- Owner downloads Update v0.2.6 to repair existing v0.2.5 updater and installs locally. Do not remotely update.
- Owner then uses server LAN firewall setup button (UAC), shares current server client key, enters/saves key on MKATUSHONOK, tests actual connection. Physical intercomputer link remains unverified.
- source: owner directive, GitHub CI and verified release.
- authority: verified-repository, owner-directive.

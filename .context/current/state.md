# Current state

## Direct WAN TLS implementation: v0.2.8 released — 29.10.2026
- Решение владельца 29.10.2026: DOMKAT имеет статический публичный IPv4 на WAN-интерфейсе маршрутизатора; удалённый клиент расположен в другой локальной сети. Развивается прямой защищённый WSS без обязательного Tailscale/посредника. Локальный адрес DOMKAT не заменяется внешним IP. Источник: прямая команда владельца; код основной ветки f8cc66848a475030b63ad5c15e25ec6c68e76308.
- Сервер Kestrel обслуживает защищённый порт TCP 55202 в дополнение к прежнему локальному TCP 55201. Сервер создаёт собственный TLS-сертификат, шифрует пароль PFX через DPAPI Windows, клиент проверяет точный SHA-256 отпечаток до передачи общего ключа. Новый режим требует минимум 32 символа общего ключа. Проверены сборки, 42 инструмента, 23 направляемых инструмента Windows, запрет неверного сертификата, выполнение удалённой команды по WSS, восстановление связи, сохранение старого LAN-подключения; GitHub Actions 40781905752 завершён успешно; v0.2.8 опубликована с установщиками Setup и Update.
- На рабочий DOMKAT v0.2.8 не устанавливалась. При последней проверке на DOMKAT работает прежний процесс и слушает 192.168.0.3:55201, порт 55202 пока не открыт. На маршрутизаторе не создавался проброс TCP 55202, правила брандмауэра не менялись. Физическая связь с MKATUSHONOK через Интернет и 23 удалённых инструмента остаётся НЕ ДОКАЗАНА. Публичный WAN IPv4 подтверждён чтением роутера; постоянство адреса основывается на словах владельца.
- Implementation: docs/direct-wan.md; code commits 2b504600 and f8cc6684, CI run 40781905752; secure port 55202, private legacy port 55201.

## Direct public IPv4 feasibility verification — 28.10.2026
- User reports static IP. Router RT-GM2-3, at local gateway 192.168.0.1, directly responded to UPnP WANIPConnection GetExternalIPAddress with a globally routable IPv4 matching three IP-checking services, including a --noproxy curl check. Exact public IP intentionally excluded from this publicly visible repository.
- The independently verified property is a public WAN address (not CG-NAT at this router), not the address permanence; latter remains user-reported. No global IPv6 observed.
- Router Lite still listening on 192.168.0.3:55201 and existing local-only Windows firewall ChatGPTMCPRouterLiteLAN remote 192.168.0.0/24. UPnP port mapping query for 55201 and 443 returned fault code 714 (no UPnP mapping); manual forwarding and Internet reachability not established.
- Direct WSS/TLS remote connection can potentially replace dependency on Tailscale/Headscale; requires explicit port forwarding and firewall handling, strong authenticated encryption and real remote verification. Never expose raw ws:// token protocol to Internet.
- No configuration, port forwarding or installed binaries modified in this WAN inspection. Working v0.2.6 on DOMKAT, v0.2.7 GitHub release available. Physical MKATUSHONOK still NOT PROVEN.
- source: read-only route/adapter/gateway, UPnP SOAP, firewall and owner statement 28.10.2026.


## Published Tailscale interoperability release — 28.10.2026 22:45 MSK
- Stable v0.2.7 now available at https://github.com/lvlaksim1/chatgpt-mcp-tunnel/releases/tag/v0.2.7; Windows CI run 40756470762 completed SUCCESS on source commit c04e2ec44c287d4f41ee4433f05b728609ed94ae.
- Full installer ChatGPT_MCP_Tunnel_Setup_v0.2.7.exe SHA256 f260ec4092e68e88f00b40d800002aec83a00470b26787939618765d23084913; Update ChatGPT_MCP_Tunnel_Update_v0.2.7.exe SHA256 0d775d1a06f82d70e2daf8a00c54352b5f9a62a2cbd0828e26ed2f936d968deb.
- Chosen transport for different physical networks is Tailscale. Client RouterUrl already supports 100.64.0.0/10; v0.2.7 adds Tailscale-address validation of real interface and a Windows Firewall UAC path allowing only exact remote peer IP and server IP TCP 55201 (not whole tailnet). Standard LAN rule remains.
- Local compilation zero warnings/errors; --test-tailnet-firewall, --test-lan-firewall, --selftest, --test-secret-forms, --test-client-errors, --test-updater-download all exit 0. Local MCP 42-tool / Router Lite / reconnect tests passed. CI passed install/update-preservation/uninstall on Windows.
- Real Tailscale login/interface on DOMKAT and MKATUSHONOK and physical inter-network client connection are NOT PROVEN. The working installed DOMKAT agent was not changed by this operation; last observed installed v0.2.6. No public port or firewall modification performed.
- source: source and local tests, authenticated GitHub Actions & Releases, Owner network topology 28.10.2026; authority: verified-runtime, verified-repository, owner-directive.


## Уточнение топологии — 28.10.2026 после восстановления менеджера
- Владелец сообщил: DOMKAT и MKATUSHONOK находятся в РАЗНЫХ локальных сетях. Прежний план прямого соединения клиента с 192.168.0.3:55201 и настройки обычного LAN-брандмауэра был ошибочным по топологии.
- DOMKAT v0.2.6 остаётся работающим узлом единственного OpenAI MCP Tunnel, Router Lite слушает локальный 192.168.0.3:55201. Реальное межсетевое подключение не подтверждено, транспорт не выбран.
- Прочитанный код v0.2.6: AgentStorage позволяет адрес из диапазона 100.64.0.0/10 для ws://; серверная настройка может привязываться к назначенному локальному IPv4; текущая кнопка LanFirewall.LocalNetwork не принимает адреса Tailscale 100.64.0.0/10. Поэтому установка одной VPN без проверки адреса прослушивания и защиты брандмауэра ещё не доказывает подключение.
- Проверка DOMKAT не обнаружила служб или адаптеров Tailscale/WireGuard/ZeroTier/NetBird/OpenVPN. О сети MKATUSHONOK, внешнем IP, NAT и дополнительных VPN-средствах данных нет.
- Исходники программы и текущую установленную конфигурацию на компьютере при этой проверке не меняли.
- source: непосредственная поправка владельца, прочитанные файлы репозитория и запрос к службам/адаптерам DOMKAT 28.10.2026; authority: owner-directive, verified-repository, verified-runtime.


## Reconciled live status — 28.10.2026 22:20 MSK
- Original ChatGPT MCP Tunnel bridge_ping succeeds; current chat exposes 41 tools.
- Running DOMKAT ChatGptMcpAgent.exe PID 30196: FileVersion 0.2.6.0, started 20:38:25 MSK; tunnel-client.exe is running. The 192.168.0.3:55201 router TCP socket listens and showed no established remote clients during the observation.
- Current stable GitHub release v0.2.6 and Windows CI 40736310056 success verified.
- This supersedes the previous pending v0.2.6 installation instruction. It does NOT prove the real MKATUSHONOK connection, firewall permission or Settings UX.
- The listener is LAN-bound: 127.0.0.1:55201/readyz cannot be reached and 192.168.0.3:55201/readyz returns 404; these checks do not by themselves indicate a router outage.
- No software installations, secret modifications, firewall changes or remote client operations were performed.
- source: direct original MCP Tunnel read-only process/version/TCP checks, bridge ping, GitHub Release/Actions 28.10.2026; authority: verified-runtime, verified-repository.


- Date: 28.10.2026 MSK; Project Manager `project-manager`, stable version 2.1.0; state/product authority branch `main`.
- Router Lite stages 1–3 implemented and locally verified in a separate Release build on DOMKAT. The working OpenAI Secure MCP Tunnel process was not restarted/replaced.
- 42 MCP tools advertised: original 41 plus `computers.list`. All 23 Windows tools support optional `computer`; if omitted, existing local behavior remains unchanged.
- Generic remote dispatcher invokes the existing Windows methods, correlates replies and preserves MCP text and PNG image blocks. GUI operations are serialized on each agent; other calls may run concurrently.
- Test evidence: `test_mcp_stdio.py`, `test_router_lite.py`, `test_router_reconnect.py`, `test_windows_tools.py`, `test_gui_input_tk.py`, `test_windows_uia.py`, `test_winforms_uia_compat.py`, `test_uia_real_apps.py` passed on DOMKAT with isolated build. Tests include incorrect token, duplicate agent, router restart and remote WPF UIA.
- External PC, remote network, installer/autostart and final release remain unverified/unimplemented. Existing two unrelated untracked local files were not changed.
- Source: Owner decision (single tunnel), verified source/build/runtime 28.10.2026.


## Installer implementation (28.10.2026 MSK)
- Added WinForms tray host `src/ChatGptMcpAgent`, typed first-run settings, protected DPAPI credentials, HKCU Run autostart, subprocess management and GitHub Releases updater.
- Added Inno Setup full/update scripts, release workflow with installer acceptance and release publication, Russian documentation in `docs/agent-installer.md`.
- Observed self-contained win-x64 publish and DPAPI/URL selftest exit 0 on DOMKAT; runtime self-contained publish, 42-tool MCP test, router test (without GUI on CI), reconnect test passed.
- Inno Setup not installed on DOMKAT: binary installers not yet compiled or installed. Live DOMKAT tunnel remains untouched.
- No second physical Windows computer tested.
- source: local build/test and owner-approved MailRu Desktop architecture.
- authority: verified-runtime, owner-directive.

## Published installer acceptance (28.10.2026 MSK)
- Binary release `v0.1.0` published with both Setup and Update assets at `https://github.com/lvlaksim1/chatgpt-mcp-tunnel/releases/tag/v0.1.0`.
- GitHub Actions run 40689966362 passed: Windows self-contained publish, agent DPAPI selftest, 42 MCP-tool checks, Router Lite integration/reconnect, Inno Setup compilation, first-install/update-preserves-data/uninstall-cleanup test.
- On DOMKAT the live MCP Tunnel was not restarted/replaced; physical second computer is still unverified.
- source: GitHub Actions release workflow 40689966362, GitHub Release v0.1.0.
- authority: verified-runtime, verified-repository.

## Editable installer folder (28.10.2026)
- Owner requirement: editable install path, server default D:, client default C:.
- Source implemented: native Inno directory page after role selection, server D:\ChatGPTMCPAgent if drive exists, client per-user program folder, fallback on no D.
- Persist InstallDir in HKCU registry; updater targets stored directory and preserves chosen path.
- CI tests were extended for default and custom directories. Real Inno acceptance pending.
- source: owner directive and local source review.
- authority: owner-directive, verified-repository.

## Universal installer v0.2.0 RELEASED (28.10.2026)
- GitHub Actions run 40696034380 completed success, including standalone WinForms and runtime build, official OpenAI tunnel-client SHA256 verification, MCP/router tests and real Inno first-install/update/uninstall acceptance.
- Universal Setup v0.2.0 published at https://github.com/lvlaksim1/chatgpt-mcp-tunnel/releases/tag/v0.2.0; Setup and Update assets both verified present (plus legacy updater alias).
- CI accepted server default D (fallback if not present), client default user-local C, arbitrary custom client directory, preservation during update and correct uninstall.
- Live DOMKAT legacy tunnel has not been removed or restarted. The owner requested a clean install and will enter the new OpenAI token; migration with temporary loss of MCP access is a separate next operation.
- source: GitHub Actions run 40696034380 and release v0.2.0.
- authority: verified-repository, verified-runtime.

## Same-tunnel clean reinstall preparation (28.10.2026)
- Owner explicitly requires same existing OpenAI cloud Tunnel ID and plugin; no create/delete cloud tunnel calls.
- New server UI uses existing Tunnel ID and accepts runtime API key; old key is not reused.
- Windows PowerShell 5.1 unsupported output redirection corrected; GUI x64 publish/selftest passed.
- Two-stage DOMKAT plan: prepare verified Setup and identity-only metadata while old tunnel works; independent local script then removes old autostart, executable, cached profiles/credentials and installs Server role on D, prompting for token.
- Old tunnel currently remains live; no destructive cutover occurred.
- source: Owner correction, source, local build and PowerShell AST checks 28.10.2026.
- authority: owner-directive, verified-runtime.

## New server successfully recovered (28.10.2026 MSK)
- Installed v0.2.1 server at D:\ChatGPTMCPAgent, previous logical OpenAI Tunnel ID remains unchanged and new DPAPI credentials are present.
- Cause of repeated process restarts: PowerShell 5.1 treated stderr INFO messages from MCP StdioServerTransport via 2>&1 under ErrorActionPreference Stop as a fatal NativeCommandError.
- Corrected installed ServerStart.ps1: direct native tunnel-client run under temporary ErrorActionPreference Continue and preserve exit checking.
- Original MCP tunnel works again: bridge_ping, system_info DOMKAT, GitHub auth, desktop.window.list verified; 42 tools available in direct MCP tool-list test.
- Router Lite binds 192.168.0.3:55201; tunnel health 127.0.0.1:55198; healthz and readyz are HTTP 200; only one actual tunnel-client instance.
- Permanent source fix prepared for v0.2.2. No second cloud tunnel or token changes.
- source: recovered live DOMKAT runtime 28.10.2026.
- authority: verified-runtime.

## Stable universal server installed: v0.2.2
- GitHub Actions 40712500651 succeeded; published Setup and Update v0.2.2 assets, verified release SHA256.
- DOMKAT installed in-place using verified Update v0.2.2, existing role server, Tunnel ID and both DPAPI credentials retained. Installer exit 0 and post-update health passed.
- Actual process version 0.2.2; one tunnel-client, one MCP Runtime; Router Lite bound 192.168.0.3:55201 and port 55198 returns healthz/readyz HTTP 200.
- Original ChatGPT plugin bridge_ping and system.info succeeded; local installed runtime MCP list 42 tools and 17 GitHub/15 multimedia checks passed.
- Old installation and metadata duplication not needed. Next separate step: physical second computer MKATUSHONOK not yet connected/tested.
- source: GitHub Release v0.2.2, Actions 40712500651, verified installed runtime 28.10.2026.
- authority: verified-runtime, verified-repository.

## Source-complete GUI and icon v0.2.3 (28.10.2026 MSK)
- Owner screenshot: generic tray icon and erroneous «Нет соединения», but original ChatGPT bridge ping and readyz HTTP 200 and Router Lite 192.168.0.3:55201 are healthy.
- Owner reported Settings menu does nothing. Win32 window enumeration showed a hidden Server Settings HWND in the existing process. Original code used ShowDialog from Application.Idle with a guard preventing reopening.
- Fixed source to use single modeless Settings Form with focus/restore, FormClosed cleanup and independent --settings named event to bring it up in existing tray.
- Fixed server tray status to use bounded asynchronous local readyz/router TCP probing instead of client stderr.
- Custom native icon has 16/24/32/48/64 Windows sizes, embedded in WinExe, both Inno scripts, forms, and green/amber/red tray state overlay.
- Self-contained win-x64 publish succeeded; --selftest, --test-icons, --probe-server against live DOMKAT all exit 0. Actual GUI upgrade, Inno CI and publication pending.
- source: owner reports, live DOMKAT inspection and local build.
- authority: owner-directive, verified-runtime.

## Native Settings window follow-up v0.2.4 (28.10.2026 MSK)
- v0.2.3 CI run 40723121794 succeeded, binary full/update release published and in-place update on DOMKAT completed with same Tunnel ID and DPAPI keys; bridge_ping and --probe-server exit 0, one tunnel-client, one Router Lite listener.
- Additional DOMKAT acceptance: v0.2.3 Settings callback logged success but desktop.window.list saw no window. Win32 EnumWindows found hidden Server Settings HWND. Explicit user32 ShowWindow(SW_SHOW) made the window visible and foreground; desktop.window.list confirmed HWND and bounds.
- v0.2.4 source adds native IsWindowVisible + ShowWindow(SW_SHOW) after Form.Show, and when reopening existing window. App compilation, --selftest, --test-icons and --probe-server all return 0; no live replacement yet.
- source: actual DOMKAT installed runtime and local .NET v0.2.4 tests.
- authority: verified-runtime.

## Approved settings UX pending v0.2.5 release (28.10.2026)
- Source modifications implementing accepted HTML mockups: main Server/Client settings contain no secret TextBox; temporary popup supplies secret, cleared on close; client lacks reveal/copy key functions; server can copy existing DPAPI client secret or generate/select replacement. Tunnel ID is editable only during first server setup; stored Tunnel ID remains unchanged thereafter.
- Client enters bare server hostname/IP, uses ws/wss existing scheme and automatic :55201/agent; server retains stored local listen address, currently 192.168.0.3.
- Buttons Проверить added: server checks local readyz + router TCP; client checks TCP reachability. Stored DPAPI key stays when user saves unrelated settings.
- Local self-contained Windows v0.2.5 build tested: --selftest exit 0, --test-icons exit 0, --probe-server exit 0, --test-secret-forms exit 0, git diff check clean.
- Owner explicitly requests full and update installer artifacts, to update DOMKAT personally via built-in app UI. Installed DOMKAT remains v0.2.3; do not run updater/remotely install.
- source: owner approval, local source build/tests 28.10.2026.
- authority: owner-directive, verified-runtime.

## v0.2.5 full+update installer release completed
- Main source commit 533d4ecb088957d3074bdb6da6810ffebf66e10a; GitHub Actions run 40732281198 completed success.
- Stable release https://github.com/lvlaksim1/chatgpt-mcp-tunnel/releases/tag/v0.2.5
- Full Setup: ChatGPT_MCP_Tunnel_Setup_v0.2.5.exe, SHA256 57b811467bf4a79a56ae8d8f99965ca36840745b853d357ece29a5a6e105f13a, 104967318 bytes.
- Update: ChatGPT_MCP_Tunnel_Update_v0.2.5.exe, SHA256 278140cb9ee4ed7a1bfcaa500a3b38d27937520185645cc30b6dc75c40ff112b, 104966353 bytes.
- Legacy alias ChatGPT_MCP_Agent_Update_v0.2.5.exe has identical binary/digest for existing built-in auto-updater.
- CI passed Windows PowerShell launcher checks, self-contained application/runtime builds, tests of secret-free forms and native app icon, first installation/update preservation/uninstall.
- DOMKAT installed program NOT updated at owner's explicit request. Current IP 192.168.0.3 preserved. Owner will install via built-in update controls.
- source: GitHub CI and release verified 28.10.2026, owner instruction.
- authority: verified-repository, owner-directive.

## v0.2.6 updater lock fix prepared (28.10.2026)
- DOMKAT installed v0.2.5; two updater IOException entries at 20:11:39 and 20:13:21. At 100% transfer the staging .part FileStream remained open while File.Move tried to rename it to .exe; Windows correctly denied rename.
- Fixed AgentUpdates.cs: await using scope closes FileStream before File.Move, check complete ContentLength, clean .part safely; new --test-updater-download code and Windows CI gate.
- Local Windows x64 tests: --test-updater-download 0; --selftest 0; --test-icons 0; --test-secret-forms 0; --probe-server 0. Fix does not modify installed app or running tunnel.
- Explicit user directive: do not install on DOMKAT. Owner requires full+update artifacts and manual launch of v0.2.6 Update EXE once because v0.2.5 automatic downloader is broken. Retain IP 192.168.0.3, existing OpenAI Tunnel ID and DPAPI secrets.
- source: owner screenshot, source, agent.log, local tests.
- authority: owner-incident, verified-runtime.

## DOMKAT LAN client diagnostic (28.10.2026)
- Server active: local IP 192.168.0.3, listener TCP 55201, OpenAI readyz 200.
- Windows Wi-Fi profile Public with inbound Block; no allow rule found for port 55201 or ChatGptMcpTunnel program. This can block MKATUSHONOK; direct MKATUSHONOK-side probe has not been run because only DOMKAT remote device is available.
- Server client secret DPAPI file timestamp 20:17:08; copies later at 20:18:45 logged without actual key. Previous client secret may be stale. RouterLite returns 401 for invalid secret, 409 for duplicate machine ID.
- Source v0.2.6 adds an explicit LAN-restricted Windows Firewall UAC button on server and descriptive client WebSocket error indicators; all local tests pass.
- User controls update; do not remotely install or change live firewall without their separate approval through UI.
- authority: verified-runtime, owner-incident.

## GitHub v0.2.6 release success (28.10.2026)
- Source commit 37702f749cd6b391a4eda4fa7766ed9c3a59a847; Windows Actions run 40736310056 completed success including installer update preservation, independent firewall/client diagnostics tests, full installation and uninstall.
- Stable https://github.com/lvlaksim1/chatgpt-mcp-tunnel/releases/tag/v0.2.6
- Full Setup ChatGPT_MCP_Tunnel_Setup_v0.2.6.exe SHA256 66a12e056afa3bf3822e878ded51f8d3bd32f96f13ba12faa94575bf8f17484d.
- Update ChatGPT_MCP_Tunnel_Update_v0.2.6.exe SHA256 2053ccadc645acfb0a79d5f7a3f4aef91453da8401db34f13bd97411805ede9b; compatibility ChatGPT_MCP_Agent_Update_v0.2.6.exe same digest.
- Installed DOMKAT was NOT upgraded or reconfigured by manager. Only after owner manual update and explicit Windows UAC can LAN firewall exception become active. MKATUSHONOK connectivity not independently verified.
- source: GitHub release/CI, owner request 28.10.2026.
- authority: verified-repository, owner-directive.

# Manager beliefs

## 29.10.2026: two distinct live ChatGPT threads and switch-back PASS
- New second local session chat_id 58f8d4f5ab3c4fea88382a4ab029967f received its own new ChatGPT thread via ui/message target:new, route waiting -> ready, and local message 477156c8b341421ebb5866f0bc4188d5 returned Status=answered, Reply=ВТОРОЙ.
- Switched GUI back to first local session chat_id c8d881eb8f0f4ff1aac24dcfe21a0a0c. Message 755051abb067448a9ff138bd2d082371 was forwarded and answered Reply=ПЕРВЫЙ in that same first session. Previous messages ПРИНЯТО and ПРОДОЛЖЕНИЕ remain visible only there. Thus 2 independent new ChatGPT threads, 2 associated local chat histories and switch-back have live end-to-end PASS.
- Browser open, ordinary ChatGPT MCP Apps, no Work/Codex/API or hourly cloud planner. Browser-closed recovery and MKATUSHONOK own local GUI not tested.

## 29.10.2026: v0.2.18 real ordinary ChatGPT multi-chat acceptance PASSED
- DOMKAT verified on v0.2.18.0 after controlled update; GitHub Actions Windows release 40926899682 SUCCESS; readyz=200, ChatGPT MCP Tunnel bridge_ping OK, computers.list DOMKAT and MKATUSHONOK connected, Windows Recovery watchdog Ready, all prior 142 message files retained.
- Authenticated Yandex Browser: ChatGPT MCP Tunnel > Manage > Refresh tools performed. In a new ordinary ChatGPT conversation the model invoked chat.ui.open. Model output and rendered app confirmed new resource URI ui://chatgpt-mcp-tunnel/relay-v0218 (cache fixed).
- Clicked Begin Forwarding in real MCP Apps, then started installed 0.2.18 GUI in preview-chat-window mode against real local Chat data and clicked New Chat. New local chat_id c8d881eb8f0f4ff1aac24dcfe21a0a0c persisted. Submitted test message marker 42F6, message_id 28d970531bc3438991bfb08816c94358. Sent via ui/message to new ChatGPT thread, iframe route state ready, ui-forwarded durable ACK created. chat.reply wrote Status=answered, Reply=ПРИНЯТО into correct local message.
- Second follow-up in same local chat marker C2A7, message_id 29d0c5d2b59b42bda27b3caf913ccbed: Reply=ПРОДОЛЖЕНИЕ, Status=answered, ui-forwarded present, same chat_id as first; verifies continuous relay after new-thread handoff.
- Via UIA selected 'История до обновления' and observed historical diagnostic messages; selected new chat and read only the two new tests (ПРИНЯТО, ПРОДОЛЖЕНИЕ), no legacy content. Classic local new chat/list/switch works in real GUI.
- Old per-hour ChatGPT automation stayed disabled, no Work/Codex/API used for transport. Active MCP Apps iframe still required; browser-closed recovery, remote MKATUSHONOK own local chats, multi-iframe robust lease remain future tasks.

## 29.10.2026: v0.2.18 MCP Apps cache-bust after live 0.2.17 test
- v0.2.17 Windows CI/release SUCCESS (40924254070), SHA256 Update c971f2f0652ec8e766181bfad7394d31f5a72c053a4b08e09ba0b845a34148e9, installed on DOMKAT with matched SHA, Windows watchdog Ready, readyz=200, both computers connected, 142 existing message files preserved.
- Live ordinary ChatGPT after plugin tool refresh showed mode mcp_apps_multi_chat for new chat.ui.open, but host rendered STALE 0.2.16 MCP Apps HTML from same ui://chatgpt-mcp-tunnel/relay URI. The installed 0.2.17 ChatRelayApp.html had new target:needsNew and no old 'Ожидание сообщений. Неотправленных: 0.' text; browser iframe displayed precisely old wording. This is real observed HTML resource caching.
- Implemented v0.2.18 cache-bust by versioned UI resource uri ui://chatgpt-mcp-tunnel/relay-v0218, updated registration and tests. Both .NET builds clean, original/extended MCP tests pass. Real new-cloud-chat target:new acceptance still pending refreshed 0.2.18 host.

## 29.10.2026: v0.2.17 multi-chat source implemented, pending Windows CI and real ChatGPT
- Owner approved v0.2.17 MCP autonomous chat manager and explicitly requested classic chat UI: '+ Новый чат', per-computer chat list, switch and separate histories. Implemented ChatSessionStore with 32hex chat IDs and on-disk sessions/active-chat records. New ChatWindow includes left sidebar, owner-drawn chat titles/previews, chat selection and isolated in-memory drafts/attachments; old message files without ChatId stay untouched and appear in 'История до обновления'. Replies remain tied to original message IDs.
- MCP route updated: chat.receive now includes chat_id; chat.ui.open(chat_id), pending(chat_id,discover) and ack(new_chat) support new ChatGPT thread handoff via ui/message target:new and new iframe auto-start via ui/notifications/tool-input. Existing unscoped widget calls only see legacy chat so cannot silently consume messages from unrelated local chats. Route heartbeats and bootstrap waiting markers retained under Chat/ui-routes.
- Local .NET Agent and MCP Server build with 0 warnings/errors. Isolated --test-chat-sessions PASS, Python routing PASS, Node target:new PASS, previous bridge tests PASS. Also manually opened preview process on DOMKAT isolated temp root, created two chats, sent messages, switched back through UIA and verified displayed selected history. Preview process and temp data cleaned. Production DOMKAT remains v0.2.16 untouched.
- Real ChatGPT acceptance of ui/message target:new and new chat iframe auto-start not yet done; do not claim completed without user-facing browser test. Remaining reliability: cross-machine shared chats, browser-closed bootstrap, longer-lifetime recovery.

## 29.10.2026: REAL ordinary ChatGPT ui/message acceptance 19:36-19:42 MSK
- Actual ordinary ChatGPT fresh browser session exposed new 52-tool catalog, invoked chat.ui.open and rendered real MCP Apps iframe with hostCapabilities.message.text and experimental.openai/message.
- Manual click probe sent user message nonce 58ewppk5 and ChatGPT answered with matching nonce, proving real ui/message new model turn.
- Once Start relay was clicked, synthetic DOMKAT queue nonce 43830fd3 auto-forwarded and ChatGPT replied with chat.reply; queue message Status=answered and durable ACK=true.
- Real WinForms ChatWindow opened by double-click on DOMKAT tray icon. Message GUI8C42 was submitted via actual text field and Send, automatically forwarded by iframe and answered via chat.reply; actual GUI history showed the returned reply, JSON Status=answered.
- Browser ChatGPT tab was switched to a different tab, real WinForms GUI message BGTAB42 still automatically forwarded and answered via chat.reply with ACK=true, then original browser tab was restored.
- Tests performed ~19:36-19:42 MSK, not at scheduled minute 44. Polling works only while the host iframe is alive, NOT proven with browser closed, host suspended or infinite duration.
- GUI footer still incorrectly reports awaiting hourly schedule despite immediate new replies; needs fix. Installed DOMKAT 0.2.16, MKATUSHONOK local GUI untested.

## 29.10.2026: DOMKAT production update 0.2.16 and ChatGPT browser tool refresh
- Owner explicitly authorized installing v0.2.16 on DOMKAT and refreshing connected action catalog via browser.
- Installed on DOMKAT with verified release SHA-256 FC05177A63EEC63E28BEE080247ED7B7835A3758098D77D072EFE9253696DBA0. Initially Inno Setup silently returned code 5 because RestartManager could not close running agent/runtime/tunnel-client (confirmed installation log); no files updated during those attempts. Successful controlled install paused only existing watchdog, invoked agent --shutdown (exit 0), waited for its own three processes to exit, ran official updater and restored existing watchdog.
- Verified resulting file version 0.2.16.0, independent local readyz HTTP 200, bridge_ping OK, DOMKAT and MKATUSHONOK connected, watchdog Ready, ChatRelayApp.html installed. All other user apps and unrelated untracked TXT files untouched.
- Via user's open Yandex browser (authenticated ChatGPT), navigated Plugins > ChatGPT MCP Tunnel > Manage and invoked 'Обновить инструменты'; button disabled during request then re-enabled without visible error. This confirms browser refresh action was invoked, NOT definitive proof this already-running chat switched from cached 45 to new 52. The actual 52-command runtime is source/CI-verified; live new-conversation availability pending.

## 29.10.2026: v0.2.16 stable Windows release verified (not live ChatGPT accepted)
- Verified 29.10.2026: GitHub Actions 40907216014 SUCCESS; published stable v0.2.16 with Setup SHA256 efda9954ec9a519ebfca5f0364276e4c0bb58386b50bff63b1e690d11dca23ab and Update SHA256 fc05177a63eec63e28bee080247ed7b7835a3758098d77d072efe9253696dba0. Real ChatGPT host iframe remains untested, installed DOMKAT 0.2.13 as last observed.

## B26 — 29.10.2026: Independent ui/message diagnostic probe added
- Source prototype v0.2.15 CI/release PASS, but installed DOMKAT still v0.2.13 and ChatGPT tool list 45. Added a separate explicit-click ui/message probe to differentiate host-level send support from timer-driven forwarding, plus host capability reporting. Local mocked host tests and 52-tool MCP protocol tests pass; real ChatGPT acceptance pending.

## B26 — v0.2.15 Windows release verified 29.10.2026
- GitHub Actions run 40904368593 SUCCESS; published stable v0.2.15 from commit 59fea9657732ae500a1834c40437471459f7b0cf. Full Windows Setup/Update/selftest/installer preservation acceptance complete. Update SHA-256 2d4b44f88fcefb4560c750d09148b2795fcdd2dba12b7d44afc83babd60f94bd; Setup SHA-256 be1ce88211544aec8447034e5ba92a1b0bd573c8b9a73c762e71c7485ee61f03. Live ChatGPT host ui/message acceptance remains unverified; installed DOMKAT is still v0.2.13.

## B25 — MCP Apps ui/message prototype source-tested 29.10.2026
- Owner explicitly instructed implementing ui/message without API, Codex, Work, browser automation or scheduler.
- Official MCP Apps specification establishes ui/message as a request FROM A CURRENTLY RUNNING MCP APP VIEW to its host; openai/message metadata supports target=active, send=true. It is NOT a server-originated trigger for closed ChatGPT.
- Implemented chat.ui.open tool with _meta.ui.resourceUri and openai/ui global/thread entrypoints, resources/read HTML, app-only chat.ui.pending/ack, user-enabled polling, and automatic ui/message; the original queue and attachments remain untouched, ack saved only after host success.
- Verified local MCP tools/list 52 tools, resources/read HTML, isolated queue and idempotent ack; JavaScript MCP host simulation accepted ui/message and returned ack, and denied ui/message without acknowledgment. Two .NET builds zero errors/warnings; prior stdio MCP and Router Lite 23 remote tools/reconnect tests pass.
- Critical: genuine ChatGPT host acceptance and widget lifetime are UNVERIFIED. This is an experimental build, not a proof of anytime access. Existing deployed app not updated.

## B24 — stable v0.2.14 Windows release accepted 29.10.2026
- GitHub Actions run 40866882778 completed SUCCESS on commit 21a1a0c5218a9425d00ba01c866deb95ce94f54d; tests passed: original stdio 49 tools, Router Lite 23 remote tools and reconnect, new attachment text/image/integrity tests, self-contained UI tests, Inno Setup full/update and clean install/update-preservation/uninstall.
- Stable GitHub Release v0.2.14 published with 3 verified assets: Setup SHA256 7ff5afc44db3a535b71e11b6f174141684f666dd0396c303fa03ee64d2d8174d, Update and legacy alias SHA256 ef4899062bde3caecb85fc29d23c03a155ea78d66902d6d49da761bf4f4504ef.
- Both physical hosts were NOT updated by manager; observed DOMKAT v0.2.13 still responds to bridge_ping. Owner-side physical clipboard/image UI test and ChatGPT plugin catalog refresh outstanding. This supersedes prior v0.2.14 Windows CI pending belief but not physical acceptance.

## B23 — Chat attachments tested in source 29.10.2026
- Owner explicitly authorized file attachments and clipboard images in existing MCP Tunnel chat window.
- Source: multi-file selection, Explorer drop, Ctrl+V image, preview/remove, attachment-only messages; copied payloads under per-message IDs with SHA-256 and JSON metadata. New chat.attachment.read returns real MCP image block, bounded UTF-8 text, or locally verified binary path. chat.receive includes metadata.
- Verified: local Windows .NET builds zero warnings/errors, ChatWindow attachment selftest PASS, isolated MCP attachment E2E PASS (PNG, UTF-8, invalid identifier and SHA-256 mismatch), baseline MCP 49 tools, Router Lite all 23 remote Windows functions and reconnect PASS.
- This is SOURCE-tested, not installed on user computers. Installed v0.2.13 remains unchanged, CI/release v0.2.14 and physical clipboard/UI acceptance pending. ChatGPT tool catalog currently 45, refresh required to expose new command.
- Evidence: direct owner instruction and verified local test results 29.10.2026.

## Verified two-host WSS file transfer, 29.10.2026
- 324820992-byte signed MAX.msi copied from MKATUSHONOK to DOMKAT over the authenticated WSS Router Lite, SHA-256 verified by independent receiver hash, and resumed after an observed reconnect at 80.7%.
- Same-user transfer CLI v0.2.10 allows remote transfer start/status without relaying binary data through model responses. ChatGPT connector currently still enumerates 41 original tools, not the 3 new methods.


## Router Lite file transfers — 29.10.2026
- Large binary files are streamed within existing authenticated WSS, never through model MCP text responses.
- Transfer tool family: file.transfer.start/status/cancel. Server orchestrates both directions using internal bounded RPC.
- Destination partial and metadata track size/hash; final save requires matching SHA-256. Safe default is no overwrite.
- Physical WAN two-device acceptance remains pending after isolated E2E 90 MiB PASS.


## B22 — Direct WSS via public WAN is selected and v0.2.8 released (29.10.2026)
- Решение владельца 29.10.2026: DOMKAT имеет статический публичный IPv4 на WAN-интерфейсе маршрутизатора; удалённый клиент расположен в другой локальной сети. Развивается прямой защищённый WSS без обязательного Tailscale/посредника. Локальный адрес DOMKAT не заменяется внешним IP. Источник: прямая команда владельца; код основной ветки f8cc66848a475030b63ad5c15e25ec6c68e76308.
- Сервер Kestrel обслуживает защищённый порт TCP 55202 в дополнение к прежнему локальному TCP 55201. Сервер создаёт собственный TLS-сертификат, шифрует пароль PFX через DPAPI Windows, клиент проверяет точный SHA-256 отпечаток до передачи общего ключа. Новый режим требует минимум 32 символа общего ключа. Проверены сборки, 42 инструмента, 23 направляемых инструмента Windows, запрет неверного сертификата, выполнение удалённой команды по WSS, восстановление связи, сохранение старого LAN-подключения; GitHub Actions 40781905752 завершён успешно; v0.2.8 опубликована с установщиками Setup и Update.
- Проверка 29.10.2026 через работающий MCP Tunnel: активный процесс ChatGptMcpAgent.exe PID 14536 имеет FileVersion 0.2.7.0 (запущен 2026-10-29 00:33:36 по системному времени Windows); Router Lite слушает 192.168.0.3:55201 (PID 2992), TCP 55202 не слушает. v0.2.8 не устанавливалась менеджером; bridge_ping успешен. Этот факт supersedes прежний текущий статус v0.2.6, но не меняет вывод о непроверенном межсетевом соединении. На маршрутизаторе не создавался проброс TCP 55202, правила брандмауэра не менялись. Физическая связь с MKATUSHONOK через Интернет и 23 удалённых инструмента остаётся НЕ ДОКАЗАНА. Публичный WAN IPv4 подтверждён чтением роутера; постоянство адреса основывается на словах владельца.
- authority: owner-directive, verified-repository, verified-runtime; supersedes earlier 'direct WSS not yet chosen', does not supersede physical acceptance gate.

## WAN verification changes architecture options — 28.10.2026
- Owner reports a static public IP for DOMKAT. Read-only verification directly asked the RT-GM2-3 router via local UPnP WANIPConnection:GetExternalIPAddress; router WAN IPv4 is public and matches three public IP-checking services, including direct curl without HTTP proxy. Numeric public IP deliberately excluded from this public repository. Consequently CG-NAT is not present at that router's WAN IPv4 interface; the proxy/TUN adapter is irrelevant to this independent WAN reading.
- This proves publicly addressed WAN, NOT contractual static permanence. Stable assignment is the owner's statement, not independently proved by one snapshot.
- Router UPnP GetSpecificPortMappingEntry for external TCP 55201 and 443 both returned SOAP fault 714, meaning no corresponding UPnP mappings. This does not rule out manually configured router forwarding. The active DOMKAT Windows Firewall rule ChatGPTMCPRouterLiteLAN only allows remote 192.168.0.0/24, local 192.168.0.3, TCP 55201; Router Lite listens only on 192.168.0.3:55201.
- Direct connection from remote MKATUSHONOK over the Internet without a Tailscale/Headscale/VPS middle server is technically feasible if port forwarding, firewall and secure TLS/WSS + authenticated remote agent are deployed. Current ws:// bearer-key router MUST NOT be directly exposed to the Internet. No external-to-internal reachability test was executed; physical MKATUSHONOK remains NOT PROVEN.
- Tailscale-capable v0.2.7 remains valid and released as an option; direct WSS architecture is not yet approved or implemented.
- Source: read-only DOMKAT shell, router IGD SOAP, network firewall and listener checks, owner statement; authority: verified-runtime, owner-directive.


## B21 — Защищённый Tailscale-транспорт выбран и поддерживается выпуском v0.2.7 (28.10.2026)
- Владелец поручил выполнить план после поправки о разных локальных сетях. Для минимальной архитектуры с одним OpenAI MCP Tunnel выбран вариант защищённой виртуальной сети Tailscale/WireGuard между DOMKAT и MKATUSHONOK; второй облачный туннель, общедоступный незащищённый порт и отдельный сервер-посредник не требуются.
- Реализована ограниченная одним IP клиента настройка брандмауэра для адреса Tailscale сервера (100.64.0.0/10) с отдельным подтверждением и Windows UAC. Предполагается реально назначенный локальному Tailscale-адаптеру IPv4 и точный IP другого устройства. Обычный локальный режим не изменён.
- Source commit c04e2ec44c287d4f41ee4433f05b728609ed94ae; локальные сборка и семь самоиспытаний, MCP 42 tools/Router Lite/reconnect успешны. GitHub Windows Actions 40756470762 completed/success: компиляция, тесты, реальный Inno Setup full/update/uninstall. Stable GitHub Release v0.2.7 опубликован с Setup/Update EXE и SHA256.
- По состоянию после публикации: на DOMKAT установленный рабочий процесс всё ещё v0.2.6; никакого обновления или настройки сети в рабочей машине от менеджера не было. Отдельного подтверждённого Tailscale-подключения DOMKAT — MKATUSHONOK нет; обе машины ещё должны быть авторизованы в одной tailnet и проверены физически. На DOMKAT до реализации Tailscale-адаптер/служба не обнаружены.
- source: прямая Owner-команда 28.10.2026, проверенные исходники, локальные испытания, Windows CI и GitHub Release v0.2.7; authority: owner-directive, verified-runtime, verified-repository.
- supersedes: раннее представление, что способ соединения между сетями ещё не выбран; не supersedes: физическая приёмка остаётся NOT PROVEN.


## B20 — DOMKAT и MKATUSHONOK находятся в разных локальных сетях (28.10.2026)
- Установленный владельцем факт: компьютеры DOMKAT и MKATUSHONOK НЕ в одной локальной сети. Адрес DOMKAT 192.168.0.3 является частным адресом его локальной сети, а не проверенным межсетевым адресом клиента. Прежние рекомендации подключать удалённый MKATUSHONOK непосредственно к ws://192.168.0.3:55201/agent и нажать кнопку разрешения локальной сети недостаточны и не должны повторяться без дополнительного защищённого маршрута.
- Межсетевой транспорт пока не выбран владельцем. Возможности: согласованная шифрованная виртуальная сеть (например, Tailscale/WireGuard) либо безопасный общедоступный WSS-конечный узел/ретранслятор. Наличие публичного IP, проброса портов и VPN на MKATUSHONOK не установлено. На DOMKAT при проверке не обнаружены службы/адаптеры Tailscale, WireGuard, ZeroTier, OpenVPN, NetBird.
- По исходникам v0.2.6 клиентская проверка допускает ws://100.64.0.0/10, сервер разрешает адрес только назначенного компьютеру IPv4, а кнопка LanFirewall.LocalNetwork принимает лишь 10/8, 172.16/12, 192.168/16 с ограничением маски. Следовательно, стандартная кнопка локального брандмауэра не является решением для Tailscale; реальная работа требует доступного VPN-адреса интерфейса, безопасной политики брандмауэра и реальной проверки.
- Существующий OpenAI MCP Tunnel не следует считать автоматически предоставляющим маршрут между двумя Windows-агентами: функция облачного туннеля — подключение ChatGPT к основному MCP-серверу; отдельный маршрут к агентам всё равно нужен.
- source: прямая поправка владельца 28.10.2026; проверенные исходники AgentStorage.cs, SettingsDialog.cs, ServerSettingsDialog.cs, LanFirewall.cs, Program.cs и наблюдение DOMKAT VPN-служб.
- authority: owner-directive, verified-repository, verified-runtime. supersedes: B17 как достаточное объяснение проблемы подключения и неверное предположение об общей LAN в прежних планах.


## B19 — Verified DOMKAT running version 0.2.6 (28.10.2026 22:20 MSK)
- statement: The running WinForms agent PID 30196 on DOMKAT is D:/ChatGPTMCPAgent/ChatGptMcpAgent.exe (FileVersion 0.2.6.0; started 20:38:25 MSK); router listens on 192.168.0.3:55201. ChatGPT MCP bridge_ping succeeds. Physical MKATUSHONOK connection remains unverified and no established client TCP connection was observed during the snapshot. Current chat exposes 41 connector tools versus 42 locally advertised earlier.
- source: original ChatGPT MCP Tunnel read-only shell.exec process/version/TCP checks; bridge_ping; GitHub v0.2.6 release and Actions run 40736310056, all 28.10.2026.
- authority: verified-runtime, verified-repository.
- supersedes: pending installation of v0.2.6 in prior handoff; does not prove network acceptance or UI quality.


## B1 — Identity and baseline
- statement: The manager's stable ID is `project-manager`, its authoritative manager and product branches are both `main`, and it governs only `lvlaksim1/chatgpt-mcp-tunnel`.
- source: `.context/manager/identity.json`, `.context/manifest.json`, live GitHub branch `main` at `e6b79a1d8977e777fc1e15faf4936d0880dd44e5` before Router Lite development.
- authority: verified-repository.

## B2 — Original Windows implementation
- statement: The existing Windows MCP server has 41 working/discoverable tools (8 system/file/process, 15 graphical, 17 GitHub, and `bridge_ping`); the original tunnel remains based on .NET 8 and official `stdio` transport.
- source: repository `README.md`, existing tests and independent live source review on 28.10.2026.
- authority: verified-repository, verified-runtime.

## B3 — Owner's Router Lite decision
- statement: One OpenAI tunnel and one MCP plugin must manage multiple computers. Add a small router to the existing project, run local commands on DOMKAT by default, connect agents from remote PCs, avoid a database, additional projects and complex infrastructure.
- source: Owner dialogue 28.10.2026, direct acceptance and instruction to start development.
- authority: owner-directive.
- supersedes: previous idea of one tunnel per computer and enterprise-style architecture.

## B4 — Prototype scope, not production readiness
statement: Router Lite stages 1-3 passed tests on DOMKAT: 42 tools, optional `computer` on all 23 Windows tools, remote files/shell/processes/GUI/UIA/PNG, concurrent requests, wrong key and duplicate name rejection, and reconnect after router restart; physical second PC not yet checked.
source: isolated Release build; test_mcp_stdio.py, test_router_lite.py, test_router_reconnect.py, test_windows_tools.py, test_gui_input_tk.py, test_windows_uia.py, test_winforms_uia_compat.py, test_uia_real_apps.py (28.10.2026).
- authority: verified-runtime.


## B5 — Agent installer reference and current status
- statement: Owner chose MailRu Desktop's per-user Inno Setup full/update design for the Router Lite agent. The WinForms tray host, self-contained runtime, Inno Setup full/update installers and CI acceptance have passed and numeric v0.1.0 is published; second physical PC is not yet proven.
- source: Owner confirmation 28.10.2026; MailRu Desktop `installer/*.iss`, `MailRuDesktop.UpdateAgent`, release workflow; local `dotnet publish`, `--selftest`, MCP/router tests.
- authority: owner-directive, verified-repository, verified-runtime.

## B7 — Installation directory
- statement: The Owner requests a user-selectable installation directory; server suggests drive D and client suggests drive C. Update must preserve the chosen directory.
- source: Owner directive 28.10.2026, installer source and CI tests.
- authority: owner-directive, verified-repository.

## B8 — universal installer release acceptance
- statement: Single Server/Client Windows full installer v0.2.0 was published after CI successfully tested the role-based default/custom installation directories and update/uninstall operations. Existing live DOMKAT installation remains unchanged.
- source: GitHub Actions 40696034380; GitHub Release v0.2.0; observed on 28.10.2026.
- authority: verified-repository, verified-runtime.

## B9 — Same OpenAI tunnel identity
- statement: Owner explicitly rejected creating new OpenAI tunnels; clean DOMKAT migration retains existing logical Tunnel ID and ChatGPT plugin while removing old local runtime, profiles and credentials.
- source: Owner correction 28.10.2026 and official tunnel-client CLI help.
- authority: owner-directive, verified-runtime.

## B10 — Recovered live DOMKAT same tunnel
- statement: The new DOMKAT Server installation preserved the original OpenAI Tunnel ID and encrypted credentials, but native stderr merging in Windows PowerShell 5.1 caused repeating early termination. Direct native invocation restored the same OpenAI MCP tunnel and Router Lite. 42 local tools tested, original bridge ping, GitHub and Windows tools work, both health endpoints return 200.
- source: direct Remote Desktop Commander investigation and actual recovered MCP calls 28.10.2026.
- authority: verified-runtime.

## B11 — Stable recovery and v0.2.2 live installation
- statement: Windows CI run 40712500651 succeeded and v0.2.2 full/update installers published. v0.2.2 update SHA256 verified, installed on live DOMKAT, existing OpenAI Tunnel ID and DPAPI credentials preserved. New app v0.2.2 responds to bridge_ping, system.info and 42 local MCP tool tests; single tunnel-client and Router Lite bound to 192.168.0.3:55201, healthz/readyz HTTP 200.
- source: GitHub Actions 40712500651, Release v0.2.2, actual Desktop Commander update result and original ChatGPT MCP tool verification on 28.10.2026.
- authority: verified-repository, verified-runtime.

## B12 — GUI defects and icon
- statement: On DOMKAT the server tray had a false offline status although the same OpenAI tunnel was healthy. The Settings menu also appeared inert because a hidden modal Server Settings Form was already present. The user additionally requested an appropriate application icon.
- source: Owner screenshots 28.10.2026, DOMKAT live bridge_ping/readyz/router checks, Win32 enumeration, .NET source inspection and local published win-x64 test.
- authority: owner-directive, verified-runtime.

## B13 — Native Win32 window visibility defect confirmed
- statement: Modeless v0.2.3 Settings callback logged opening its Form, but the native top-level HWND remained hidden without WS_VISIBLE. Manually calling user32 ShowWindow(SW_SHOW) on the actual DOMKAT Server Settings HWND changed visibility to true and desktop.window.list then found the foreground window. v0.2.4 source verifies native IsWindowVisible and applies SW_SHOW after showing and reopening.
- source: live DOMKAT Win32 EnumWindows, ShowWindow and desktop.window.list evidence 28.10.2026, self-contained win-x64 v0.2.4 build/probe tests.
- authority: verified-runtime, owner-directive.

## B14 — Approved secret UX and independent release
- statement: User approved native settings forms matching interactive HTML mockup: secret values are absent from primary forms, supplied only through an on-demand temporary entry dialog. On client only key-entry is permitted, never reveal/copy; on server separate token replacement and client-key generation/copy. No port entry, use 55201. DOMKAT current address 192.168.0.3 stays unchanged until separate DHCP discussion. User requires full and update installer release, but will self-update the installed DOMKAT application using its built-in updater.
- source: user decisions 28.10.2026, verified local v0.2.5 win-x64 build and selftest/icon/status/forms tests.
- authority: owner-directive, verified-runtime.

## B15 — Stable v0.2.5 binaries published, owner self-updates
- statement: v0.2.5 source commit 533d4ecb088957d3074bdb6da6810ffebf66e10a produced two Windows Inno binaries, full ChatGPT_MCP_Tunnel_Setup_v0.2.5.exe SHA256 57b811467bf4a79a56ae8d8f99965ca36840745b853d357ece29a5a6e105f13a and Update ChatGPT_MCP_Tunnel_Update_v0.2.5.exe SHA256 278140cb9ee4ed7a1bfcaa500a3b38d27937520185645cc30b6dc75c40ff112b. Windows GitHub Actions run 40732281198 completed success, including install/update/preservation tests. Owner explicitly performs actual DOMKAT update personally; installed app remains untouched by manager.
- source: GitHub Release v0.2.5, Actions 40732281198, owner directive 28.10.2026.
- authority: verified-repository, owner-directive.

## B16 — verified v0.2.5 updater IOException root cause
- statement: Windows file-sharing conflict in v0.2.5 downloader occurs because File.Move on staged .part happens before disposing its FileStream. User screenshot shows 100% and file-in-use error; DOMKAT Logs/agent.log has Update failed IOException 20:11:39 and 20:13:21 on 28.10.2026. DOMKAT installed v0.2.5. Fix v0.2.6 releases writer before move and adds --test-updater-download, local tests success.
- source: owner screenshot, read-only DOMKAT log/source inspection, local compilation/tests.
- authority: owner-incident, verified-runtime.

## B17 — Server LAN access likely blocked by Windows Firewall
- statement: On DOMKAT at 20:20 MSK Router Lite listened on 192.168.0.3:55201 and OpenAI readyz returned 200, while Wi-Fi used Public firewall profile with BlockInbound and no app/port 55201 inbound allow rule. This is a plausible client connectivity blocker, but independent network/authorization test from MKATUSHONOK has NOT been performed. DPAPI server key file updated 20:17:08, so clients require latest shared key. v0.2.6 adds optional on-demand UAC firewall rule scoped to current LAN subnet and client error interpretation; local tests pass.
- source: read-only DOMKAT network/firewall/process/log checks, verified code and local tests 28.10.2026.
- authority: verified-runtime, owner-incident.

## B18 — Stable Windows v0.2.6 updater/LAN release verified
- statement: Windows Actions run 40736310056 completed success on commit 37702f749cd6b391a4eda4fa7766ed9c3a59a847. Stable Release v0.2.6 contains Setup SHA256 66a12e056afa3bf3822e878ded51f8d3bd32f96f13ba12faa94575bf8f17484d and Update SHA256 2053ccadc645acfb0a79d5f7a3f4aef91453da8401db34f13bd97411805ede9b; legacy updater alias identical. Local fixes include close staging file before rename, optional LAN-only Windows Firewall rule button requiring UAC, error states for WebSocket 401/409/network. Actual MKATUSHONOK-to-DOMKAT connectivity remains UNVERIFIED until owner installs and tests; installed DOMKAT left untouched by project manager.
- source: release API, CI run and owner update instruction 28.10.2026.
- authority: verified-repository, owner-directive.

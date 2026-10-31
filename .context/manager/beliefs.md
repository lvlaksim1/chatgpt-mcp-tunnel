## Confirmed v0.2.65 VPN work (31.10.2026 20:18 MSK)
Owner instructed completion of remaining v0.2.64 VPN work and GitHub archival. v0.2.65 is installed on DOMKAT, deployment status VERIFIED, GUI PID6152, proxy bridge PID9368, original official tunnel-client PID8624 and MCP runtime PID33264 preserved. MCP bridge_ping succeeded after deployment, Router Lite remote client MKATUSHONOK remained connected. Existing Happ mode remains default, unified authorized ChatGPT WebView2 profile confirmed signed-in Plus with original chats after installation (screenshot inspected). Three-hour task FreeRealityRefresh was manually started: result 0, verified THREE live VLESS/REALITY nodes among eight probed, stored primary and two separate CurrentUser DPAPI-encrypted standby profiles under LocalAppData; no raw VLESS URIs or UUIDs in status/catalog or version control. Updated bridge tries a pretested standby using separate Xray loopback 11822 before Happ fallback, then promotes it to standard port 11821, closes stale MCP sockets for reestablishment, preserves one official client. Isolated forced-primary-failure test PASS: auto notice free-node-rotated, remained free and api.openai.com returned expected unauthenticated HTTP401 through standby, production client PID8624 unaffected and production mode Happ. Five isolated stress cycles Happ-Free-Happ: 15/15 HTTPS requests succeeded without any production MCP client/bridge restart. Installed GUI TrayContext displays user-facing Windows balloon for free node rotation or fallback using safe reason codes and fresh notice timestamps. Injected event test on installed GUI PASS, one log entry for free-node-rotated; test notice removed afterwards. Seven Python regression scripts PASS including DPAPI standby preservation across source outage, six published binary selftests PASS, and all three full/setup/update installers SHA256 verified. Original signed-in browser backup under .tools/deployment-backups/pre-v0265-standby-vpn, never publish. Unused legacy WebView2-FreeReality separate browser profile (383 files, 19.4MB) deleted after verifying not in use and original signed-in profile preserved. Technical evidence docs/VPN-STANDBY-FAILOVER-V0265-2026-10-31.md. No actual hours-long public-node outage simulated against live authenticated browser; isolated failure and real proxy HTTPS tests passed.

---

## Verified unified VPN v0.2.64 (31.10.2026 19:40 MSK)
Owner specifically required ONE pre-existing authorized WebView2 ChatGPT profile in BOTH Happ and Free Xray modes; separate new browser profile prohibited. v0.2.64 installed on DOMKAT GUI PID7752, bridge PID38032. Current single authorized UDF remains %LOCALAPPDATA%/ChatGPTMCPAgent/WebView2/ChatGPT (EBWebView child). VpnModeController.BrowserProfileFolder now always returns original folder. Switch waits asynchronously on CoreWebView2Environment.BrowserProcessExited after disposing previous WebView2 controls, before creating the different proxy-configured browser process. Free selection preflights HTTPS via server bridge before GUI rebuild and suppresses timer races via IsTransitioning flag. GUI also watches server route mode and recreates browser after external watchdog failback. Server ChatGptMcpProxyBridge now monitors free HTTPS health every 20s and atomically switches to Happ after 3 failures (isolated forced-failure test PASS, Happ HTTP200). Free node update DPAPI profile is preflighted using Xray isolated port11822 before active Xray at11821 is replaced (isolated live HTTPS test PASS, Xray PID35640->26492, official MCP PID8624 unchanged). Real installed GUI test Happ->Free->Happ showed SAME authenticated ChatGPT Plus account and full existing history in both modes, no second login, same original UDF, official MCP bridge_ping OK and MKATUSHONOK RouterLite client connected during Free; GUI and server route both changed coherently. Another real GUI test simulated external failback by changing saved mode Free->Happ: app autonomously reopened original authorized profile with VPN:Happ and no login, while original MCP/tunnel-client PIDs unchanged. Final GUI v0.2.64, client PID8624, Runtime PID33264, bridge PID38032, mode Happ, free listener stopped. Full Setup/Update local artifacts SHA256 3/3 verified. One initial deploy failed due reserved PowerShell $PID function parameter; original v0.2.63 and MCP were restored, bug fixed, second autonomous deployment verified. Source and binary/profile backup under .tools/deployment-backups/pre-v0264-single-profile-retry, auth profile ~219MB, NOT in Git. No GitHub release was published. The previous v0.2.63 legacy unused FreeReality separate profile was not deleted. Full report docs/VPN-SINGLE-AUTH-PROFILE-FAILBACK-V0264-2026-10-31.md.

---

## VPN E2E verified facts — gen 70 (31.10.2026 18:58 MSK)
Owner instructed final verification of DOMKAT Happ/Free switch, performed live on v0.2.63. Autonomous rollback protected free MCP tests: first transient example.com failure but free control-plane api.openai.com/v1/models HTTP401, MCP /readyz 200 and tunnel-client TCP socket to bridge 11827 with bridge mode=free; second complete autonomous acceptance PASS all criteria (example.com HTTP200 via Free Xray, api.openai.com HTTP401, /readyz HTTP200, bridge free mode, client TCP socket connected, unchanged client PID8624 and bridge PID28024, successful rollback to Happ HTTP200). Live GUI topbar switch Happ->Free with explicit warning confirmed, ChatGPT public guest page loaded through separate Free WebView2 profile; isolated installed browser smoke test returned 0. Live MCP bridge_ping returned OK and Router Lite computers_list retained DOMKAT + MKATUSHONOK when mode=free, with matching bridge health free metric 67 connections. Isolated invalid-mode bridge test returned HTTP503 fail-closed and Happ HTTP200 after restoring, without touching production listener. A temporary mismatched GUI status after an external guardian rollback revealed a genuine defect: external mode-file changes do not automatically recreate already-open WebView2; first native Happ click was ignored because persisted mode already Happ. Correctly restored GUI via native switch, screenshot confirms authenticated ChatGPT Plus and VPN:Happ restored, all original chats. Final status 18:57 MSK: GUI version0.2.63, GUI PID5060, tunnel-client PID8624, runtime PID33264, bridge PID28024, persisted mode Happ, bridge observed Happ, free port 11821 stopped, refresh task 0. Still NOT completed: authenticated Free-profile ChatGPT conversation/login; automatic failback to Happ on remote Free node outage; hot-reload of Free Xray after subscription update; browser synchronization on external automatic mode change. Unknown public node operator and transient first failure mean reliability/trust remains uncertain. Read docs/VPN-E2E-ACCEPTANCE-2026-10-31.md. No new release/commit.

---

## DOMKAT bridge verified (31.10.2026 18:10 MSK)
Owner affirmed server-only external MCP VPN switching on DOMKAT; remote Router Lite clients must not run any tunnel-client. Verified native OpenAI tunnel-client 0.0.18 supports --http-proxy for control-plane, MCP and Harpoon. Implemented independent .NET8 ChatGptMcpProxyBridge.exe at loopback 127.0.0.1:11827 with HTTP CONNECT forwarding to Happ local HTTP 127.0.0.1:10809 or Free Xray HTTP 127.0.0.1:11821 based on existing GUI network-mode.json. Bridge monitors mode changes every 500ms and closes old persistent TCP tunnels to force Go reconnect on new route without restarting tunnel-client. Bridge controls no OS-wide routes or VPN adapters, no second official MCP client. ServerStart.ps1 starts/validates bridge only for DOMKAT server role, launches official existing tunnel-client with --http-proxy http://127.0.0.1:11827. Remote RouterLite --agent role unchanged. Isolated pilot 3/3 HTTPS example.com HTTP200 Happ->Free->Happ through SAME bridge PID. v0.2.63 deployed locally using detached backup-and-rollback script; deployment status VERIFIED. New tunnel-client PID8624 (command line verified proxy flag), MCP runtime child PID33264, bridge PID28024, GUI PID5060 (v0.2.63), Happ PID9908 untouched. Local bridge-health.json reported mode=happ, successfulUpstreamConnections=5, one established local bridge socket observed; proves live control-plane uses bridge. MCP bridge_ping returned CHATGPT_MCP_TUNNEL_PING_OK post reconnect. Router Lite computers.list shows DOMKAT local and MKATUSHONOK remote CONNECTED. Three v0.2.63 Setup/Update installers verified SHA256 3/3. Scheduler free-server refresh still active, next due 2026-10-31 19:28 MSK. Limitations: authenticated production ChatGPT/MCP over unknown public Free exit not tested; automatic failback on remote Free outage not implemented, Free mode currently fail-closed and can return to Happ via selector; in-use free Xray not hot-reloaded upon scheduler profile updates. Original Happ and clients unchanged; backup at .tools/deployment-backups/pre-v0263-mcp-proxy. Details docs/MCP-SERVER-VPN-BRIDGE-V0263-2026-10-31.md. Local source uncommitted; do not reset.

---

## Verified network mode GUI (31.10.2026 17:08 MSK)
The owner requested the user-facing two-way VPN selector Happ (current) or free servers. Installed GUI v0.2.62 on DOMKAT with top titlebar dropdown and equivalent tray recovery menu. Default remains Happ and existing authenticated WebView2 profile remains untouched. When the owner explicitly chooses Free, a privacy consent prompt warns about unknown public node operators. The program checks the scheduler's latest 6h verified Free-Reality-Auto.dpapi, Windows CurrentUser DPAPI decrypts the Xray JSON, validates loopback-only proxy and VLESS/REALITY, checks bundled Xray-core v26.10.29 SHA256 and starts its own Xray process at 127.0.0.1:11821. A distinct FreeReality WebView2 user-data folder prevents Chromium proxy argument conflicts and may need new login. On mode choice only the GUI form is recreated; original adopted tunnel-client PID 24884 and MCP runtime PID 28372 remain alive, Happ is not stopped. Switching back to Happ kills only GUI-owned Xray. A missing profile/binary/port fails closed; does not switch silently to direct. Source tests for GUI, network and voice PASS; packaged selftest, main-chat-ui, toolbar-dom, voice-dom and webview-network exit 0; installed BrowserChrome inline JS syntax PASS. Full local Setup and Update v0.2.62 installers verified SHA256; local backup/rollback path prepared. Deployed with .tools/deploy-v0262-vpn-safe.ps1 and status verified. Standalone isolated --test-main-chat-live returned 103, so genuine ChatGPT GUI E2E remains UNVERIFIED. CRITICAL LIMITATION: selected mode changes WebView2 only, not yet the official MCP tunnel-client transport; no running Xray hot-reload on scheduler's server changes. This remains follow-on work. Documented docs/VPN-MODE-SWITCH-V0262-2026-10-31.md. Local sources uncommitted, no GitHub release posted.

---

## Verified free node refreshing (31.10.2026 16:36 MSK)
Owner requested automated updates of free VLESS/TCP/REALITY servers, not single static URI. Implemented scripts/Vpn/free_reality_updater.py, free_reality_sources.json with 3 independent approved HTTPS feeds (AetrisVPN and 0xRadikal verified/fast), Windows Task Scheduler task ChatGPTMCP-FreeReality-Refresh (user DOMKAT\Maksim, every 3 hours indefinitely via daily repeating trigger, StartWhenAvailable, no admin, Python 3.13 pythonw.exe). Scheduled task's first immediate forced test exited result=0; next run 2026-10-31 19:28:28 MSK. Initial real refresh found 512 deduplicated valid candidates, probed 11, got 2 healthy; selected 147.45.245.177:443. Task-managed second refresh checked two, both healthy. Selected config encrypted using CurrentUser DPAPI under %LOCALAPPDATA%/ChatGPTMCPAgent/Vpn/profiles/Free-Reality-Auto.dpapi, catalog/status files omit raw URIs and UUID/password; Windows DPAPI roundtrip test successful. Selected profile was actually started with Start-ProtectedXrayProfile.ps1; isolated local HTTP proxy 127.0.0.1:11821 returned https://example.com HTTP 200 and stopped without orphan listener. Six dedicated offline tests passed (source filtering, base64, private address rejection, Xray config, failing source preserve last-good, selection priority); prior VPN isolation regression passed. Concurrent refresh lock, fail-closed test port and SHA-256 verified Xray binary. Status and disable scripts and detailed docs/VPN-FREE-SERVERS-AUTO-REFRESH-2026-10-31.md added. IMPORTANT: auto-refresh selects and stores a trusted-at-runtime public candidate but does NOT automatically redirect the installed authenticated WebView2 or live MCP connection to unknown free node, and does NOT yet auto-restart an actively running selected Xray on failure. Those are separate future production UI and failover stages requiring E2E testing and privacy choice. Happ, GUI v0.2.61 and original MCP transport unchanged. Source tree not committed or published; do not reset.

---

## Tested public VLESS/REALITY, generation 65 (31.10.2026 14:33 MSK)
The user asked for a free VLESS/REALITY link. Public AetrisVPN free list found at https://raw.githubusercontent.com/flaafix/AetrisVPN-white-list-lite/main/AetrisVPN.txt. Verified individual node on DOMKAT with Xray-core v26.10.29: VLESS/TCP/REALITY 147.45.245.177:443, observed exit 77.110.116.110; actual HTTPS example.com HTTP 200. Earlier public 0xRadikal verified candidates (16 tested) all failed from this PC; 7th Aetris candidate passed. Public URI is in .tools/vpn-pilot/free-public-reality-verified.txt (not in Git). Independent egress IP differs from the Happ local HTTP proxy exit IP. Existing Import-VlessReality.ps1 was updated to accept standard omitted encryption=none and type=tcp parameters; DPAPI import, protected Xray launch and stop passed. Isolated WebView2 test via the public node passed exit 0; neither login nor MCP client was switched. Free public exit operator unknown; do NOT route authorized ChatGPT, secrets or other private traffic without explicit informed decision. Public availability can expire. Real Happ and MCP remain operational, experimental Xray stopped and test profile deleted. Details docs/VPN-FREE-REALITY-VALIDATED-2026-10-31.md.

---

- source: legacy-v2-state
- authority: legacy-unverified
## Verified LamaVPN findings (31.10.2026 14:12 MSK)
Owner authorized reusing existing paid LamaVPN Germany in embedded Xray. Happ 4.6.1 active on DOMKAT, LamaVPN Germany selected, VLESS/TCP/REALITY; subscription visible until 2027-01-05. An old URL to sub.thelama.ru was found in Happ's subscription_log; current HTTP 200 returns only 'App not supported' dummy VLESS (Hide-Settings:1), including ordinary Happ/v2rayNG/sing-box and ClashMeta request formats; it has NOT been verified to be the current subscribed URL. The frequently updated tshbotpopup.ru belongs to unrelated Toshibu VPN, not used. Happ subs.db has one high-entropy stored subscription blob; no readable live Germany credentials extracted. Happ itself and any hidden providers were NOT bypassed. Implementation added: scripts/Vpn/Import-VlessReality.ps1 imports one authorized standard VLESS/TCP/REALITY share URL from local file, validates with Xray v26.10.29 and encrypts settings with Windows DPAPI CurrentUser under LocalAppData. Start-ProtectedXrayProfile.ps1 decrypts only locally, starts independent loopback proxy, removes plaintext temporary file; Stop-ProtectedXrayProfile.ps1 uses PID and EXE ownership checks. tests/test_vpn_protected_import.ps1 PASS (synthetic UUID/REALITY key, DPAPI ciphertext, loopback listener, safe stop and test file cleanup). Happ sing-box TUN config has process-name direct bypass for xray.exe, encouraging isolated testing but does not prove independent external REALITY routing. No real LamaVPN profile imported, no independent ChatGPT/MCP E2E accepted. The current missing dependency is a provider-approved ordinary VLESS/REALITY profile or standard subscription from LamaVPN management/support. Installed v0.2.61 GUI, existing Happ, tunnel-client and MCP runtime unchanged; experimental xray ports stopped. See docs/VPN-LAMAVPN-PROFILE-INTEGRATION-2026-10-31.md.

---

- source: legacy-v2-state
- authority: legacy-unverified
## VPN verified facts (31.10.2026, 13:30 MSK)
Xray-core official v26.10.29 downloaded to .tools/vpn-pilot; official Windows archive SHA256 verified. Pilot direct HTTP proxy 127.0.0.1:11809 returned HTTPS example.com 200 but ChatGPT timed out. Chained HTTP proxy 127.0.0.1:11810 -> Happ SOCKS5 10808 passed isolated WebView2 example.com test (exit 0); chatgpt.com curl HEAD returned Cloudflare 403, not a login test. False proxy port 11999 correctly failed WebView2. Temporary local VLESS client/server HTTP test passed; neither a remote REALITY tunnel nor independent ChatGPT was proven. Opt-in proxy selection added to new GUI source and future ServerStart.ps1 but NOT deployed. No VPN credentials imported, no Windows routing changes, no additional MCP tunnel. Pilot processes stopped; installed GUI remains v0.2.61; original MCP client and runtime PIDs unchanged. Next blocker: a trusted independent VLESS/REALITY server and end-to-end verification for both WebView2 and tunnel-client. See docs/VPN-XRAY-PILOT-2026-10-31.md.

---

- source: legacy-v2-state
- authority: legacy-unverified
## Подтверждённое наблюдение: 31.10.2026, 12:32 MSK
**Приёмка владельца (31.10.2026, 12:32 MSK):** голосовой ответ ChatGPT MCP Tunnel v0.2.61 действительно слышен через подключённые колонки; владелец подтвердил: «голос слышен на колонках всё ок». **TTS / физическое воспроизведение: ПРИНЯТО.** Микрофона ещё нет, поэтому тест реального микрофонного ввода (STT/VAD/PTT) отложен. Отдельные стресс-тесты автоматического озвучивания 2/2 и 10/10 пока не отмечать пройденными.

---

- source: legacy-v2-state
- authority: legacy-unverified
## VERIFIED 31.10.2026 — project-manager generation 61
DOMKAT installed ChatGPT MCP Tunnel GUI v0.2.61.0, local Sherpa/Piper voice worker v0.2.61, MCP runtime v0.2.58 remains PID 28372 and tunnel-client PID 24884; bridge ping OK. Voice synthesis through installed Realtek USB2.0 Audio completed with speech_finished interrupted=false. Missing mic verified: NAudio sees 0 input devices, USB2.0 MIC not present. The TTS worker now starts independently on voice_reply; missing-mic branch returns a localized error. Live normal ChatGPT WebView2 produced two actual text responses. The old [data-message-author-role=assistant] selector returned zero in live host; current responses are inside class DilResponseRoot. Both selectors are supported by hot-loaded ChatGPT-Voice.js; a live DOM observer identified the second reply, and a controlled bridge event started worker and generated audio. A physical user-confirmed hearing test and consecutive actual auto-audio 2/2 and 10/10 remain OPEN. Tested node JS syntax, GUI/voice builds, mock WebView2 DOM tests, TTS without mic and local audio playback. Verified full and update local installers v0.2.61 contain the final JS; SHA256 manifest available. Local source changes remain uncommitted; do not reset, force push or restart the active tunnel.

---

# Manager beliefs

- source: legacy-v2-state
- authority: legacy-unverified
## АКТУАЛЬНО 31.10.2026 — менеджер проекта, поколение 60
Источник: прямое поручение владельца 31.10.2026; проверка MCP bridge_ping, версии установленного GUI, PID, локальных моделей, журнала развёртывания, результат однократной автоматической задачи восстановления.

**Подтверждено:** DOMKAT, GUI ChatGPT MCP Tunnel **v0.2.60.0**, PID 27916; MCP runtime оставлен **v0.2.58.0** намеренно; tunnel-client.exe PID 24884, ChatGptMcpTunnel.exe PID 28372, bridge_ping=OK. Журнал .tools/deploy-v0260-status.json: phase=verified, 31.10 06:37 MSK, GUI SHA-256 62B17A3DDBD0380219172797DEC68074DC1F5BE2F5B37E8CD8FA5F31BA1EF84D. Установщики v0.2.60 Setup/Update собраны локально, проверены SHA. Backup v0.2.59 сохранён в .tools/deployment-backups/pre-v0260-safe-20261031 и pre-v0260-v0259. Встроенный профиль WebView2 не изменялся. Задача автоматического мониторинга после успеха отключена.

**Реализовано:** основной экран — настоящий ChatGPT внутри WebView2, вкладки/адресная строка/тёплая тёмная тема; никакого отдельного RichTextBox чата. MCP остаётся транспортом для Windows-инструментов. Голосовой worker ChatGptMcpVoice: локальный Sherpa-ONNX **1.13.8** (2.0 несовместим с данной Piper TTS), русская Zipformer ASR sherpa-onnx-zipformer-ru-2024-09-18, Silero VAD, русская Piper Irina TTS, NAudio. Микрофон: удержание кнопки и непрерывный режим; пауза 300–1500 мс, default 700; автоотправка по умолчанию OFF, автоозвучивание отдельно. Голос в штатный composer выбранной WebView2 вкладки; ответ для TTS через DOM текущей вкладки.

**Доказанные проверки:** русский автономный TTS WAV затем ASR восстановил текст; изолированный WebView2 mock DOM проверил вставку текста без автоматической отправки и один ответ без дублирования; сборка и packaged selftests PASS; установленные модели --check-models Ready/Stt/Vad/Tts=true. **Не проверено и не принято:** живой голосовой E2E в обычной беседе, физический микрофон USB2.0 MIC (статус Windows Unknown), воспроизведение динамиками с участием пользователя, 2/2 и 10/10 диалогов, barge-in/эхо/смена вкладок/реальный restart для голоса. Тесты на синтетическом аудио не заменяют живую приёмку.

**Исходники:** D:/Dev/ChatGPT-MCP-Tunnel, feature/webview2-primary-ui-v0248 HEAD 33d114b, все изменения WebView2+голоса v0.2.60 НЕ ЗАКОММИЧЕНЫ. Сохранять их без reset, затем прогнать регрессию и сохранить в GitHub. GitHub Actions не применять. Семь стратегических целей проекта остаются; равноправные узлы, другие LLM и автономность Happ — backlog.

**Опасная процедура:** прежний вызов ChatGptMcpAgent.exe --shutdown при принятом собственном tunnel-client оборвал MCP. После этого безопасная установка закрывала только GUI по PID, оставив те же PID клиента и runtime. Для следующих обновлений запрещено использовать старый --shutdown пока он может останавливать транспорт; обязательны backup, PID check и rollback.

**SUPERSEDED:** ниже исторические записи о текущей v0.2.37, WebView2 только для авторизации и MCP Apps как единственном пользовательском чате. Владелец выбрал настоящий ChatGPT WebView2, он единственный интерфейс переписки. Экспериментальные MCP Apps выводы 30 октября — архив, а не текущий план.

---


- source: legacy-v2-state
- authority: legacy-unverified
## Актуально 30.10.2026 — подтверждённый E2E и блокер повторного сообщения
2026-10-30, verified with real normal ChatGPT Host: A dev-mode plugin has separate frozen tool metadata. Its Settings -> Plugins -> ChatGPT MCP Tunnel -> Manage -> Обновить инструменты button refreshed tool list and UI resource; new chat then saw chat.thread.relay.probe and loaded thread-relay-probe-v0240, with automatic relay-poll and bound chat_id. Server notification tools/list_changed alone did not visibly refresh existing snapshot. One real MCP-only round-trip succeeded: message 0f62f2defcb04778a60437fc83157760 -> ui/message ACK -> chat.reply -> Status=answered, exact reply МАРШРУТ MCP РАБОТАЕТ. Second message 52382de5d39f4f6ea2178de0781b260f got ui-forwarded at 15:55:29 UTC but no chat.reply after minutes; it remains pending+forwarded, DO NOT RETRY. Relay heartbeat/poll continued. ChatGPT UIA showed technical routing metadata in visible user message despite audience assistant annotation. Never claim 10/10 or hidden metadata achieved.

- source: legacy-v2-state
- authority: legacy-unverified
## Приоритетное обновление — 30.10.2026, поколение 58
30.10.2026 next experiment evidence: An MCP tool returning resource URI and chat_id does not force ChatGPT Host to load that HTML. In existing ordinary ChatGPT thread v0.2.40/v0.2.41, chat.thread.open returned new thread-relay-probe-v0240 URI but Host continued initializing OLD cached lifecycle app; no probe resource-read, relay-begin or relay-poll. Sending notifications/tools/list_changed from McpServer was independently confirmed, but did NOT visibly update host tool metadata in this conversation. This proves stale host state in one conversation, not global impossibility. Also chat.ui.open marks status=ready prematurely before iframe activation, a false-ready bug.


- source: legacy-v2-state
- authority: legacy-unverified
## Verified 2026-10-30 live MCP host experiment
REAL MCP HOST EXPERIMENT, 2026-10-30: Two actual requests ui/request-display-mode {mode:pip} from thread MCP Apps in ordinary ChatGPT returned mode=fullscreen, not pip. Host declared inline/fullscreen only, hostInfo=chatgpt, MCP Apps 2026-01-26. Thread iframe initialized and heartbeats continued while visibility hidden, but this is not guaranteed permanent. Host did not advertise sampling or openai/message in this particular diagnostic context. Calling global chat.hub.open produced tool-open but no observed new global iframe. Experimental 0.2.38 was rolled back; installed DOMKAT is again 0.2.37. Preserve exact evidence in origin/experiment/mcp-host-pip-20261030 docs/MCP-HOST-PIP-EXPERIMENTS-2026-10-30.md.


- source: legacy-v2-state
- authority: legacy-unverified
## Архитектурная истина, согласованная владельцем 30.10.2026
MCP Tunnel — самостоятельная универсальная инфраструктура «человек ↔ ИИ ↔ компьютеры». Проект НЕ ограничивается управлением ПК или локальным мессенджером, а ChatGPT НЕ является единственным возможным LLM. Стратегия охватывает многокомпьютерные операции, обмен файлами, равноправные автономные узлы, текст/голос, интеграцию Goose/Ollama, безопасное выполнение команд, обновления и минимум внешних зависимостей. Текущая централизованная Router Lite и отсутствие полноценного чата/голоса/мультимодельности — промежуточное состояние; не утверждать, что стратегические цели уже реализованы. Владелец подтвердил весь этот набор без изменений.


- source: legacy-v2-state
- authority: legacy-unverified
## СТАТУС С НАИВЫСШИМ ПРИОРИТЕТОМ — 30.10.2026, 16:43 МСК
- Роль текущего менеджера — project-manager / Менеджер проекта; не supervisor.
- DOMKAT: установленный агент и MCP Runtime v0.2.37+45d3289, исходники main, Git HEAD до обновления капсулы dec5430, опубликован GitHub pre-release v0.2.37. Компьютеры DOMKAT и MKATUSHONOK доступны по Router Lite.
- WebView2 в WinForms используется исключительно для авторизации и постоянного профиля; вход и сохранение авторизации на реальном аккаунте еще не приняты. Он НЕ отправляет и НЕ читает сообщения. Браузерный Node/CDP транспорт удалён из установленной версии.
- JSON-очереди, списки локальных чатов, MCP Apps ui/message target:active/new, ACK и chat.reply существуют. Автозапуск опроса внутри MCP Apps после host tool-result проверен синтетическими тестами; настоящая автономная двусторонняя связь с ChatGPT Host не доказана. Мессенджер НЕ ПРИНЯТ.
- MCP Apps iframe создаёт и уничтожает ChatGPT Host. Сервер не имеет проверенного механизма самостоятельного восстановления iframe после закрытия. Assistant-only routing annotations для chat.reply — в исходниках; фактическое поведение реального Host не принято.
- Два исторических сообщения имеют status=pending и отметку ui-forwarded. Это неопределённая доставка: не удалять, не отправлять вслепую повторно и не утверждать, что они никогда не попадали к модели.
- Приоритет фактов этого блока выше исторических описаний v0.2.18–v0.2.36 ниже. Исследовать возможности спецификации MCP экспериментально, но не выдавать тестовый хост за реальный ChatGPT.


- source: legacy-v2-state
- authority: legacy-unverified
## Verified 30 Oct 2026: v0.2.35 chat routing release
Source commit 729838e was pushed to main; GitHub stable release v0.2.35 has two installer assets and SHA256 manifest. Packaged integration tests passed. Old-session discovery had incorrectly created new ChatGPT conversations; a new cloud dialog is now only dispatched from an explicitly newly created local chat. The visible message is shorter but still has a compact reply marker. Actual ChatGPT ordinary-mode E2E has NOT been verified. Installed DOMKAT remains 0.2.33.


- source: legacy-v2-state
- authority: legacy-unverified
## 2026-10-30 05:25 MSK: stable v0.2.18 relay restoration
Owner directed return to working v0.2.18 chat. Git comparison proves current ChatRelayApp.html is unchanged from accepted eb1845f. Subsequent Bootstrap/HostActivator routing created non-delivery and false-ready states. Local candidate v0.2.30 disables competing automatic dispatch and tray activation, preserving unrelated MCP tools. Two dotnet builds, ten isolated tests and packaged release tests passed, but live acceptance remains open.


- source: legacy-v2-state
- authority: legacy-unverified
## 2026-10-30 04:14 MSK — ordinary ChatGPT Work-regression investigation
- Verified historical code: v0.2.18 ChatRelayApp sent MCP Apps `ui/message` with `_meta["openai/message"].target="new"` from an iframe embedded in a normal ChatGPT conversation. A Global MCP entrypoint may have different host execution context; merely instructing the model "do not use Work" does not control that context.
- Real diagnostic `chat.thread.open` from this ChatGPT conversation at approximately 04:05 MSK: `hasOpenAiMessage=true`, `hasDeepLink=false`, `displayMode=inline`. Earlier bootstrap observation 29.10 had `hasOpenAiMessage=false`. These are time- and host-context-dependent; do not assume capability from old recordings.
- Owner directive: new dialogs created by tunnel must be **ordinary ChatGPT**, not Work or Codex, with no separate API. Refuse unsafe Global dispatch. `ui/message` acknowledgement alone does NOT prove the resulting thread is not Work.
- Verified local fix and candidate: v0.2.29, commit `e80e3ea13f6eeaaa7611d9a065201f4edaba28ca`, prepared and published as GitHub pre-release from DOMKAT, without Actions. Global HTML now has no `ui/message` and no queue polling; ordinary `chat.bootstrap.open` resource v0229 verifies extension and route, prompts a one-time user confirmation when host omits deep link, revokes on context change, preserves unforwarded queue otherwise.
- As of 30.10.2026 04:13 MSK installed DOMKAT remains `v0.2.28`, not v0.2.29. Four historical messages were pending and preserved. Real E2E ordinary-mode acceptance **NOT completed**. Authority: owner-directive and verified source/tests/host lifecycle/installed version. Details: `docs/ORDINARY-CHAT-ROUTING-2026-10-30.md`.

- source: legacy-v2-state
## Актуализация 30.10.2026, 03:46 MSK — подтверждённая v0.2.28 и текущая роль
- **Verified runtime:** DOMKAT: установленный `D:\ChatGPTMCPAgent\ChatGptMcpAgent.exe` имеет версию `0.2.28+ad28e28218a404a645f0df805f8d4f205fd2e079`. Рабочее дерево `D:\Dev\ChatGPT-MCP-Tunnel`, ветка `main`, HEAD `ad28e28218a404a645f0df805f8d4f205fd2e079`, `git status` пуст. Проверено непосредственно на DOMKAT 30.10.2026. Установку `v0.2.28` пользователь выполнил самостоятельно; ассистент её не запускал.
- **Verified release:** v0.2.28 локально собрана на DOMKAT, прошла .NET/WinForms/MCP/Router Lite/Node тесты и опубликована как готовые full/update EXE + SHA256 в GitHub Release. GitHub Actions для неё не использовались.
- **Owner + role correction:** настоящий диалог привязан к постоянному менеджеру `project-manager` (Project Manager v2.1.0), а не `supervisor`. Моя фраза «своя капсула супервизора» ошибочно интерпретировала обращение владельца; остановка тогда выполнена, капсула супервизора не изменялась. При поручении «актуализируй капсулу менеджера» обновляется только та же Context Capsule менеджера.
- **Verified implementation/limitation:** `executor` передаётся на каждый Windows MCP-вызов; `project-manager` → «Менеджер проекта», `supervisor` → «Супервизор», без метаданных → «ChatGPT». Это **заявленная**, не удостоверенная MCP Host идентичность; реальная визуальная приёмка подписей **после** установки 0.2.28 не проведена, хотя WinForms и Router Lite тесты прошли.
- Правила локальной пакетной разработки, STOP, GUI ~1 сек, CLI без пауз, GitHub как хранилище/Actions-off сохраняются. Новая сверка замещает устаревшие «v0.2.28 не опубликована/не установлена» ниже. Provenance: прямые сообщения владельца и проверенные Git + установленная версия + протокол локальной сборки, 30.10.2026.

- source: legacy-v2-state
- authority: legacy-unverified
## 30.10.2026 — атрибуция исполнителя MCP-вызовов
- Owner: в уведомлении показывать «Исполнитель: Менеджер проекта / Супервизор / ChatGPT», в зависимости от персонализированности вызывающего чата.
- Verified implementation v0.2.28 in local source: optional per-call \`executor\` for 24 Windows MCP tools and file.transfer.start; ControlState stores sanitized identity, overlay displays last active/recent identity, Router Lite/desktop.batch/transfer wire forward it. Unknown or absent => ChatGPT. Local build/Windows/router selftests pass, not yet deployed.
- Limitation: MCP Host does not offer verified chat persona identity; the model must supply \`executor\` per call. This is non-authoritative informational attribution, never identity-based access control.
- Source: owner-directive 30.10.2026 and verified local tests. Specification docs/EXECUTOR-ATTRIBUTION-2026-10-30.md.

- authority: legacy-unverified
## 30.10.2026 — актуальная сверка, имеющая приоритет над версиями ниже
- Прямое решение Owner: локальная разработка, компиляция, тесты и Inno на DOMKAT `D:\Dev\ChatGPT-MCP-Tunnel`; GitHub **только** хранение исходников, истории и готовых Releases; GitHub Actions запрещены/отключены (3 disabled_manually). Прежние указания запускать CI заменены.
- Факт: `v0.2.27` собрана локально и установлена на DOMKAT в `D:\ChatGPTMCPAgent`, после обновления `bridge_ping` работал; исходники текущего Git `main` до передачи `2f7fe42`.
- Факт: в рабочем дереве **четыре незакоммиченных файла** поправки Unicode `v0.2.28`, первоначальная C# сборка с предупреждением CS0162; не удалять и не считать релизом.
- Владелец требует **пакетное выполнение**, сокращение числа model/tool-циклов; GUI ~1 сек между изменениями, CLI без задержки. STOP локальный и неотложный; индикация ожидания модели и реального завершения пока не реализована.
- История дефектов: v0.2.25 индикатор не создавался в реальном TrayContext, исправлен v0.2.26; v0.2.27 улучшила имена операций, но допустила пять буквально вопросительных строк в русском UI.
- Источник: прямой чат владельца 29–30.10 + проверенные Git, Windows, releases. Полный контекст: `docs/PROJECT-MANAGER-HANDOFF-2026-10-30.md`.

- source: legacy-v2-state
- authority: legacy-unverified
## 2026-10-30: v0.2.24 return to ordinary ChatGPT instead of Work
- User identified critical regression: newest cloud threads created from Global MCP Apps appear in Work (observed GPT-6 Astra, Work execution time/results), while v0.2.18 model-invoked chat.ui.open generated ordinary ChatGPT threads from existing /c conversation with normal Chat model. Real browser comparison confirmed even Work threads can have /c URL and a top-level 'ChatGPT' mode selector, which are insufficient proof of billing/model mode.
- Root architecture: Global entrypoint /mcp-app/chat.hub.open creates new threads via ui/message target:new in the Global host context. OpenAI extensions do not specify a parameter to force ordinary ChatGPT mode; Web support refers to Work browser. New design v0.2.24: Global becomes observer with no ui/message, model-only chat.bootstrap.open is invoked once in an existing ordinary /c thread and auto-dispatches target:new from that normal host context; OS activator launches anchored /c URL, not /mcp-app. New source and isolated tests PASS. Real cloud host acceptance NOT YET performed. Documentation docs/ordinary-chat-bootstrap-v0.2.24.md.
- Windows helper stores private anchor URL in Chat/hub-entrypoint.url and accepts /c/<uuid> only, filters heartbeat to chatbridge-* instances. Old Work-oriented Global iframe cannot suppress ordinary chat launch. Existing messages/files unchanged, user said old chats not valuable.

- source: legacy-v2-state
- authority: legacy-unverified
## 2026-10-30: v0.2.23 automatic Windows Global Hub activation
- DOMKAT live: all browser.exe stopped, MCP readyz=200, Windows deep link cold-started Global Hub and created fresh iframe; new local message 30OCT-9D41 was ui/message target:new forwarded and ultimately replied ХОЛОДНЫЙ-УСПЕХ to matching chat_id using chat.reply. Follow-up forwarded but stalled pending; v0.2.22 introduces local-context snapshot for new cloud threads; full continuity acceptance remains.
- v0.2.23 Windows ChatHostActivator detects pending unforwarded messages and absence of fresh Global Hub heartbeat and automatically launches per-user ChatGPT Global URL via OS, with URL allowlist and 120-second cooldown. Integrated into existing tray instance only for server role. No hard-coded account plugin ID, no second tunnel or hourly ChatGPT planner. Source and tests docs/mcp-hub-host-activator-v0.2.23.md.
- User explicitly allows closing all Yandex Browser windows and says old tunnel chats have no value. Avoid special preservation mechanisms; do not delete unrelated user files. Installed production remains v0.2.21 until verified release/upgrade.

- source: legacy-v2-state
- authority: legacy-unverified
## 2026-10-29: v0.2.22 local chat context recovery after Global cold start
- Owner allowed closing all Yandex Browser windows/processes and does not value old tunnel test chats. v0.2.21 is INSTALLED and cold-started from Windows URI with browser.exe=0 before activation. Hide via ShowWindow SW_HIDE; user only used local WinForms chat. Real new local message eb8754a07f7841e9be2abd51a36bf55b / chat_id 810e129d5501471c9927630b033cf301 forwarded by Global Hub, ChatGPT answered АВТОЗАПУСК-21 via chat.reply, Status=answered. This PROVES full first-turn model loop with hidden browser host, NOT a no-browser-process inference mechanism.
- Second local follow-up bdbfca780e364437bd8624eda5d1fa56 was forwarded by global again after old cloud thread route went stale. Separate new ChatGPT conversation displayed response «Точный текст предыдущего ответа в этом диалоге мне недоступен», did NOT call chat.reply; local status remains pending with ACK. Thus context continuity / assured tool reply remained unresolved.
- v0.2.22 SOURCE: ChatUiTools.Pending(discover=true) now returns bounded last 5 answered local turns as context; Global Hub versioned hub-relay-v0222 prepends this transcript to target:new first prompt, strengthens requirement to use chat.reply before textual answer. C# + Python routing and JS hub tests PASS. Pending GitHub CI and practical re-test. Notes docs/mcp-global-hub-context-recovery-v0.2.22.md.

- authority: legacy-unverified
## 2026-10-29: v0.2.21 Global Hub cold-start and auto-relay experiment
- User authorized full Yandex Browser closure, and explicitly says old local tunnel chats have no value: do not spend effort on special history backups/migrations.
- On DOMKAT v0.2.20 actual browser cold launch PASSED: first closed visible Yandex Browser normally and all remaining browser.exe background processes, confirmed browser.exe=0 while tunnel readyz=200. From Windows Start-Process on observed global ChatGPT MCP Apps URI booted browser, authenticated ChatGPT and Global iframe; fresh instance ui-7uzwykoh05-mvu0g62v and tool-open/ui-initialize/ui-initialized in journal at 20:43:21-23 UTC. No manual browser actions.
- v0.2.21 source adds separate Global Hub resource ui://chatgpt-mcp-tunnel/hub-relay-v0221 served via ChatGlobalHubApp.html. It auto-polls chat.ui.pending(discover=true) and forwards new local chat messages through ui/message target:new, then chat.ui.ack after host acceptance; model is instructed to invoke existing chat.ui.open(chat_id) and chat.reply. Error retry 7s. Original relay untouched. Isolated Python/JS tests PASS. *Real model reply after full cold start not yet tested.*

- source: legacy-v2-state
- authority: legacy-unverified
## 2026-10-29: v0.2.20 Global/Thread host compatibility correction
- v0.2.19 published and installed DOMKAT by SHA-verified updater, readyz=200, both computers connected, 146 messages retained, Windows watchdog Ready, no user history changed.
- Real ordinary ChatGPT Yandex browser after refresh tools showed banner: "Для этих приватных инструментов невозможно отобразить виджеты: chat.hub.open, chat.thread.open" because both entrypoint tools had _meta.ui.visibility=["app"]. Host fetched both resources (resource-read entries) but no tool-open/ui-initialize. Changed to visibility=["model","app"], as already working in v0.2.18. Removed static global/thread entrypoint metadata from existing chat.ui.open (retained model tool and relay), leaving exclusive chat.hub.open global and chat.thread.open thread. Both new resource URIs versioned v0220. Locally built/tested, v0.2.20 source pending CI.
- Exact observed browser global route v0.2.18: https://chatgpt.com/mcp-app/asdk_app_6ae131ee666c8191b89ee9bbfd947f52/chat.ui.open. Reusing observed host route form for experimentation; real link to new hub not assumed accepted until tested.

- source: legacy-v2-state
- authority: legacy-unverified
## 2026-10-29: v0.2.19 Global/Thread lifecycle diagnostic implementation
- Owner authorized execution of MCP Apps global/thread and browser lifecycle experiment, preserve working v0.2.18 until tests pass. v0.2.19 working tree introduces distinct global-only chat.hub.open and thread-only chat.thread.open, ui:// lifecycle-global-v0219 and lifecycle-thread-v0219 with separate HTML. app-only chat.lifecycle.event/status write filtered technical events under Chat/lifecycle-probe/events.jsonl, no text/user data/url. UI auto runs ui/initialize, logs handshake, host context changes, heartbeat, teardown, but never ui/message or chat.ui.pending. Existing production relay untouched.
- Local .NET build clean; isolated Python entrypoint contract and JS iframe host emulator PASS; existing chat and Router Lite regression tests PASS. Actual ordinary ChatGPT Global/Thread entrypoints and automatic host activation not yet tested pending verified installer. Research and test plan: docs/mcp-apps-lifecycle-experiment-v0.2.19.md.

- source: legacy-v2-state
- authority: legacy-unverified
## 29.10.2026: normative MCP 2026-07-28, MCP Apps and OpenAI extensions re-analysis
- Investigation re-centered on primary technical specifications (not third-party failures). MCP 2026-07-28 stateless server/client differs from MCP Apps iframe 2026-01-26: former has server/discover and per-request _meta, latter still requires ui/initialize/initialized. OpenAI extension adds global/thread entrypoints, deep-link hostContext and ui/message target:new/active. Full technical report: docs/mcp-2026-core-apps-openai-protocol-review-2026-10-29.md.
- Direct, isolated on DOMKAT current server v0.2.18 ModelContextProtocol NuGet 2.2.0: server/discover returned supportedVersions=[2026-07-28], capabilities tools.listChanged=true, resources.listChanged=true, logging; tools/list returned 52 tools using modern protocol. subscriptions/listen for toolsListChanged/resourcesListChanged returned notifications/subscriptions/acknowledged with both filters and subscriptionId=7. Modern resources/read for ui://chatgpt-mcp-tunnel/relay-v0218 returned resultType=complete, mime=text/html;profile=mcp-app and 13803 HTML bytes. Tool chat.ui.open lists global+thread entrypoints, model+app visibility, chat_id schema.
- Design implication: preserve single Tunnel, separate Windows persistent logical chat from MCP network session and MCP Apps View instance; use explicit (computer_id,chat_id,message_id), per-iframe instance leases, context capsule and ui/update-model-context. Modern subscriptions announce actual list/resource changes; to push chat-specific events introduce subscribed resources + resources.subscribe rather than misusing tools/list_changed. The UI View's supported standard MCP subset has tools/call/resources/read, so retain polling fallback for app.
- The model request itself is initiated by ui/message on a live ChatGPT Host; stateful Tasks and MRTR are for server operations/continuation, not a replacement for invoking the Host. Focus next experiments on global/thread entrypoint lifecycle, deep links, full context bootstrap and automatic Host activation after Windows reboot, without Work/Codex/API.

- source: legacy-v2-state
- authority: legacy-unverified
## 2026-10-29: Deep MCP Apps autonomy research, external GitHub implementations
- Deep verified investigation: global/thread entrypoints launch UI inside a living MCP Host (both accept {}); they are not independent model inference endpoints. Deep link passes app relative route via openai/deepLink to global app, but URL does not guarantee existing instance reuse or host startup. In OpenAI extensions platform table Web explicitly refers to Work browser, not classic ChatGPT; ordinary ChatGPT must be validated empirically.
- Research projects: open-mcp-apps SQLite + append-only event ledger + idempotent commands for persistent data; goose-chatgpt-web separate session vs BrowserHost/daemon; ChatCmd browser-extension local chat; PC-Bridge Windows app with embedded ChatGPT web view. None proves inference with zero ChatGPT host process. Source docs/mcp-apps-autonomy-deep-research-2026-10-29.md.
- GitHub issue findings: openai/mcp-extensions#29 manual thread bootstrap failure marked fixed 2026-10-21; #54 independent app instances on model invocations (staff reply WebMCP, not planned); #37 global deep link may launch a new tab not existing instance; ext-apps#417 localStorage View persistence not part of portable protocol; ext-apps#754 asks for pinned view. Need instance identity, lease/fencing and DB source of truth.

- source: legacy-v2-state
- authority: legacy-unverified
## 2026-10-29: Study of MCP Apps global and thread entrypoints
- Source-verified: global fullscreen navigation and thread-specific content tab are static MCP App entrypoints (_meta[openai/ui].entrypoints); both call tool with {}. A deep link can pass relative route via hostContext[openai/deepLink].url and host-context-changed notifications. ui/message target:new is live-validated in v0.2.18; manual global/thread entrypoints and deep links in ordinary Yandex ChatGPT are to be verified. OpenAI advises separate tools per entrypoint and versioned UI resource caching. Research: docs/mcp-apps-entrypoints-research-2026-10-29.md.

- source: legacy-v2-state
- authority: legacy-unverified
## 29.10.2026: two distinct live ChatGPT threads and switch-back PASS
- New second local session chat_id 58f8d4f5ab3c4fea88382a4ab029967f received its own new ChatGPT thread via ui/message target:new, route waiting -> ready, and local message 477156c8b341421ebb5866f0bc4188d5 returned Status=answered, Reply=ВТОРОЙ.
- Switched GUI back to first local session chat_id c8d881eb8f0f4ff1aac24dcfe21a0a0c. Message 755051abb067448a9ff138bd2d082371 was forwarded and answered Reply=ПЕРВЫЙ in that same first session. Previous messages ПРИНЯТО and ПРОДОЛЖЕНИЕ remain visible only there. Thus 2 independent new ChatGPT threads, 2 associated local chat histories and switch-back have live end-to-end PASS.
- Browser open, ordinary ChatGPT MCP Apps, no Work/Codex/API or hourly cloud planner. Browser-closed recovery and MKATUSHONOK own local GUI not tested.

- source: legacy-v2-state
- authority: legacy-unverified
## 29.10.2026: v0.2.18 real ordinary ChatGPT multi-chat acceptance PASSED
- DOMKAT verified on v0.2.18.0 after controlled update; GitHub Actions Windows release 40926899682 SUCCESS; readyz=200, ChatGPT MCP Tunnel bridge_ping OK, computers.list DOMKAT and MKATUSHONOK connected, Windows Recovery watchdog Ready, all prior 142 message files retained.
- Authenticated Yandex Browser: ChatGPT MCP Tunnel > Manage > Refresh tools performed. In a new ordinary ChatGPT conversation the model invoked chat.ui.open. Model output and rendered app confirmed new resource URI ui://chatgpt-mcp-tunnel/relay-v0218 (cache fixed).
- Clicked Begin Forwarding in real MCP Apps, then started installed 0.2.18 GUI in preview-chat-window mode against real local Chat data and clicked New Chat. New local chat_id c8d881eb8f0f4ff1aac24dcfe21a0a0c persisted. Submitted test message marker 42F6, message_id 28d970531bc3438991bfb08816c94358. Sent via ui/message to new ChatGPT thread, iframe route state ready, ui-forwarded durable ACK created. chat.reply wrote Status=answered, Reply=ПРИНЯТО into correct local message.
- Second follow-up in same local chat marker C2A7, message_id 29d0c5d2b59b42bda27b3caf913ccbed: Reply=ПРОДОЛЖЕНИЕ, Status=answered, ui-forwarded present, same chat_id as first; verifies continuous relay after new-thread handoff.
- Via UIA selected 'История до обновления' and observed historical diagnostic messages; selected new chat and read only the two new tests (ПРИНЯТО, ПРОДОЛЖЕНИЕ), no legacy content. Classic local new chat/list/switch works in real GUI.
- Old per-hour ChatGPT automation stayed disabled, no Work/Codex/API used for transport. Active MCP Apps iframe still required; browser-closed recovery, remote MKATUSHONOK own local chats, multi-iframe robust lease remain future tasks.

- source: legacy-v2-state
- authority: legacy-unverified
## 29.10.2026: v0.2.18 MCP Apps cache-bust after live 0.2.17 test
- v0.2.17 Windows CI/release SUCCESS (40924254070), SHA256 Update c971f2f0652ec8e766181bfad7394d31f5a72c053a4b08e09ba0b845a34148e9, installed on DOMKAT with matched SHA, Windows watchdog Ready, readyz=200, both computers connected, 142 existing message files preserved.
- Live ordinary ChatGPT after plugin tool refresh showed mode mcp_apps_multi_chat for new chat.ui.open, but host rendered STALE 0.2.16 MCP Apps HTML from same ui://chatgpt-mcp-tunnel/relay URI. The installed 0.2.17 ChatRelayApp.html had new target:needsNew and no old 'Ожидание сообщений. Неотправленных: 0.' text; browser iframe displayed precisely old wording. This is real observed HTML resource caching.
- Implemented v0.2.18 cache-bust by versioned UI resource uri ui://chatgpt-mcp-tunnel/relay-v0218, updated registration and tests. Both .NET builds clean, original/extended MCP tests pass. Real new-cloud-chat target:new acceptance still pending refreshed 0.2.18 host.

- source: legacy-v2-state
- authority: legacy-unverified
## 29.10.2026: v0.2.17 multi-chat source implemented, pending Windows CI and real ChatGPT
- Owner approved v0.2.17 MCP autonomous chat manager and explicitly requested classic chat UI: '+ Новый чат', per-computer chat list, switch and separate histories. Implemented ChatSessionStore with 32hex chat IDs and on-disk sessions/active-chat records. New ChatWindow includes left sidebar, owner-drawn chat titles/previews, chat selection and isolated in-memory drafts/attachments; old message files without ChatId stay untouched and appear in 'История до обновления'. Replies remain tied to original message IDs.
- MCP route updated: chat.receive now includes chat_id; chat.ui.open(chat_id), pending(chat_id,discover) and ack(new_chat) support new ChatGPT thread handoff via ui/message target:new and new iframe auto-start via ui/notifications/tool-input. Existing unscoped widget calls only see legacy chat so cannot silently consume messages from unrelated local chats. Route heartbeats and bootstrap waiting markers retained under Chat/ui-routes.
- Local .NET Agent and MCP Server build with 0 warnings/errors. Isolated --test-chat-sessions PASS, Python routing PASS, Node target:new PASS, previous bridge tests PASS. Also manually opened preview process on DOMKAT isolated temp root, created two chats, sent messages, switched back through UIA and verified displayed selected history. Preview process and temp data cleaned. Production DOMKAT remains v0.2.16 untouched.
- Real ChatGPT acceptance of ui/message target:new and new chat iframe auto-start not yet done; do not claim completed without user-facing browser test. Remaining reliability: cross-machine shared chats, browser-closed bootstrap, longer-lifetime recovery.

- source: legacy-v2-state
- authority: legacy-unverified
## 29.10.2026: REAL ordinary ChatGPT ui/message acceptance 19:36-19:42 MSK
- Actual ordinary ChatGPT fresh browser session exposed new 52-tool catalog, invoked chat.ui.open and rendered real MCP Apps iframe with hostCapabilities.message.text and experimental.openai/message.
- Manual click probe sent user message nonce 58ewppk5 and ChatGPT answered with matching nonce, proving real ui/message new model turn.
- Once Start relay was clicked, synthetic DOMKAT queue nonce 43830fd3 auto-forwarded and ChatGPT replied with chat.reply; queue message Status=answered and durable ACK=true.
- Real WinForms ChatWindow opened by double-click on DOMKAT tray icon. Message GUI8C42 was submitted via actual text field and Send, automatically forwarded by iframe and answered via chat.reply; actual GUI history showed the returned reply, JSON Status=answered.
- Browser ChatGPT tab was switched to a different tab, real WinForms GUI message BGTAB42 still automatically forwarded and answered via chat.reply with ACK=true, then original browser tab was restored.
- Tests performed ~19:36-19:42 MSK, not at scheduled minute 44. Polling works only while the host iframe is alive, NOT proven with browser closed, host suspended or infinite duration.
- GUI footer still incorrectly reports awaiting hourly schedule despite immediate new replies; needs fix. Installed DOMKAT 0.2.16, MKATUSHONOK local GUI untested.

- source: legacy-v2-state
- authority: legacy-unverified
## 29.10.2026: DOMKAT production update 0.2.16 and ChatGPT browser tool refresh
- Owner explicitly authorized installing v0.2.16 on DOMKAT and refreshing connected action catalog via browser.
- Installed on DOMKAT with verified release SHA-256 FC05177A63EEC63E28BEE080247ED7B7835A3758098D77D072EFE9253696DBA0. Initially Inno Setup silently returned code 5 because RestartManager could not close running agent/runtime/tunnel-client (confirmed installation log); no files updated during those attempts. Successful controlled install paused only existing watchdog, invoked agent --shutdown (exit 0), waited for its own three processes to exit, ran official updater and restored existing watchdog.
- Verified resulting file version 0.2.16.0, independent local readyz HTTP 200, bridge_ping OK, DOMKAT and MKATUSHONOK connected, watchdog Ready, ChatRelayApp.html installed. All other user apps and unrelated untracked TXT files untouched.
- Via user's open Yandex browser (authenticated ChatGPT), navigated Plugins > ChatGPT MCP Tunnel > Manage and invoked 'Обновить инструменты'; button disabled during request then re-enabled without visible error. This confirms browser refresh action was invoked, NOT definitive proof this already-running chat switched from cached 45 to new 52. The actual 52-command runtime is source/CI-verified; live new-conversation availability pending.

- source: legacy-v2-state
- authority: legacy-unverified
## 29.10.2026: v0.2.16 stable Windows release verified (not live ChatGPT accepted)
- Verified 29.10.2026: GitHub Actions 40907216014 SUCCESS; published stable v0.2.16 with Setup SHA256 efda9954ec9a519ebfca5f0364276e4c0bb58386b50bff63b1e690d11dca23ab and Update SHA256 fc05177a63eec63e28bee080247ed7b7835a3758098d77d072efe9253696dba0. Real ChatGPT host iframe remains untested, installed DOMKAT 0.2.13 as last observed.

- source: legacy-v2-state
- authority: legacy-unverified
## B26 — 29.10.2026: Independent ui/message diagnostic probe added
- Source prototype v0.2.15 CI/release PASS, but installed DOMKAT still v0.2.13 and ChatGPT tool list 45. Added a separate explicit-click ui/message probe to differentiate host-level send support from timer-driven forwarding, plus host capability reporting. Local mocked host tests and 52-tool MCP protocol tests pass; real ChatGPT acceptance pending.

- source: legacy-v2-state
- authority: legacy-unverified
## B26 — v0.2.15 Windows release verified 29.10.2026
- GitHub Actions run 40904368593 SUCCESS; published stable v0.2.15 from commit 59fea9657732ae500a1834c40437471459f7b0cf. Full Windows Setup/Update/selftest/installer preservation acceptance complete. Update SHA-256 2d4b44f88fcefb4560c750d09148b2795fcdd2dba12b7d44afc83babd60f94bd; Setup SHA-256 be1ce88211544aec8447034e5ba92a1b0bd573c8b9a73c762e71c7485ee61f03. Live ChatGPT host ui/message acceptance remains unverified; installed DOMKAT is still v0.2.13.

- source: legacy-v2-state
- authority: legacy-unverified
## B25 — MCP Apps ui/message prototype source-tested 29.10.2026
- Owner explicitly instructed implementing ui/message without API, Codex, Work, browser automation or scheduler.
- Official MCP Apps specification establishes ui/message as a request FROM A CURRENTLY RUNNING MCP APP VIEW to its host; openai/message metadata supports target=active, send=true. It is NOT a server-originated trigger for closed ChatGPT.
- Implemented chat.ui.open tool with _meta.ui.resourceUri and openai/ui global/thread entrypoints, resources/read HTML, app-only chat.ui.pending/ack, user-enabled polling, and automatic ui/message; the original queue and attachments remain untouched, ack saved only after host success.
- Verified local MCP tools/list 52 tools, resources/read HTML, isolated queue and idempotent ack; JavaScript MCP host simulation accepted ui/message and returned ack, and denied ui/message without acknowledgment. Two .NET builds zero errors/warnings; prior stdio MCP and Router Lite 23 remote tools/reconnect tests pass.
- Critical: genuine ChatGPT host acceptance and widget lifetime are UNVERIFIED. This is an experimental build, not a proof of anytime access. Existing deployed app not updated.

- source: legacy-v2-state
- authority: legacy-unverified
## B24 — stable v0.2.14 Windows release accepted 29.10.2026
- GitHub Actions run 40866882778 completed SUCCESS on commit 21a1a0c5218a9425d00ba01c866deb95ce94f54d; tests passed: original stdio 49 tools, Router Lite 23 remote tools and reconnect, new attachment text/image/integrity tests, self-contained UI tests, Inno Setup full/update and clean install/update-preservation/uninstall.
- Stable GitHub Release v0.2.14 published with 3 verified assets: Setup SHA256 7ff5afc44db3a535b71e11b6f174141684f666dd0396c303fa03ee64d2d8174d, Update and legacy alias SHA256 ef4899062bde3caecb85fc29d23c03a155ea78d66902d6d49da761bf4f4504ef.
- Both physical hosts were NOT updated by manager; observed DOMKAT v0.2.13 still responds to bridge_ping. Owner-side physical clipboard/image UI test and ChatGPT plugin catalog refresh outstanding. This supersedes prior v0.2.14 Windows CI pending belief but not physical acceptance.

- source: legacy-v2-state
- authority: legacy-unverified
## B23 — Chat attachments tested in source 29.10.2026
- Owner explicitly authorized file attachments and clipboard images in existing MCP Tunnel chat window.
- Source: multi-file selection, Explorer drop, Ctrl+V image, preview/remove, attachment-only messages; copied payloads under per-message IDs with SHA-256 and JSON metadata. New chat.attachment.read returns real MCP image block, bounded UTF-8 text, or locally verified binary path. chat.receive includes metadata.
- Verified: local Windows .NET builds zero warnings/errors, ChatWindow attachment selftest PASS, isolated MCP attachment E2E PASS (PNG, UTF-8, invalid identifier and SHA-256 mismatch), baseline MCP 49 tools, Router Lite all 23 remote Windows functions and reconnect PASS.
- This is SOURCE-tested, not installed on user computers. Installed v0.2.13 remains unchanged, CI/release v0.2.14 and physical clipboard/UI acceptance pending. ChatGPT tool catalog currently 45, refresh required to expose new command.
- Evidence: direct owner instruction and verified local test results 29.10.2026.

- authority: legacy-unverified
## Verified two-host WSS file transfer, 29.10.2026
- 324820992-byte signed MAX.msi copied from MKATUSHONOK to DOMKAT over the authenticated WSS Router Lite, SHA-256 verified by independent receiver hash, and resumed after an observed reconnect at 80.7%.
- Same-user transfer CLI v0.2.10 allows remote transfer start/status without relaying binary data through model responses. ChatGPT connector currently still enumerates 41 original tools, not the 3 new methods.


- source: legacy-v2-state
- authority: legacy-unverified
## Router Lite file transfers — 29.10.2026
- Large binary files are streamed within existing authenticated WSS, never through model MCP text responses.
- Transfer tool family: file.transfer.start/status/cancel. Server orchestrates both directions using internal bounded RPC.
- Destination partial and metadata track size/hash; final save requires matching SHA-256. Safe default is no overwrite.
- Physical WAN two-device acceptance remains pending after isolated E2E 90 MiB PASS.


- source: legacy-v2-state
- authority: legacy-unverified
## B22 — Direct WSS via public WAN is selected and v0.2.8 released (29.10.2026)
- Решение владельца 29.10.2026: DOMKAT имеет статический публичный IPv4 на WAN-интерфейсе маршрутизатора; удалённый клиент расположен в другой локальной сети. Развивается прямой защищённый WSS без обязательного Tailscale/посредника. Локальный адрес DOMKAT не заменяется внешним IP. Источник: прямая команда владельца; код основной ветки f8cc66848a475030b63ad5c15e25ec6c68e76308.
- Сервер Kestrel обслуживает защищённый порт TCP 55202 в дополнение к прежнему локальному TCP 55201. Сервер создаёт собственный TLS-сертификат, шифрует пароль PFX через DPAPI Windows, клиент проверяет точный SHA-256 отпечаток до передачи общего ключа. Новый режим требует минимум 32 символа общего ключа. Проверены сборки, 42 инструмента, 23 направляемых инструмента Windows, запрет неверного сертификата, выполнение удалённой команды по WSS, восстановление связи, сохранение старого LAN-подключения; GitHub Actions 40781905752 завершён успешно; v0.2.8 опубликована с установщиками Setup и Update.
- Проверка 29.10.2026 через работающий MCP Tunnel: активный процесс ChatGptMcpAgent.exe PID 14536 имеет FileVersion 0.2.7.0 (запущен 2026-10-29 00:33:36 по системному времени Windows); Router Lite слушает 192.168.0.3:55201 (PID 2992), TCP 55202 не слушает. v0.2.8 не устанавливалась менеджером; bridge_ping успешен. Этот факт supersedes прежний текущий статус v0.2.6, но не меняет вывод о непроверенном межсетевом соединении. На маршрутизаторе не создавался проброс TCP 55202, правила брандмауэра не менялись. Физическая связь с MKATUSHONOK через Интернет и 23 удалённых инструмента остаётся НЕ ДОКАЗАНА. Публичный WAN IPv4 подтверждён чтением роутера; постоянство адреса основывается на словах владельца.
- authority: owner-directive, verified-repository, verified-runtime; supersedes earlier 'direct WSS not yet chosen', does not supersede physical acceptance gate.

- source: legacy-v2-state
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

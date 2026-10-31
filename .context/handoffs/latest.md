## HANDOFF GEN 74 — авторизация MKATUSHONOK и локальный Free Xray (31.10.2026 21:27 МСК)
Состояние после переноса WebView2 с DOMKAT на MKATUSHONOK. Обе машины используют ChatGPT MCP Tunnel v0.2.65; роль DOMKAT — сервер и единственный официальный MCP tunnel-client; MKATUSHONOK — клиент Router Lite (--agent), без своего официального tunnel-client. Связь Router Lite DOMKAT↔MKATUSHONOK подтверждена, MCP bridge_ping OK. На DOMKAT остаётся Happ для серверного ChatGPT/MCP. На MKATUSHONOK по прямому требованию владельца только локальный бесплатный Xray VLESS/TCP/REALITY для встроенного ChatGPT WebView2; режим free, без Happ и без прямого обхода при недоступности бесплатного узла. WebView2 использует локальный HTTP-прокси 127.0.0.1:11821. Предыдущий профиль MKATUSHONOK сохранён для отката; существующий профиль WebView2 заменён авторизованным снимком DOMKAT с перепривязкой Chromium master key к Windows CurrentUser DPAPI пользователя MKATUSHONOK. Реальная проверка GUI показала уже авторизованный ChatGPT Plus и историю чатов, повторный вход не требовался. На DOMKAT профиль и действующий MCP остались неизменны. Пакет передачи был зашифрован и прошёл контроль SHA-256; временный зашифрованный пакет, временный ключ передачи и промежуточные копии удалены; локальный откатный профиль оставлен. На MKATUSHONOK зарегистрированы автономный supervisor ChatGPTMCP-Client-FreeXray и обновлятор ChatGPTMCP-Client-FreeReality-Refresh с интервалом 3 часа. Последняя проверка обновлятора: код 0, 2 проверенных бесплатных узла, client-vpn-health ready, free mode активен. Ни один профиль, ключ или авторизационные данные в GitHub не публиковались. ВАЖНО: клиентский supervisor и локальные сценарии переноса/обновления пока установлены вне репозитория в профиле Windows MKATUSHONOK; штатный инсталлятор не гарантирует их восстановление при чистой переустановке. Этот пробел документирован как следующая инженерная задача. Действие уже выполнено; не повторять перенос без нового прямого запроса.
Перед дальнейшими изменениями обязательно уточнить роль целевого компьютера, проверить Router Lite computers.list, сохранность пользовательского WebView2 и отдельные клиентские службы. Текущий профиль MKATUSHONOK содержит реально перенесённую авторизацию; не перезаписывать его при обновлениях.

---

## Supervisor handoff generation 73 (31.10.2026 20:24 MSK)
LOCAL and GITHUB RELEASE ACCEPTANCE: installed DOMKAT v0.2.65.0, Happ active, GUI PID6152, MCP proxy bridge PID9368, original official tunnel-client PID8624 and MCP runtime PID33264. MCP bridge_ping OK and Router Lite MKATUSHONOK remote client connected. FreeReality 3h Task Scheduler latest result 0, next 2026-10-31 22:28 MSK; two DPAPI-protected standby VLESS/REALITY nodes available, no exposed key files. One original authenticated WebView2 browser profile retained, user still logged in to ChatGPT Plus. Isolated primary outage: first standby validated HTTPS and promoted, HTTP401 expected from unauthenticated api.openai.com endpoint. Five stress cycles Happ->Free->Happ gave 15/15 HTTPS successes. GUI's free-node-rotated notification handler tested successfully in installed v0.2.65 and log; test notice removed. Legacy unused FreeReality separate browser profile (19.4MB) and four ephemeral lab folders removed, but authorized WebView2 profile and local backup retained. Git audit of 68 changed files found 0 leaked VLESS URIs/private keys/DPAPI binaries, Git whitespace and context integrity passed. Feature branch and origin/main safely reconciled with a normal merge, no force push; both published at source commit eacd133 (code change 3010e8b). Stable annotated v0.2.65 tag points to eacd133, published GitHub release https://github.com/lvlaksim1/chatgpt-mcp-tunnel/releases/tag/v0.2.65 is latest, not draft or prerelease. Two full/setup and update binaries plus SHA256 manifest verified local SHA256 and remote asset sizes. No GitHub Actions used to build, no prerelease issued. Local build and installation happened first.
No further changes needed to clients; MKATUSHONOK connected. GitHub stable v0.2.65 already live, latest release, not prerelease.

---

## Supervisor handoff generation 72 — VPN v0.2.65 (31.10.2026 20:18 MSK)
Owner instructed completion of remaining v0.2.64 VPN work and GitHub archival. v0.2.65 is installed on DOMKAT, deployment status VERIFIED, GUI PID6152, proxy bridge PID9368, original official tunnel-client PID8624 and MCP runtime PID33264 preserved. MCP bridge_ping succeeded after deployment, Router Lite remote client MKATUSHONOK remained connected. Existing Happ mode remains default, unified authorized ChatGPT WebView2 profile confirmed signed-in Plus with original chats after installation (screenshot inspected). Three-hour task FreeRealityRefresh was manually started: result 0, verified THREE live VLESS/REALITY nodes among eight probed, stored primary and two separate CurrentUser DPAPI-encrypted standby profiles under LocalAppData; no raw VLESS URIs or UUIDs in status/catalog or version control. Updated bridge tries a pretested standby using separate Xray loopback 11822 before Happ fallback, then promotes it to standard port 11821, closes stale MCP sockets for reestablishment, preserves one official client. Isolated forced-primary-failure test PASS: auto notice free-node-rotated, remained free and api.openai.com returned expected unauthenticated HTTP401 through standby, production client PID8624 unaffected and production mode Happ. Five isolated stress cycles Happ-Free-Happ: 15/15 HTTPS requests succeeded without any production MCP client/bridge restart. Installed GUI TrayContext displays user-facing Windows balloon for free node rotation or fallback using safe reason codes and fresh notice timestamps. Injected event test on installed GUI PASS, one log entry for free-node-rotated; test notice removed afterwards. Seven Python regression scripts PASS including DPAPI standby preservation across source outage, six published binary selftests PASS, and all three full/setup/update installers SHA256 verified. Original signed-in browser backup under .tools/deployment-backups/pre-v0265-standby-vpn, never publish. Unused legacy WebView2-FreeReality separate browser profile (383 files, 19.4MB) deleted after verifying not in use and original signed-in profile preserved. Technical evidence docs/VPN-STANDBY-FAILOVER-V0265-2026-10-31.md. No actual hours-long public-node outage simulated against live authenticated browser; isolated failure and real proxy HTTPS tests passed.
DOMKAT current mode Happ, old duplicate free browser profile removed after safe checks. Git publication review pending in this handoff.

---

## Supervisor handoff generation 71 (31.10.2026 19:40 MSK)
Owner specifically required ONE pre-existing authorized WebView2 ChatGPT profile in BOTH Happ and Free Xray modes; separate new browser profile prohibited. v0.2.64 installed on DOMKAT GUI PID7752, bridge PID38032. Current single authorized UDF remains %LOCALAPPDATA%/ChatGPTMCPAgent/WebView2/ChatGPT (EBWebView child). VpnModeController.BrowserProfileFolder now always returns original folder. Switch waits asynchronously on CoreWebView2Environment.BrowserProcessExited after disposing previous WebView2 controls, before creating the different proxy-configured browser process. Free selection preflights HTTPS via server bridge before GUI rebuild and suppresses timer races via IsTransitioning flag. GUI also watches server route mode and recreates browser after external watchdog failback. Server ChatGptMcpProxyBridge now monitors free HTTPS health every 20s and atomically switches to Happ after 3 failures (isolated forced-failure test PASS, Happ HTTP200). Free node update DPAPI profile is preflighted using Xray isolated port11822 before active Xray at11821 is replaced (isolated live HTTPS test PASS, Xray PID35640->26492, official MCP PID8624 unchanged). Real installed GUI test Happ->Free->Happ showed SAME authenticated ChatGPT Plus account and full existing history in both modes, no second login, same original UDF, official MCP bridge_ping OK and MKATUSHONOK RouterLite client connected during Free; GUI and server route both changed coherently. Another real GUI test simulated external failback by changing saved mode Free->Happ: app autonomously reopened original authorized profile with VPN:Happ and no login, while original MCP/tunnel-client PIDs unchanged. Final GUI v0.2.64, client PID8624, Runtime PID33264, bridge PID38032, mode Happ, free listener stopped. Full Setup/Update local artifacts SHA256 3/3 verified. One initial deploy failed due reserved PowerShell $PID function parameter; original v0.2.63 and MCP were restored, bug fixed, second autonomous deployment verified. Source and binary/profile backup under .tools/deployment-backups/pre-v0264-single-profile-retry, auth profile ~219MB, NOT in Git. No GitHub release was published. The previous v0.2.63 legacy unused FreeReality separate profile was not deleted. Full report docs/VPN-SINGLE-AUTH-PROFILE-FAILBACK-V0264-2026-10-31.md.
DOMKAT now v0.2.64, mode Happ; all active MCP processes stable. Previously deployed separate FreeReality WebView2 folder is legacy unused and not deleted.

---

## VPN test handoff gen 70 (31.10.2026 18:58 MSK)
Owner instructed final verification of DOMKAT Happ/Free switch, performed live on v0.2.63. Autonomous rollback protected free MCP tests: first transient example.com failure but free control-plane api.openai.com/v1/models HTTP401, MCP /readyz 200 and tunnel-client TCP socket to bridge 11827 with bridge mode=free; second complete autonomous acceptance PASS all criteria (example.com HTTP200 via Free Xray, api.openai.com HTTP401, /readyz HTTP200, bridge free mode, client TCP socket connected, unchanged client PID8624 and bridge PID28024, successful rollback to Happ HTTP200). Live GUI topbar switch Happ->Free with explicit warning confirmed, ChatGPT public guest page loaded through separate Free WebView2 profile; isolated installed browser smoke test returned 0. Live MCP bridge_ping returned OK and Router Lite computers_list retained DOMKAT + MKATUSHONOK when mode=free, with matching bridge health free metric 67 connections. Isolated invalid-mode bridge test returned HTTP503 fail-closed and Happ HTTP200 after restoring, without touching production listener. A temporary mismatched GUI status after an external guardian rollback revealed a genuine defect: external mode-file changes do not automatically recreate already-open WebView2; first native Happ click was ignored because persisted mode already Happ. Correctly restored GUI via native switch, screenshot confirms authenticated ChatGPT Plus and VPN:Happ restored, all original chats. Final status 18:57 MSK: GUI version0.2.63, GUI PID5060, tunnel-client PID8624, runtime PID33264, bridge PID28024, persisted mode Happ, bridge observed Happ, free port 11821 stopped, refresh task 0. Still NOT completed: authenticated Free-profile ChatGPT conversation/login; automatic failback to Happ on remote Free node outage; hot-reload of Free Xray after subscription update; browser synchronization on external automatic mode change. Unknown public node operator and transient first failure mean reliability/trust remains uncertain. Read docs/VPN-E2E-ACCEPTANCE-2026-10-31.md. No new release/commit.
Do not mistake file-only Happ rollback for browser recreation. App currently restored to actual Happ GUI and route with no leftover test Xray listener.

---

## Handoff generation 69 — server MCP VPN switching (31.10.2026 18:10 MSK)
Owner affirmed server-only external MCP VPN switching on DOMKAT; remote Router Lite clients must not run any tunnel-client. Verified native OpenAI tunnel-client 0.0.18 supports --http-proxy for control-plane, MCP and Harpoon. Implemented independent .NET8 ChatGptMcpProxyBridge.exe at loopback 127.0.0.1:11827 with HTTP CONNECT forwarding to Happ local HTTP 127.0.0.1:10809 or Free Xray HTTP 127.0.0.1:11821 based on existing GUI network-mode.json. Bridge monitors mode changes every 500ms and closes old persistent TCP tunnels to force Go reconnect on new route without restarting tunnel-client. Bridge controls no OS-wide routes or VPN adapters, no second official MCP client. ServerStart.ps1 starts/validates bridge only for DOMKAT server role, launches official existing tunnel-client with --http-proxy http://127.0.0.1:11827. Remote RouterLite --agent role unchanged. Isolated pilot 3/3 HTTPS example.com HTTP200 Happ->Free->Happ through SAME bridge PID. v0.2.63 deployed locally using detached backup-and-rollback script; deployment status VERIFIED. New tunnel-client PID8624 (command line verified proxy flag), MCP runtime child PID33264, bridge PID28024, GUI PID5060 (v0.2.63), Happ PID9908 untouched. Local bridge-health.json reported mode=happ, successfulUpstreamConnections=5, one established local bridge socket observed; proves live control-plane uses bridge. MCP bridge_ping returned CHATGPT_MCP_TUNNEL_PING_OK post reconnect. Router Lite computers.list shows DOMKAT local and MKATUSHONOK remote CONNECTED. Three v0.2.63 Setup/Update installers verified SHA256 3/3. Scheduler free-server refresh still active, next due 2026-10-31 19:28 MSK. Limitations: authenticated production ChatGPT/MCP over unknown public Free exit not tested; automatic failback on remote Free outage not implemented, Free mode currently fail-closed and can return to Happ via selector; in-use free Xray not hot-reloaded upon scheduler profile updates. Original Happ and clients unchanged; backup at .tools/deployment-backups/pre-v0263-mcp-proxy. Details docs/MCP-SERVER-VPN-BRIDGE-V0263-2026-10-31.md. Local source uncommitted; do not reset.
No client-side tunnel-client required; Router Lite agent MKATUSHONOK remains connected. Never restart MCP for ordinary mode switching after one-time v0.2.63 cutover.

---

## Handoff generation 68 — VPN switching (31.10.2026 17:08 MSK)
The owner requested the user-facing two-way VPN selector Happ (current) or free servers. Installed GUI v0.2.62 on DOMKAT with top titlebar dropdown and equivalent tray recovery menu. Default remains Happ and existing authenticated WebView2 profile remains untouched. When the owner explicitly chooses Free, a privacy consent prompt warns about unknown public node operators. The program checks the scheduler's latest 6h verified Free-Reality-Auto.dpapi, Windows CurrentUser DPAPI decrypts the Xray JSON, validates loopback-only proxy and VLESS/REALITY, checks bundled Xray-core v26.10.29 SHA256 and starts its own Xray process at 127.0.0.1:11821. A distinct FreeReality WebView2 user-data folder prevents Chromium proxy argument conflicts and may need new login. On mode choice only the GUI form is recreated; original adopted tunnel-client PID 24884 and MCP runtime PID 28372 remain alive, Happ is not stopped. Switching back to Happ kills only GUI-owned Xray. A missing profile/binary/port fails closed; does not switch silently to direct. Source tests for GUI, network and voice PASS; packaged selftest, main-chat-ui, toolbar-dom, voice-dom and webview-network exit 0; installed BrowserChrome inline JS syntax PASS. Full local Setup and Update v0.2.62 installers verified SHA256; local backup/rollback path prepared. Deployed with .tools/deploy-v0262-vpn-safe.ps1 and status verified. Standalone isolated --test-main-chat-live returned 103, so genuine ChatGPT GUI E2E remains UNVERIFIED. CRITICAL LIMITATION: selected mode changes WebView2 only, not yet the official MCP tunnel-client transport; no running Xray hot-reload on scheduler's server changes. This remains follow-on work. Documented docs/VPN-MODE-SWITCH-V0262-2026-10-31.md. Local sources uncommitted, no GitHub release posted.
Do not confuse installed GUI switch with full MCP client network routing. Existing Happ and MCP client remain unchanged.

---

## Handoff generation 67 — automated free VPN (31.10.2026 16:36 MSK)
Owner requested automated updates of free VLESS/TCP/REALITY servers, not single static URI. Implemented scripts/Vpn/free_reality_updater.py, free_reality_sources.json with 3 independent approved HTTPS feeds (AetrisVPN and 0xRadikal verified/fast), Windows Task Scheduler task ChatGPTMCP-FreeReality-Refresh (user DOMKAT\Maksim, every 3 hours indefinitely via daily repeating trigger, StartWhenAvailable, no admin, Python 3.13 pythonw.exe). Scheduled task's first immediate forced test exited result=0; next run 2026-10-31 19:28:28 MSK. Initial real refresh found 512 deduplicated valid candidates, probed 11, got 2 healthy; selected 147.45.245.177:443. Task-managed second refresh checked two, both healthy. Selected config encrypted using CurrentUser DPAPI under %LOCALAPPDATA%/ChatGPTMCPAgent/Vpn/profiles/Free-Reality-Auto.dpapi, catalog/status files omit raw URIs and UUID/password; Windows DPAPI roundtrip test successful. Selected profile was actually started with Start-ProtectedXrayProfile.ps1; isolated local HTTP proxy 127.0.0.1:11821 returned https://example.com HTTP 200 and stopped without orphan listener. Six dedicated offline tests passed (source filtering, base64, private address rejection, Xray config, failing source preserve last-good, selection priority); prior VPN isolation regression passed. Concurrent refresh lock, fail-closed test port and SHA-256 verified Xray binary. Status and disable scripts and detailed docs/VPN-FREE-SERVERS-AUTO-REFRESH-2026-10-31.md added. IMPORTANT: auto-refresh selects and stores a trusted-at-runtime public candidate but does NOT automatically redirect the installed authenticated WebView2 or live MCP connection to unknown free node, and does NOT yet auto-restart an actively running selected Xray on failure. Those are separate future production UI and failover stages requiring E2E testing and privacy choice. Happ, GUI v0.2.61 and original MCP transport unchanged. Source tree not committed or published; do not reset.
Inspect task via Get-ScheduledTask -TaskName ChatGPTMCP-FreeReality-Refresh and Get-FreeRealityStatus.ps1. Protected file in LocalAppData. Do not assume active user ChatGPT uses free node. Preserve working Happ/MCP.

---

## Handoff free Reality, generation 65 (31.10.2026 14:33 MSK)
The user asked for a free VLESS/REALITY link. Public AetrisVPN free list found at https://raw.githubusercontent.com/flaafix/AetrisVPN-white-list-lite/main/AetrisVPN.txt. Verified individual node on DOMKAT with Xray-core v26.10.29: VLESS/TCP/REALITY 147.45.245.177:443, observed exit 77.110.116.110; actual HTTPS example.com HTTP 200. Earlier public 0xRadikal verified candidates (16 tested) all failed from this PC; 7th Aetris candidate passed. Public URI is in .tools/vpn-pilot/free-public-reality-verified.txt (not in Git). Independent egress IP differs from the Happ local HTTP proxy exit IP. Existing Import-VlessReality.ps1 was updated to accept standard omitted encryption=none and type=tcp parameters; DPAPI import, protected Xray launch and stop passed. Isolated WebView2 test via the public node passed exit 0; neither login nor MCP client was switched. Free public exit operator unknown; do NOT route authorized ChatGPT, secrets or other private traffic without explicit informed decision. Public availability can expire. Real Happ and MCP remain operational, experimental Xray stopped and test profile deleted. Details docs/VPN-FREE-REALITY-VALIDATED-2026-10-31.md.
No release was made; existing installed app, Happ and MCP unchanged; avoid claiming real authorized ChatGPT traffic or long-term VPN stability.

---

## LamaVPN handoff — generation 64 (31.10.2026 14:12 MSK)
Owner authorized reusing existing paid LamaVPN Germany in embedded Xray. Happ 4.6.1 active on DOMKAT, LamaVPN Germany selected, VLESS/TCP/REALITY; subscription visible until 2027-01-05. An old URL to sub.thelama.ru was found in Happ's subscription_log; current HTTP 200 returns only 'App not supported' dummy VLESS (Hide-Settings:1), including ordinary Happ/v2rayNG/sing-box and ClashMeta request formats; it has NOT been verified to be the current subscribed URL. The frequently updated tshbotpopup.ru belongs to unrelated Toshibu VPN, not used. Happ subs.db has one high-entropy stored subscription blob; no readable live Germany credentials extracted. Happ itself and any hidden providers were NOT bypassed. Implementation added: scripts/Vpn/Import-VlessReality.ps1 imports one authorized standard VLESS/TCP/REALITY share URL from local file, validates with Xray v26.10.29 and encrypts settings with Windows DPAPI CurrentUser under LocalAppData. Start-ProtectedXrayProfile.ps1 decrypts only locally, starts independent loopback proxy, removes plaintext temporary file; Stop-ProtectedXrayProfile.ps1 uses PID and EXE ownership checks. tests/test_vpn_protected_import.ps1 PASS (synthetic UUID/REALITY key, DPAPI ciphertext, loopback listener, safe stop and test file cleanup). Happ sing-box TUN config has process-name direct bypass for xray.exe, encouraging isolated testing but does not prove independent external REALITY routing. No real LamaVPN profile imported, no independent ChatGPT/MCP E2E accepted. The current missing dependency is a provider-approved ordinary VLESS/REALITY profile or standard subscription from LamaVPN management/support. Installed v0.2.61 GUI, existing Happ, tunnel-client and MCP runtime unchanged; experimental xray ports stopped. See docs/VPN-LAMAVPN-PROFILE-INTEGRATION-2026-10-31.md.
Primary blocker: provider hides usable Germany config; don't mistake old sub.thelama placeholder or Toshibu link for live access. Never move real secrets into source or transcript.

---

## Handoff VPN — поколение 63 (31.10.2026, 13:30 MSK)
Xray-core official v26.10.29 downloaded to .tools/vpn-pilot; official Windows archive SHA256 verified. Pilot direct HTTP proxy 127.0.0.1:11809 returned HTTPS example.com 200 but ChatGPT timed out. Chained HTTP proxy 127.0.0.1:11810 -> Happ SOCKS5 10808 passed isolated WebView2 example.com test (exit 0); chatgpt.com curl HEAD returned Cloudflare 403, not a login test. False proxy port 11999 correctly failed WebView2. Temporary local VLESS client/server HTTP test passed; neither a remote REALITY tunnel nor independent ChatGPT was proven. Opt-in proxy selection added to new GUI source and future ServerStart.ps1 but NOT deployed. No VPN credentials imported, no Windows routing changes, no additional MCP tunnel. Pilot processes stopped; installed GUI remains v0.2.61; original MCP client and runtime PIDs unchanged. Next blocker: a trusted independent VLESS/REALITY server and end-to-end verification for both WebView2 and tunnel-client. See docs/VPN-XRAY-PILOT-2026-10-31.md.
Active user request: integrate VPN without focusing on Unbihex. Tested Xray-core pilot; real independent remote server remains open. Do not conflate local/chain tests with paid Happ replacement, do not alter live MCP processes.

---

## Передача менеджеру: поколение 62, 31.10.2026, 12:32 MSK
Пользователь подтвердил, что звук голосового ответа модели реально слышен в колонках на DOMKAT. **Голосовой вывод принят; микрофона у владельца пока нет.** GUI v0.2.61 ранее установлен, пакеты Setup/Update проверены, MCP-сервер и клиент сохранены. Не утверждать завершение тестов STT и TTS 10/10.

---

## LATEST HANDOFF 31.10.2026 — generation 61, TTS without a microphone
DOMKAT installed ChatGPT MCP Tunnel GUI v0.2.61.0, local Sherpa/Piper voice worker v0.2.61, MCP runtime v0.2.58 remains PID 28372 and tunnel-client PID 24884; bridge ping OK. Voice synthesis through installed Realtek USB2.0 Audio completed with speech_finished interrupted=false. Missing mic verified: NAudio sees 0 input devices, USB2.0 MIC not present. The TTS worker now starts independently on voice_reply; missing-mic branch returns a localized error. Live normal ChatGPT WebView2 produced two actual text responses. The old [data-message-author-role=assistant] selector returned zero in live host; current responses are inside class DilResponseRoot. Both selectors are supported by hot-loaded ChatGPT-Voice.js; a live DOM observer identified the second reply, and a controlled bridge event started worker and generated audio. A physical user-confirmed hearing test and consecutive actual auto-audio 2/2 and 10/10 remain OPEN. Tested node JS syntax, GUI/voice builds, mock WebView2 DOM tests, TTS without mic and local audio playback. Verified full and update local installers v0.2.61 contain the final JS; SHA256 manifest available. Local source changes remain uncommitted; do not reset, force push or restart the active tunnel.
Owner's current request: speaker/TTS must work while no microphone is available. The app is installed v0.2.61; actual source branch is feature/webview2-primary-ui-v0248. Never apply historical v0.2.37 MCP Apps-only directives, and do not claim audible confirmation. DIL DOM compatibility implementation: src/ChatGptMcpAgent/Themes/ChatGPT-Voice.js.

---

## АКТУАЛЬНО 31.10.2026 — менеджер проекта, поколение 60
**31.10.2026 11:26 MSK / project-manager / generation 60.** Установлена v0.2.60 GUI, PID 27916; живой tunnel-client PID 24884, MCP server PID 28372 runtime 0.2.58; bridge_ping OK, deployment verified. Voice Zipformer RU+Silero VAD+Piper Irina Sherpa-ONNX 1.13.8; TTS→STT и mock DOM PASS. USB2.0 MIC Unknown: живую голосовую E2E НЕ считать принятой. Backup v0.2.59; безопасная установка по GUI PID без --shutdown; авторизационный WebView2 профиль не менялся. Рабочая feature/webview2-primary-ui-v0248 HEAD 33d114b с незакоммиченными source правками, не сбрасывать. Следом живые голосовые и вкладочные тесты, git сохранение. Мониторинг установки отключён. Старый v0.2.37/MCP Apps-only ниже superseded.

---

## Актуально 30.10.2026 — подтверждённый E2E и блокер повторного сообщения
2026-10-30, capsule 59. Project manager of universal ChatGPT MCP Tunnel (seven owner-confirmed strategic goals unchanged). DOMKAT running stable v0.2.37 agent/MCP runtime, hashes verified, Router Lite DOMKAT + MKATUSHONOK connected, ping OK. Fresh normal ChatGPT conversation after Plugins Manage Refresh successfully exposed experimental chat.thread.relay.probe tool and loaded new MCP Apps resource thread-relay-probe-v0240 with automatic polling. Canary first message 0f62f2defcb04778a60437fc83157760 answered exactly via real MCP ui/message and chat.reply. Canary second 52382de5d39f4f6ea2178de0781b260f ACKed but no reply; pending+ui-forwarded, do not retry. Host continued poll. Assistant-audience route metadata appeared in visible user text: bug to fix. Experiment code and details in origin/experiment/thread-relay-host-20261030 commit 0f6af99. v0.2.41 reverted to stable v0.2.37, extra probe HTML/canary selector archived. Second Refresh after downgrade attempted and settings page recovered after Ctrl+R; final new-chat tool listing NOT independently verified because UI read blocked by platform. Next work: repeated message problem, true model-only context, 2/2 then 10/10; never use DOM transport/API/Work/Codex.

## Приоритетное обновление — 30.10.2026, поколение 58
Handoff 30 Oct 2026, manager capsule 58. Installed DOMKAT and MKATUSHONOK reachable; DOMKAT agent/MCP runtime v0.2.37, source main. Experiment branch origin/experiment/thread-relay-host-20261030 b7c527e saved with report docs/MCP-THREAD-RELAY-HOST-EXPERIMENTS-2026-10-30.md. v0.2.39/40/41 introduced isolated Thread Relay, versioned app resource, event telemetry and tools/list_changed. Real Host existing normal chat continued to run stale lifecycle widget rather than newly returned URI; no resource-read or relay-poll; list_changed emitted by server but not visibly reflected in model's existing tool listing. New thread canary didn't send messages. Next: one-time fresh ordinary ChatGPT conversation to isolate metadata cache; then one real reply, recovery, 10/10. False ready in chat.ui.open also needs fixing. User does not accept manual activation before each message. Do not use Work/Codex/API/DOM sends. All seven strategic goals from generation 56 remain.

## Verified 2026-10-30 live MCP host experiment
Handoff 30 October 2026 after real MCP Host experiments, capsule 57. Original goals seven unchanged. Experimental branch origin/experiment/mcp-host-pip-20261030 commit de43821 holds source/test/report. v0.2.38 temporary diagnostic installed and fully rolled back to v0.2.37 (agent/runtime), bridge ping restored, DOMKAT and MKATUSHONOK connected. Real Thread MCP Apps host=chatgpt, protocol 2026-01-26, available inline/fullscreen, pip request twice produced fullscreen. Hidden iframe heartbeats observed. Global open only tool-open, and real local queued test through chat.ui.open failed to obtain forwarding ACK or reply; test-owned pending record archived safely. Current .NET MCP server supports resources/subscribe/unsubscribe but not 2026-07-28 Tasks/MRTR/subscriptions/listen; Host did not advertise sampling. No actual reliable autonomous chat; preserve P0. See docs/MCP-HOST-PIP-EXPERIMENTS-2026-10-30.md in experiment branch. Main has no experimental runtime code, and no Work/Codex/API/DOM messaging.

## Долгосрочные цели, лично подтверждённые владельцем 30.10.2026 — capsule 56
ChatGPT MCP Tunnel — универсальная, независимая среда «человек — ИИ — все его компьютеры», а не только Windows MCP-набор или чат. Семь целей: [1] полное управление Windows через MCP и сложные задания; [2] географически распределённые компьютеры и файлы; [3] равноправные узлы без обязательного центрального ПК; [4] полноценный локальный двухсторонний чат без кнопок/подключений; [5] бесплатный автономный ввод/вывод голоса; [6] другие LLM, включая Goose/Ollama; [7] безопасность и минимальные внешние зависимости. Обычный ChatGPT через MCP без API/Work/Codex; WebView2 — только вход; Windows 10, Happ не отключать, цель будущей независимости от него. Один туннель, локальные сборки и готовые GitHub Releases без Actions. НЕ принимать стратегические цели за реализованные возможности. Сейчас P0 — автоматический MCP-чат; v0.2.37 установлена, полный E2E НЕ принят. При восстановлении читать первыми актуальные цели и планы капсулы 56, затем исторические записи.

## ПЕРЕДАЧА ПРОЕКТА 30.10.2026, 16:43 МСК — generation 55
Роль: project-manager. Главная цель владельца: локальный MCP Tunnel чат должен работать как полноценный мессенджер, без ручных вызовов chat.ui.open, без кнопок и обслуживания iframe. Только MCP для сообщений; WebView2 только авторизация; обычный ChatGPT, не Work/Codex/API.
DOMKAT: установленный ChatGptMcpAgent и MCP Runtime v0.2.37+45d3289, связь bridge_ping OK, Router Lite DOMKAT и MKATUSHONOK connected. Git source main: runtime 45d3289, капсула до обновления dec5430. GitHub release v0.2.37 pre-release, локальный откат на v0.2.36 готов; разработка D:/Dev/ChatGPT-MCP-Tunnel.
Файлы: src/ChatGptMcpAgent/ChatGptWebViewAuthForm.cs (изолированный WebView2 профиль); src/ChatGptMcpTunnel/ChatRelayApp.html и ChatUiTools.cs (MCP Apps ui/message + chat.reply); docs/MCP-WEBVIEW-AUTH-V0237.md. Node/CDP браузерный отправитель исключён.
Статус: локальные окна/очереди/синтетические MCP тесты есть; реальный автоматический двусторонний чат не принят. Host управляет iframe MCP Apps; без host-активации ничего не отправляется. Два исторических pending+ui-forwarded не дублировать. WebView2 вход не равен открытию MCP Apps. Тестировать настоящую доставку в обычном ChatGPT, 10/10, после restart и на MKATUSHONOK; не начинать с очередного косметического релиза.
Сохранять GitHub Actions отключёнными; не выключать Happ и не переходить на Windows 11. Старые записи капсулы ниже — исторические.

## 30.10.2026 — v0.2.37 (MCP-only + WebView2 auth)
Handoff 30 Oct 2026, 16:33 MSK, project-manager. v0.2.37 WebView2-auth/MCP-only transport is installed on DOMKAT, origin/main 45d3289, prerelease published with setup/update installers and checksums. DOMKAT and MKATUSHONOK connected, backup of v0.2.36 saved under .tools/local-release/v0.2.37/deployment/backup-before-v0237. WebView2 isolated persistent profile under LOCALAPPDATA/ChatGPTMCPAgent/WebView2/ChatGPT. Node/CDP sender removed/deactivated. MCP Apps auto-start upon host tool input/result; ui/message visible text contains user message only; assistant-audience text includes route to chat.reply. Tests pass, real host E2E remains open and server cannot force host iframe creation after teardown. See docs/MCP-WEBVIEW-AUTH-V0237.md. No Work/Codex/API, no GitHub Actions.

## 30 October 2026: automatic browser transport preview
Handoff 2026-10-30 15:53 MSK. Project manager restored, target autonomy. Experimental v0.2.36 source 7367ae5 on origin/main and GitHub prerelease v0.2.36. Installed DOMKAT successfully with independent installer; backup v0.2.35 under .tools/local-release/v0.2.36/deployment/backup-before-v0236. Agent/runtime 0.2.36, MCP ping works, DOMKAT/MKATUSHONOK connected. Bundled Node browser worker live, dedicated Yandex profile launched on chatgpt.com (localhost CDP), not yet signed in; browser heartbeat waiting_login, no background chat result accepted. MCP Apps fallback retained until web auth. Existing unbound chats and attachments deliberately not auto-migrated. See docs/BROWSER-CHAT-TRANSPORT-V0236.md.

# Latest handoff

## 2026-10-30 MSK -- v0.2.35 released, live acceptance open
Persistent manager project-manager, same identity. Source commit 729838e is on origin/main and stable GitHub Release v0.2.35 has ChatGPT_MCP_Tunnel_Setup_v0.2.35.exe, ChatGPT_MCP_Tunnel_Update_v0.2.35.exe and SHA256-v0.2.35.json. Packaged Windows/MCP/UTF-8/Node/GUI tests passed. Existing cloud route uses target:active; only explicit local '+ New Chat' may trigger target:new; discovery lease prevents dual creation. Visible content has only user text plus compact reply-ID footer. No real ChatGPT E2E accepted yet. Installed DOMKAT v0.2.33 unchanged (owner previously elected self-install); no Work/Codex/API, no GitHub Actions. Next: controlled install and E2E ordinary mode verification. docs/CHAT-ROUTING-V0235.md


## 2026-10-30 05:25 MSK: stable v0.2.18 relay restoration
30.10.2026 owner instructed restoring accepted v0.2.18 text chat. Completed local source changes for v0.2.30: untouched ChatRelayApp.html, disabled competing Bootstrap and tray HostActivator, corrected Bootstrap Russian label, retained legacy resource aliases and version display. Ten targeted suites and packaged builds with SHA256 pass; installed runtime is still v0.2.29 until deployment. Live E2E acceptance not yet completed. No Work/Codex/API, GitHub Actions, or automatic release. docs/STABLE-CHAT-RESTORE-2026-10-30.md.


## Дополнение от владельца 30.10.2026 — версия в уведомлении
Новая отложенная задача: **в следующей версии ChatGPT MCP Tunnel добавить указание номера версии программы**. В контексте обсуждения окна управления — отображать текущую установленную версию в заголовке/строке уведомления, подгружая её из версии реально запущенного агента. Пока только сохранено в капсуле; код, установщик и рабочая программа не менялись. Не делать отдельный релиз без поручения. Владелец сказал «на будущее запомни».

## LATEST HANDOFF — 2026-10-30 04:14 MSK — normal ChatGPT vs Work
Persistent role `project-manager`, not supervisor. Owner instructed to restore automatic new dialogs in ordinary ChatGPT from local Tunnel; previous new threads were Work. Investigation compared v0.2.18, v0.2.28 and current ChatGPT host. Verified Global must never originate `ui/message`. Today a diagnostic Thread MCP App in this ordinary ChatGPT conversation reported `hasOpenAiMessage=true` and `hasDeepLink=false`: extension exists, host route metadata absent.

Candidate `v0.2.29`, commit `e80e3ea`, locally built on DOMKAT, tests passed, full/update Inno files SHA256-checked and directly published as GitHub **pre-release**, without GitHub Actions. Global HTML has **no ui/message**; bootstrap in normal ChatGPT validates extension, route and offers **one-time manual confirmation if host hides /c URL**, revokes it on context change, keeps messages pending in uncertain Work/Global contexts. Local message queue had 4 pending old messages; they were not deliberately forwarded or deleted.

Installed DOMKAT remains `v0.2.28` at last verification. **LIVE E2E ORDINARY THREAD IS NOT VERIFIED**, and successful app/host ack is not proof of no Work. Next after owner manual installation: single controlled local message from a visually ordinary /c ChatGPT bootstrap, inspect resulting UI and receive reply; avoid replaying four previous pending messages blindly. Keep one tunnel, no Work/Codex/API, system Happ, GitHub storage only, local development in `D:\Dev`. See `docs/ORDINARY-CHAT-ROUTING-2026-10-30.md`.

Authority/provenance: direct owner request 30.10.2026, Git history, local tests/release, real ChatGPT lifecycle diagnostic, DOMKAT VersionInfo. Older handoff entries below are historical.

## ПОСЛЕДНЯЯ ПЕРЕДАЧА МЕНЕДЖЕРУ — 30.10.2026, 03:46 MSK
**Чей контекст:** ровно существующий `manager_id=project-manager` (Project Manager 2.1.0). Владелец уточнил: вызван менеджер проекта ChatGPT MCP Tunnel, не супервизор; прежняя реплика ассистента про «капсулу супервизора» ошибочна. Никакого переключения identity не было. Владелец приказал **актуализировать капсулу менеджера**.

**Текущий факт после сверки:** DOMKAT `D:\Dev\ChatGPT-MCP-Tunnel`, Git `main` HEAD `ad28e28218a404a645f0df805f8d4f205fd2e079`, статус чистый до обновления капсулы; установлен `D:\ChatGPTMCPAgent\ChatGptMcpAgent.exe` с `0.2.28+ad28e28`. Пользователь самостоятельно установил `v0.2.28` после того, как ассистент локально собрал/протестировал/опубликовал релиз через GitHub Releases **без GitHub Actions**. Прежние пункты «выпустить/установить 0.2.28» закрыты.

**Реализовано в v0.2.28:** исправление повреждённых кириллических подписей; в индикаторе появилась строка исполнителя; `executor` на каждый Windows MCP tool call, в Router Lite, desktop.batch и file.transfer. Default ChatGPT, `project-manager` → Менеджер проекта, `supervisor` → Супервизор. Это информационное значение **не удостоверяется Host**. Локальные тесты прошли; реальная экранная приёмка всех подписей после самостоятельной установки пока НЕ проведена.

**Непреложные правила:** главная локальная разработка и сборки на DOMKAT в `D:\Dev`, GitHub только хранилище исходников и Release assets; Actions выключены. Пакетные сценарии с fail-fast вместо микровызовов; GUI ~1 с между действиями, CLI без искусственных пауз; локальный STOP. Не Work/Codex/API, системный Happ VPN не выключать, не плодить второй туннель. Роль разговора — project-manager, не supervisor.

**Продолжить:** при новом поручении проверить уведомление исполнителя на живом `v0.2.28`, затем отдельным будущим этапом — корректные состояния «выполняю / ожидаю модель / завершено» и ограничения STOP для фоновых процессов. Старые E2E-задачи обычного ChatGPT/многокомпьютерной работы не закрывать без фактической проверки.

**Provenance:** владелец в текущем разговоре 30.10.2026; Git/version/runtime на DOMKAT; успешная локальная сборка и опубликованный релиз v0.2.28. Это уточнение **заменяет** любые более старые приоритетные заметки, утверждающие, что последняя установленная версия 0.2.27 или что менеджер не вызван.

## UPDATE 30.10.2026 — исполнитель в уведомлении
Владелец распорядился добавить персонализированного исполнителя: «Менеджер проекта», «Супервизор», и для обычного чата «ChatGPT». Source v0.2.28: \`ControlExecutor\`, per-invocation \`executor\`, per-operation \`ControlState\`, WinForms строка «Исполнитель», Router Lite, batch, file transfer propagation. Local builds/WinForms/Python/Node/Router tests PASS. Нет проверенного автоматического определения identity из Host: новая схема зависит от явного параметра модели, без глобального состояния и без привязки к авторизации. Документ docs/EXECUTOR-ATTRIBUTION-2026-10-30.md, owner decision .context/decisions/executor-attribution-2026-10-30.md. Последние Unicode исправления v0.2.28 сохранены. GitHub Actions отключены; дальнейший выпуск только локально.

## АКТУАЛЬНАЯ ПЕРЕДАЧА 30.10.2026, ~03:15 MSK
Продолжается тот же `manager_id=project-manager` v2.1.0. Владелец потребовал восстановить менеджера и передать весь разговор. **Прочитать прежде старой истории:** `.context/decisions/operating-model-2026-10-30.md` и `docs/PROJECT-MANAGER-HANDOFF-2026-10-30.md`.

Новое обязательное решение: **DOMKAT `D:\Dev\ChatGPT-MCP-Tunnel`** для разработки, сборок/тестов/инсталляторов; GitHub только исходники/история/готовые Release; Actions disabled. Локально выпущена и установлена v0.2.27, MCP работает. В исходниках четыре **незакоммиченные** Unicode-правки v0.2.28, локальная сборка с предупреждением CS0162, релиза ещё нет. Не потерять их. Пакетное исполнение вместо микровызовов; GUI ~1 сек, CLI без паузы, STOP локальный. Разделение «работаю/жду модель/закончил» ещё не реализовано.

Продолжать с проверки этих правок и локальных тестов. Старые CI-планы в архивных записях **superseded**, GitHub Actions не запускать. Не Work/Codex/API; не выключать Happ; не создавать второй tunnel, не удалять старую папку или пользовательские данные. Источник: владелец 29–30.10.2026, git/runtime verification. Историческая передача ниже сохраняется.

## 2026-10-30: v0.2.24 return to ordinary ChatGPT instead of Work
- User reports newly generated tunnel chats are Work. Direct browser inspection of 'Проверка предыдущего ответа' found GPT-6 Astra + Work-style execution/results, whereas v0.2.18 'Приёмка MCP Tunnel' remains normal ChatGPT with High thinking. Global entrypoint origin /mcp-app causes model request target:new in Work host context; normal chat did not.
- v0.2.24 SOURCE ONLY: new chat.bootstrap.open (non-entrypoint, model-visible), ui://chat-bootstrap-v0224 auto-dispatch in ordinary existing /c. Old Global becomes observer-only; Windows activator changed to strict /c/<uuid> and only chatbridge-* heartbeat. Local tests and builds PASS. Need CI release, install, one-time ordinary chat bootstrap and cold /c auto reopen + mode verification.

## 2026-10-30: v0.2.23 automatic Windows Global Hub activation
- User authorized full Yandex Browser close and no special retention of old tunnel chats. DOMKAT v0.2.21 currently installed. Repeated test: browser.exe 0 -> OS deep link -> fresh Global Hub init -> new local message 30OCT-9D41 -> answer ХОЛОДНЫЙ-УСПЕХ in local chat. New follow-up forwarded but remained pending when checked.
- v0.2.22 source added local context restoration; our new v0.2.23 source adds Windows tray ChatHostActivator with per-user URL file, heartbeat/queue monitoring, strict URL validate, 2min cooldown. C# builds, --test-host-activator PASS. Need CI release+install, activate private URL, no-manual-browser send acceptance; then follow-up and reboot.

## 2026-10-29: v0.2.22 local chat context recovery after Global cold start
- v0.2.21 deployed and first fully automated answer proven (browser.exe initially 0; Windows URI starts host, browser hidden; new local chat message eb875... answered АВТОЗАПУСК-21). Second follow-up bdbf... was forwarded by Global into a fresh cloud thread but ChatGPT responded it lacked prior context and did not invoke chat.reply. Local pending/ACK.
- v0.2.22 source prepends bounded local history from answered JSON messages on Global target:new, requires model chat.reply before text. Isolated tests PASS, GitHub CI/release/deploy pending. Next retry second message by deleting only its ui-forwarded ACK with user approval to discard old test chat data, verify continuity. User explicitly authorized closing Yandex and no old history backups.

## 2026-10-29: v0.2.21 Global Hub cold-start and auto-relay experiment
- v0.2.20 cold-browser Global initialization proven after all 35+ Yandex Browser processes closed. Host restored from Windows URI automatically; new ui-initialize in server log. User authorized browser closure, says old tunnel chats disposable.
- v0.2.21 source creates real automatic Global Hub transport in separate ChatGlobalHubApp.html, ui://.../hub-relay-v0221. It discovers queued local chat messages and requests ui/message target:new, ACK, existing chat.ui.open(chat_id) for thread relay. Local mock and MCP contract tests pass. Next CI/install, real cold-browser + local chat message -> model reply.

## 2026-10-29: v0.2.20 Global/Thread host compatibility correction
- v0.2.19 published/installed DOMKAT successfully. Real browser showed a private-tools/no-widgets warning for app-only Global/Thread, and only resource-read events (no iframe init). New v0.2.20 source switches Global/Thread UI visibility to model+app, removes global/thread entrypoint metadata from legacy chat.ui.open and versions resources v0220; tests PASS, pending release.

## 2026-10-29: v0.2.19 Global/Thread lifecycle diagnostic implementation
- v0.2.19 lifecycle instrumentation written and locally tested but not yet committed/released/installed. Two separate app-only entrypoints with distinct UI resources and synthetic iframe diagnostics; existing user message relay remains unchanged. All local tests pass; production v0.2.18. Need CI, deployment, browser Global/Thread and deep-link test.

## 29.10.2026: normative MCP 2026-07-28, MCP Apps and OpenAI extensions re-analysis
- Investigation re-centered on primary technical specifications (not third-party failures). MCP 2026-07-28 stateless server/client differs from MCP Apps iframe 2026-01-26: former has server/discover and per-request _meta, latter still requires ui/initialize/initialized. OpenAI extension adds global/thread entrypoints, deep-link hostContext and ui/message target:new/active. Full technical report: docs/mcp-2026-core-apps-openai-protocol-review-2026-10-29.md.
- Direct, isolated on DOMKAT current server v0.2.18 ModelContextProtocol NuGet 2.2.0: server/discover returned supportedVersions=[2026-07-28], capabilities tools.listChanged=true, resources.listChanged=true, logging; tools/list returned 52 tools using modern protocol. subscriptions/listen for toolsListChanged/resourcesListChanged returned notifications/subscriptions/acknowledged with both filters and subscriptionId=7. Modern resources/read for ui://chatgpt-mcp-tunnel/relay-v0218 returned resultType=complete, mime=text/html;profile=mcp-app and 13803 HTML bytes. Tool chat.ui.open lists global+thread entrypoints, model+app visibility, chat_id schema.
- Design implication: preserve single Tunnel, separate Windows persistent logical chat from MCP network session and MCP Apps View instance; use explicit (computer_id,chat_id,message_id), per-iframe instance leases, context capsule and ui/update-model-context. Modern subscriptions announce actual list/resource changes; to push chat-specific events introduce subscribed resources + resources.subscribe rather than misusing tools/list_changed. The UI View's supported standard MCP subset has tools/call/resources/read, so retain polling fallback for app.
- The model request itself is initiated by ui/message on a live ChatGPT Host; stateful Tasks and MRTR are for server operations/continuation, not a replacement for invoking the Host. Focus next experiments on global/thread entrypoint lifecycle, deep links, full context bootstrap and automatic Host activation after Windows reboot, without Work/Codex/API.

## 2026-10-29: Deep MCP Apps autonomy research, external GitHub implementations
- Investigated official MCP Apps and OpenAI extensions plus real GitHub projects and 2026 Oct issues. Detailed findings saved docs/mcp-apps-autonomy-deep-research-2026-10-29.md. Only repo markdown/context changed; installed DOMKAT v0.2.18 unaffected.
- Important: Global/Thread tool entrypoints with {} launch inside a running Host, deep links only route to global apps and do not guarantee host background inference. Full no-browser / reboot acceptance still unproven, unlike two independent threads already live-tested in ordinary ChatGPT.
- Proposed next action: empirical Global/Thread/deep-link host boot probe and browser-closed first-turn test, followed by local persistent SQLite state and lease-protected relay.

## 2026-10-29: Study of MCP Apps global and thread entrypoints
- Source-verified: global fullscreen navigation and thread-specific content tab are static MCP App entrypoints (_meta[openai/ui].entrypoints); both call tool with {}. A deep link can pass relative route via hostContext[openai/deepLink].url and host-context-changed notifications. ui/message target:new is live-validated in v0.2.18; manual global/thread entrypoints and deep links in ordinary Yandex ChatGPT are to be verified. OpenAI advises separate tools per entrypoint and versioned UI resource caching. Research: docs/mcp-apps-entrypoints-research-2026-10-29.md.

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
- v0.2.17 production on DOMKAT verified (readyz 200, both computers connected, 142 messages retained, watchdog Ready), source multi-chat list/create/switch implemented and tested. Windows CI release success 40924254070.
- Real ChatGPT host after refreshed tools invoked chat.ui.open and returned new mode, but iframe was 0.2.16 cached HTML from unchanged ui://chatgpt-mcp-tunnel/relay URI (confirmed by old status wording; installed HTML was new). No target:new live verification yet.
- v0.2.18 source changes resource to versioned uri ui://chatgpt-mcp-tunnel/relay-v0218; builds and Python MCP contract tests pass. Next commit/publish/update/refresh/real acceptance. Preserve other user files and disabled hourly cloud task.

## 29.10.2026: v0.2.17 multi-chat source implemented, pending Windows CI and real ChatGPT
- v0.2.17 in working tree pending CI: classic WinForms multi-chat UI with create/list/select, local per-chat message mapping and migration-safe legacy chat; MCP Apps ui/message target:new experimental handoff, chat.ui.open(chat_id), per-chat polling and routing.
- Local builds/selftests and preview with two chats on DOMKAT PASS. Preview app PID 11116 and temp directory removed, production DOMKAT v0.2.16 still running. Hourly cloud task disabled as owner requested, Windows watchdog remains.
- Need source commit, CI release, controlled install, browser tool refresh, live target:new / auto-component/chat.reply acceptance. Real browser closed behavior and MKATUSHONOK multi-chat remain untested; two user-owned untracked TXT files remain untouched.

## 29.10.2026: REAL ordinary ChatGPT ui/message acceptance 19:36-19:42 MSK
- 29.10.2026 19:36-19:42 MSK real ordinary ChatGPT user interface and MCP Apps iframe acceptance PASS. New chat exposed 52 tools, chat.ui.open rendered iframe; manual ui/message nonce 58ewppk5 generated and returned exact model turn.
- Started relay once. DOMKAT queue nonce 43830fd3, actual ChatWindow GUI nonce GUI8C42 and background-tab GUI nonce BGTAB42 all caused automatic ChatGPT turns, with chat.reply writing responses and durable ACK. Displayed answer verified visually in local UI.
- Hourly planner not involved with these test timestamps (scheduled at minute 44). Browser tab was restored. No promise for browser closed/suspended or indefinite iframe; future work is lifetime/recovery and UI footer correction. MKATUSHONOK GUI untested.
- Unrelated user TXT files untouched; existing process watchdog and fallback schedule unchanged.

## 29.10.2026: DOMKAT production update 0.2.16 and ChatGPT browser tool refresh
- 29.10.2026 Owner directed DOMKAT install and browser action refresh. Completed v0.2.16.0 live deployment with SHA256 FC05177A63EEC63E28BEE080247ED7B7835A3758098D77D072EFE9253696DBA0. After two Inno code 5 due active processes, succeeded via watchdog pause + agent --shutdown + silent Inno. Current readyz 200, bridge_ping OK, both machines connected, watchdog Ready.
- Authenticated Yandex Browser ChatGPT > Plugins > ChatGPT MCP Tunnel > Manage > 'Обновить инструменты': UI action performed, button disabled then re-enabled with no visible error. Current turn's host catalog may be cached at 45, runtime CI 52; must validate in fresh turn before claiming live new action exposure.
- Remaining: real chat.ui.open and user-click versus automatic ui/message acceptance, local chat.reply. Deployment regression: silent updater currently cannot always shut down active tunnel without controlled preflight.
- No alteration to unrelated files, no changes to tunnel ID, WSS credentials, other computers or other apps.

## 29.10.2026: v0.2.16 stable Windows release verified (not live ChatGPT accepted)
- Stable release v0.2.16 published, GitHub Actions run 40907216014 SUCCESS; Setup/Update assets built, installation/upgrade/uninstallation preserved data. Source a853992; managers now track two-case diagnostic.
- DOMKAT last checked v0.2.13, existing WSS connection stable; ChatGPT MCP tool catalog 45, runtime source 52. Owner-managed update and action refresh required to try actual ui/message in ordinary ChatGPT. No real host test performed or falsely claimed.

## Handoff: ui/message two-case diagnostic (v0.2.16 source)
- Prior 0.2.15 stable Windows build and MCP Apps relay published; working DOMKAT still 0.2.13 and visible plugin catalogue 45 tools. Owner requested continuation after stall.
- Additional v0.2.16 source: direct-click «Тест ui/message» emits test user message, no ack to local queue; existing «Начать передачу» tests automatic timer forwarding. Host capability readout differentiates support claims. JS simulator and .NET MCP tests pass; real ChatGPT iframe behavior not verified, and installed hosts not changed.
- Next stage release/owner update/refresh and real click-versus-timer test. Untracked user TXT files untouched.

## Handoff: verified v0.2.15 release, real host experiment remains
- Source commit 59fea96 published; official Windows release v0.2.15 built successfully by GitHub Actions 40904368593. Installer Setup/Update and preservation tests passed; download update is https://github.com/lvlaksim1/chatgpt-mcp-tunnel/releases/download/v0.2.15/ChatGPT_MCP_Tunnel_Update_v0.2.15.exe. New chat.ui.open/pending/ack and MCP Apps HTML ui/message relay registered; simulated host positive/negative tests passed.
- No production program updated; DOMKAT observed v0.2.13. To prove the intended feature, refresh ChatGPT plugin, open component in ordinary ChatGPT, enable, send local message and confirm host starts turn and chat.reply.
- Keep unrelated untracked files untouched; no independent API, Codex, Work, browser automation or scheduler introduced.

## Handoff — ui/message experiment v0.2.15 source tested, 29.10.2026
- Owner requested a real release based on MCP Apps ui/message, independent of Codex/Work/API/browser automation/scheduler. Code implemented, in main after commit.
- New chat.ui.open MCP tool with MCP Apps resource/entrypoints and UI button «Начать передачу»; chat.ui.pending returns oldest queued message and attached metadata, iframe calls ui/message, chat.ui.ack saves a separate durable acknowledgment only after success.
- Local tests: 52 tools, resource MIME and HTML, simulated host actual JSON-RPC ui/message accepted and denied, zero warnings/errors, original stdio & Router Lite all 23 Windows remote tools/reconnect PASS.
- Not a proof of production host: active ChatGPT embedded component required. User-side manual test in ordinary ChatGPT + refreshed plugin necessary; deployed network/windows apps untouched.

## Handoff after v0.2.14 stable release 29.10.2026
- The new local chat attachments and Ctrl+V screenshot feature is published. GitHub Actions run 40866882778 SUCCESS, full Setup/Update installation/upgrade/uninstall acceptance passed; release v0.2.14 stable, source commit 21a1a0c.
- 49 MCP tools in new runtime, including chat.attachment.read, verified image and UTF-8 text as real tool content; 23 remote Router Lite tools/reconnect preserved.
- Setup SHA256 7ff5afc44db3a535b71e11b6f174141684f666dd0396c303fa03ee64d2d8174d; Update/legacy alias SHA256 ef4899062bde3caecb85fc29d23c03a155ea78d66902d6d49da761bf4f4504ef.
- Installed DOMKAT and MKATUSHONOK remain v0.2.13 because owner updates them personally; their installed GUI has not been tested for new feature. ChatGPT cached tool list 45 needs refreshing.
- Next: owner update and observed physical clipboard/file message; do not declare deployed without evidence.

## Handoff: chat attachments v0.2.14 code accepted, release pending — 29.10.2026
- Owner requested attach-and-send files and Ctrl+V clipboard images in existing ChatGPT MCP Tunnel chat window.
- Implemented GUI selection/drag-drop/clipboard image/preview/removal/file-only sends; queue stores copies with SHA-256; chat.receive includes attachments, chat.attachment.read emits MCP real image or UTF-8 text or verified local binary path.
- Verification: .NET zero warnings/errors, UI attachment test exit 0, tests/test_chat_attachments.py all E2E passed; original MCP 49 tools, Router Lite 23 remote tools and reconnection passed.
- Windows installer release v0.2.14 and real owner GUI test pending. Installed DOMKAT/MKATUSHONOK remain v0.2.13. ChatGPT plugin snapshot 45 needs refresh.
- Two unrelated untracked user text files and production tunnel/security/network unchanged.

## Handoff after physical 325 MB WAN transfer accepted 29.10.2026
- Source MKATUSHONOK C:\Users\m.katushonok\Downloads\MAX.msi; destination DOMKAT C:\Users\Maksim\Downloads\MAX.msi.
- Job 4afa737122d1400dbacfa3e48cd39bee completed, 324820992 bytes, SHA-256 14A528C373B19B35B3C9E77B5FAEE88DC34A073F15D8801673DDB7C040B2D48F; signature valid. Recovered after reconnect at 80.7%.
- DOMKAT v0.2.10 / MKATUSHONOK v0.2.9. Live transfer via same-user control CLI over WSS, no file payload in chat. Plugin still exposes only 41 tools.
- CLI status exit code parsing bug to fix, GUI visual live-check optional. MAX installation not part of this task.


## Direct static WAN architecture implemented and released — 29.10.2026
- Решение владельца 29.10.2026: DOMKAT имеет статический публичный IPv4 на WAN-интерфейсе маршрутизатора; удалённый клиент расположен в другой локальной сети. Развивается прямой защищённый WSS без обязательного Tailscale/посредника. Локальный адрес DOMKAT не заменяется внешним IP. Источник: прямая команда владельца; код основной ветки f8cc66848a475030b63ad5c15e25ec6c68e76308.
- Сервер Kestrel обслуживает защищённый порт TCP 55202 в дополнение к прежнему локальному TCP 55201. Сервер создаёт собственный TLS-сертификат, шифрует пароль PFX через DPAPI Windows, клиент проверяет точный SHA-256 отпечаток до передачи общего ключа. Новый режим требует минимум 32 символа общего ключа. Проверены сборки, 42 инструмента, 23 направляемых инструмента Windows, запрет неверного сертификата, выполнение удалённой команды по WSS, восстановление связи, сохранение старого LAN-подключения; GitHub Actions 40781905752 завершён успешно; v0.2.8 опубликована с установщиками Setup и Update.
- Проверка 29.10.2026 через работающий MCP Tunnel: активный процесс ChatGptMcpAgent.exe PID 14536 имеет FileVersion 0.2.7.0 (запущен 2026-10-29 00:33:36 по системному времени Windows); Router Lite слушает 192.168.0.3:55201 (PID 2992), TCP 55202 не слушает. v0.2.8 не устанавливалась менеджером; bridge_ping успешен. Этот факт supersedes прежний текущий статус v0.2.6, но не меняет вывод о непроверенном межсетевом соединении. На маршрутизаторе не создавался проброс TCP 55202, правила брандмауэра не менялись. Физическая связь с MKATUSHONOK через Интернет и 23 удалённых инструмента остаётся НЕ ДОКАЗАНА. Публичный WAN IPv4 подтверждён чтением роутера; постоянство адреса основывается на словах владельца.
- Next action: owner-controlled v0.2.8 install and WAN rule/forwarding, then physical MKATUSHONOK WSS connectivity and full tool acceptance. Tailscale remains optional fallback, not required.
- Evidence: docs/direct-wan.md, commit f8cc66848a475030b63ad5c15e25ec6c68e76308, Windows Actions 40781905752 success, GitHub Release v0.2.8.

## Verified public WAN on DOMKAT — topology re-evaluation 28.10.2026
- Owner reports a static IP; direct read-only IGD UPnP router RT-GM2-3 verification shows globally routable WAN IPv4 identical to three outside IP service readings. This is much stronger than the Xray/TUN-influenced outbound address; static assignment over time is user assertion. Exact IPv4 withheld from public repo.
- Existing router local IP is 192.168.0.1, DOMKAT 192.168.0.3. Windows Router Lite TCP 55201 bound only LAN, firewall restricted to 192.168.0.0/24. Router replied UPnP 714 for mappings TCP 55201 and 443. Manually configured forwards unknown, and no test was performed from a second network.
- Direct secure WSS remote agents without Tailscale/Headscale/cloud mediator are feasible in principle. DO NOT expose current raw ws:// and client key. Need TLS + agent authentication, explicit safe inbound port mapping, firewall and real external testing.
- Released v0.2.7 Tailscale functionality remains a working development option; Owner has not yet approved replacing that approach with direct WSS, and running installed v0.2.6 remains untouched.
- source: DOMKAT read-only shell and router GetExternalIPAddress/GetSpecificPortMappingEntry, Owner 28.10.2026.


## v0.2.7 Tailscale implementation/release handoff — 28.10.2026 22:45 MSK
- Owner clarified DOMKAT and MKATUSHONOK are in DIFFERENT local networks and then instructed to execute the proposed protected inter-network approach. Chosen Tailscale/WireGuard overlay preserves ONE OpenAI Secure MCP Tunnel and Router Lite.
- Source c04e2ec adds a narrowly scoped per-peer Tailscale Windows Firewall rule (only exact client IP → exact server IP, TCP 55201, runtime executable), validation of local Tailscale adapter, UAC confirmation, automatic server Settings button label and new selftest. No existing LAN feature removed.
- Local Windows selftests and MCP 42-tools/router/reconnect passed, Windows CI run 40756470762 completed success, v0.2.7 stable full/update installers published with SHA256. Physical VPN installation/login and real MKATUSHONOK connection not yet executed or verified.
- Working DOMKAT v0.2.6 was NOT upgraded/restarted. No firewall/IP/Tunnel ID/keys changed during development.
- Next: interactive Windows Tailscale installation/authentication on BOTH PCs, ensure same authorized tailnet, owner-side v0.2.7 in-place update, server listen on own VPN IP and exact peer firewall approval, client own key and physical E2E test.
- source: Owner directive, local runtime tests, GitHub Actions/Release 28.10.2026; authority: owner-directive, verified-runtime, verified-repository.


## Критическая поправка владельца — разные локальные сети (28.10.2026)
- Владелец уточнил: DOMKAT и MKATUSHONOK НЕ находятся в одной локальной сети. Указанный ранее клиентский адрес 192.168.0.3:55201 — лишь частный адрес DOMKAT; инструкции подключения к нему без виртуальной сети или иной маршрутизации ошибочны.
- Один OpenAI MCP Tunnel сохраняется. Для реальной связи между удалёнными агентами нужен отдельный защищённый интернет-маршрут: рекомендуемый к оценке Tailscale/WireGuard, либо WSS-посредник. Решение по установке/транспорту ещё не принято.
- Код v0.2.6 принимает частные VPN-адреса Tailscale для ws:// клиента, но сервер должен слушать правильный интерфейс, а LanFirewall.LocalNetwork не поддерживает 100.64.0.0/10; стандартная кнопка LAN-доступа не подходит для VPN-сценария без адаптации/безопасной настройки.
- На DOMKAT сейчас не обнаружены VPN-службы/адаптеры указанного типа. Нет данных о наличии VPN на MKATUSHONOK. Раздельная сеть — факт со слов владельца; реальные параметры NAT/доступности не проверены.
- Следующий шаг: согласование сетевого транспорта, затем защищённая настройка и испытание реального MKATUSHONOK; не менять работающий Tunnel ID, программу или публичную доступность пока решение не принято.
- source: owner-directive 28.10.2026, read-only проверка DOMKAT и прочитанные исходники v0.2.6.


## Reinstantiation reconciliation — 28.10.2026 22:20 MSK
- Project Manager project-manager restored from generation 16, all 8 coupled Git blob digests matched. PM 2.1.0 and Capsule 1.3.1; both authority branches main.
- Original bridge_ping responded CHATGPT_MCP_TUNNEL_PING_OK. DOMKAT is now running agent FileVersion 0.2.6.0 (PID 30196, started 20:38:25 MSK), so the older v0.2.6 manual-install pending state is superseded.
- Router Lite listens at 192.168.0.3:55201. At this snapshot no established physical client connection was observed; 41 tools visible in ChatGPT against previously advertised 42. Stable release v0.2.6 and CI 40736310056 verified.
- Physical MKATUSHONOK acceptance still NOT PROVEN. Next: approved LAN firewall/key verification followed by real second-PC connection, remote Windows/GUI tests and reconnect. No deployment, secret, IP, tunnel identity or firewall change occurred.
- source: original MCP Tunnel runtime/GitHub tools 28.10.2026; authority: verified-runtime, verified-repository, owner-directive.


This is a compact working view; sealed manager BDI state takes precedence.

## Completed and verified (28.10.2026)

- Project Manager `project-manager` reinstated with stable v2.1.0 and previously published Router Lite decision.
- Router Lite stages 1–3 implemented; all 23 Windows MCP tools now have optional `computer`, while 17 GitHub tools stay global.
- One agent/one router mode in same .NET 8 binary; 42 MCP tools total. Preserved screenshot PNG blocks and original Windows behavior.
- Local DOMKAT tests passed: MCP stdio, two-process remote files/shell/processes/GUI/UIA/PNG, 23 target schemas, parallel requests, bad key, duplicate ID, unknown name, disconnect and reconnect after router restart.
- Passed original Windows, Tk GUI, WPF UIA, WinForms UIA and real-application read-only regressions using an isolated build. Live tunnel unchanged.

## Open commitment

Demonstrate and deploy a real second physical computer over protected network; add simple agent startup/credential handling, then release after full acceptance.

## Next operation

Physical PC onboarding and cross-host tests; do not claim production-ready multi-PC operation based only on DOMKAT tests. See `docs/router-lite.md`.


## Agent installer handoff (28.10.2026)
- Owner accepted MailRu Desktop conventions: full + update Inno Setup, no elevation, data/DPAPI under LocalAppData, tray/autostart, upgrade preserving state and GitHub release updates.
- Added `src/ChatGptMcpAgent` Windows Forms tray host, `installer/agent-full.iss`, `installer/agent-update.iss`, `.github/workflows/agent-installer-release.yml`, `docs/agent-installer.md`.
- Self-contained publish and DPAPI/URL selftest passed on DOMKAT. 42-tool MCP, router and reconnect tests passed on self-contained runtime.
- Next: GitHub Actions Inno Setup build and install/update/uninstall acceptance; publish numeric v0.1.0; second physical Windows PC remains not checked. Do not modify live tunnel before acceptance.

## Agent installer release acceptance
- GitHub Actions 40689966362: success on Windows, both Setup and Update v0.1.0 tested and published as GitHub Release assets.
- Installation, settings preservation during update, uninstall cleanup and agent DPAPI/MCP/router smoke checks passed in CI.
- Next: physical PC onboarding, protected network address, real remote UI/IO tests. Existing tunnel DOMKAT unchanged.
- source: GitHub Actions 40689966362 and GitHub Release v0.1.0; verified 28.10.2026.
- authority: verified-repository, verified-runtime.

## Editable installation folder
- Owner directive: one full installer with selectable install folder, server default D:, client default C:.
- Source implemented and CI tests added for custom directory, update, default paths and uninstall.
- Binary acceptance pending; existing DOMKAT tunnel still running.

## Verified universal installer release v0.2.0
- Successful Windows CI 40696034380 and published GitHub Release v0.2.0 (Setup, Update, legacy update alias).
- Role picker, default server D/client C, editable custom destination, preservation on update, autostart task on/off, clean uninstall were validated by real installer execution in CI.
- DOMKAT still has the previous active tunnel. The owner wants a clean reinstallation and will supply the token in the new server UI; do not prematurely stop the only connection.

## Owner correction 28.10.2026: existing tunnel only
- NO new logical OpenAI cloud tunnel; preserve current Tunnel ID and ChatGPT plugin.
- Replace only local DOMKAT runner, stored auth and profiles; Owner enters new token in new server UI.
- PowerShell 5.1 launcher fixed, x64 GUI selftest passed, staging/cutover scripts authored; live old tunnel unchanged.
- Next: CI release v0.2.1, stage verified installer, then autonomous cutover.

## DOMKAT tunnel is online again (28.10.2026)
- Clean server v0.2.1 installed successfully earlier, with same OpenAI Tunnel ID and new DPAPI token.
- Original failure: 2>&1 on tunnel-client run under PowerShell 5.1 Stop preference promoted normal MCP stderr INFO to terminating NativeCommandError, caused seven-second restarts.
- Direct native tunnel-client call in installed ServerStart.ps1 fixed problem; original ChatGPT bridge_ping and system/GitHub/window tools work, 55198/55201 listen, health/ready 200, 42 MCP tools registered.
- Source fix for v0.2.2 prepared. Next CI release and physical MKATUSHONOK.

## Live stable v0.2.2 confirmed 28.10.2026
- Fixed PowerShell 5.1 native stderr error was released in v0.2.2 (CI 40712500651 success).
- SHA256-verified update installed on DOMKAT without new cloud tunnel, same Tunnel ID, token in DPAPI, server role and autostart intact.
- Inno setup returned exit 0, app version 0.2.2, one tunnel-client and MCP Runtime, Router Lite 192.168.0.3:55201, health/ready 200.
- Original connector successfully calls bridge_ping and system.info; installed runtime local MCP test advertises 42 tools.
- Next MKATUSHONOK real client test; no need to reinstall DOMKAT.

## GUI status/Settings and icon in v0.2.3
- Owner sees generic tray icon, false «Нет соединения» and an inert Settings menu.
- Confirmed hidden Server Settings HWND. Corrected modal ShowDialog/guard to modeless reused Form, exposed named-event --settings, added async server readyz+Router TCP and custom native ICO with colored state dot.
- Local source build and win-x64 --selftest, --test-icons, --probe-server pass. Windows Inno CI and actual installed UI verification pending. Existing DOMKAT tunnel retained.

## v0.2.4 follow-up for actual native Settings visibility
- v0.2.3 Settings menu callback did execute, but its HWND was hidden. Win32 ShowWindow SW_SHOW on live DOMKAT made the Settings window appear; desktop.window.list confirmed correct foreground bounds.
- v0.2.4 patches reusable Form show logic to enforce IsWindowVisible/ShowWindow after Form.Show and when reopened.
- Local self-contained build, selftest, icon test, live read-only server probe all pass. Next Windows CI/release and real DOMKAT --settings acceptance.

## v0.2.5 Windows installer release — user self-updates
- User approved secret-free main Settings mockup for Server/Client. Current v0.2.5 source implements on-demand temporary secret popup, separate token replacement on Server, Server key generate/copy, input-only key on Client, automatic port 55201, keep configured address 192.168.0.3 and existing OpenAI Tunnel ID.
- Tests --selftest, --test-icons, --probe-server, --test-secret-forms passed with self-contained executable.
- User explicitly instructed: prepare full and update installer; do NOT update DOMKAT yourself. Owner will update via built-in UI.
- Next: release Windows GitHub Actions, verify both installer assets and report.

## Release v0.2.5 delivered; user updates DOMKAT
- Stable GitHub Release v0.2.5 and CI run 40732281198 success.
- Full Setup SHA256 57b811467bf4a79a56ae8d8f99965ca36840745b853d357ece29a5a6e105f13a; Update SHA256 278140cb9ee4ed7a1bfcaa500a3b38d27937520185645cc30b6dc75c40ff112b. Release includes third legacy-named update alias to support existing update mechanism.
- Contains accepted server/client secret-free Settings UX; inline buttons open temporary secret popup. Client key has no reveal/copy. IP still 192.168.0.3, port 55201 automatic.
- Crucial: owner requested no remote update; DOMKAT existing installation was left unchanged. Wait for owner self-update acceptance.

## v0.2.6 Windows downloader bug
- User screenshot showed 100% and file-in-use error. Cause: await using var output lasted through File.Move; .part still locked on Windows. Log has Update failed IOException twice; installed DOMKAT v0.2.5.
- v0.2.6 closes stream before rename, validates full length, tests overwriting/partial cleanup/exclusive open, --test-updater-download success.
- Owner self-updates: deliver full and update installers; must manually run v0.2.6 update installer once. Do not execute local install remotely. IP 192.168.0.3 and credentials remain.

## MKATUSHONOK connectivity regression and v0.2.6 fix
- Owner reported «клиент не подключается». Server DOMKAT works: 192.168.0.3:55201 listens, OpenAI readyz 200. Public Wi-Fi Windows Firewall inbound Block, no inbound allow rule. No direct MKATUSHONOK test available; likely firewall but not conclusively proven.
- Server key file updated 20:17:08, so client must use latest copied key; Router Lite returns HTTP 401 wrong key, 409 duplicate machine name.
- v0.2.6 adds server LAN-only firewall permission button with Windows UAC, scope specific runtime, IP, TCP 55201, local subnet. Client shows appropriate WebSocket failures. This and updater file-lock fix tested locally.
- Owner must self-update; remote installed DOMKAT app and firewall remain unchanged.

## Stable v0.2.6 released; client acceptance pending
- Successful GitHub Windows CI 40736310056, source 37702f749cd6b391a4eda4fa7766ed9c3a59a847.
- Full Setup SHA256 66a12e056afa3bf3822e878ded51f8d3bd32f96f13ba12faa94575bf8f17484d, Update and alias SHA256 2053ccadc645acfb0a79d5f7a3f4aef91453da8401db34f13bd97411805ede9b.
- v0.2.6 fixes Windows staging writer file lock, adds server UI LAN firewall UAC scoped to runtime, LAN address, TCP 55201 and local subnet, and visible 401/409/network errors on client.
- DOMKAT up at 192.168.0.3:55201 readyz 200 before release; firewall Public BlockInbound with no allow rule. Client shared secret modified 20:17:08. Physical MKATUSHONOK-side connectivity not checked. Do not claim resolved until actual test.
- User must manually install v0.2.6 Update; never remote-upgrade. Afterwards click server LAN rule button and enter current copied key on client.

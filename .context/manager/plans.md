## v0.2.65 release plan (31.10.2026 20:18 MSK)
Local v0.2.65 installed and live accepted; archive reviewed source to GitHub without credentials, then publish official stable tagged installer if remote/main ancestry allows. Monitor scheduled refresh 3h; do not use prereleases.

---

## Continued VPN roadmap (31.10.2026 19:40 MSK)
v0.2.64 E2E accepted: authorized ChatGPT Plus remains signed-in Happ↔Free and external rollback, official MCP connectivity and Router Lite retained. Continue monitoring uptime and polish user-facing fallback notifications without touching global VPN.

---

## VPN plan after E2E — gen 70 (31.10.2026 18:58 MSK)
Live v0.2.63 path accepted Happ↔Free on DOMKAT; next deliver robust automatic Free failure fallback with browser mode synchronization and in-use Xray hot reload; then full acceptance.

---

## DOMKAT bridge plan (31.10.2026 18:10 MSK)
v0.2.63 installed and successful. Next test owner-authorized authenticated Free app mode, implement automatic Xray node hot swap and health-based Happ fallback/notification, then accept and GitHub archive. Keep local Setup/Update artifacts and backup.

---

## VPN switch plan (31.10.2026 17:08 MSK)
Implemented v0.2.62 UI; next real owner acceptance of both browser modes, verify separate free profile and return without breaking the MCP tunnel; investigate MCP transport and live Xray failover. Local installers available; GitHub only for completed releases, no Actions.

---

## Free node updater next plan (31.10.2026 16:36 MSK)
Scheduled free list refresh is operational every 3 hours on DOMKAT. Next, integrate profile selection with GUI, explicit opt-in, health-based failover of owned Xray child and tested replacement of both WebView2 and MCP client routes while keeping Happ backup. No GitHub Actions; release only after full regression.

---

## Free VPN plan, generation 65 (31.10.2026 14:33 MSK)
Selected free Reality URI validated via Xray and isolated WebView2, source under .tools. Next: explicitly opt-in to trial if requested, health checks and auto-refresh, sandbox isolated traffic and use paid Happ as fallback until a trusted route is ready.

---

## LamaVPN plan (31.10.2026 14:12 MSK)
DPAPI-protected importer+runner built/tested with synthetic profile; next acquire normal LamaVPN VLESS link, execute independent REALITY E2E, verify app transport and UI. Maintain local-first releases and preserve one tunnel.

---

## VPN implementation plan (31.10.2026, 13:30 MSK)
Isolated Xray POC PASS only in documented scope. Next remote secure endpoint, genuine WebView2 & MCP acceptance, UX for profile import and fail-closed proxy selection, release packaging and user approval. GitHub Actions disabled; local development and release only.

---

## План: поколение 62, 31.10.2026, 12:32 MSK
Голосовой выход v0.2.61 прошёл пользовательскую приёмку. Дальше — регрессия/устойчивость озвучивания 2/2 и 10/10 при необходимости, затем реальные тесты голосового ввода при наличии устройства. Не использовать отдельный API, Work или Codex и не менять транспорт без причины.

---

## NEXT PLAN 31.10.2026 — project-manager generation 61
After owner verifies audible reply, run second and tenth sequential auto-TTS reply acceptance, check tab changes and dedup. On failure instrument WebView2 voice_reply -> worker speaking/speech_finished path without logging private contents. Do not touch MCP server/client, do not reinstall for CSS/JS tweaks. When mic physically appears, resume PTT/continuous STT trials.

---

# Manager plans

## АКТУАЛЬНО 31.10.2026 — менеджер проекта, поколение 60
**Текущий план v0.2.60:** 1. Сохранить незакоммиченные реализации в Git. 2. Проверить аудиоустройства/USB2.0 MIC. 3. Выполнить настоящий PTT и непрерывный STT, VAD, TTS динамиков. 4. Полный ChatGPT WebView2 E2E в реальном диалоге, 2/2 затем 10/10, restart, смена вкладки, отсутствие дублей и эха, правильный STOP. 5. Аудит цветовой темы и штатных функций ChatGPT. 6. Не применять --shutdown к усыновлённому транспорту. Общее планирование независимых равноправных узлов и других LLM остаётся backlog. Исследования MCP Apps-only ниже — исторические планы, явным образом superseded.

---


## Актуально 30.10.2026 — подтверждённый E2E и блокер повторного сообщения
Plan after real first E2E: (1) start on stable DOMKAT v0.2.37, check both computers and logs; (2) using isolated experimental branch, trace repeated ui/message request, Host ACK, visible user turn and actual chat.reply, determine why second fails; (3) test official ui/update-model-context or other host-compatible invisible routing, verify actual ChatGPT UI; (4) repeat 2/2 then 10/10 sequential, teardown/reconnect and multi-computer; (5) only then merge/release. For development changed MCP tools/UI URI, use ChatGPT Plugins Manage Refresh; tools/list_changed did not update host snapshot in observed case.

## Приоритетное обновление — 30.10.2026, поколение 58
New experiment staged: (1) preserve main v0.2.37 as installed; (2) read origin/experiment/thread-relay-host-20261030 report and code (commit b7c527e); (3) in ONE new ordinary ChatGPT conversation after plugin/tool schema refresh check chat.thread.relay.probe discovery and uri resource-read; (4) only if isolated relay records ui-initialize, tool-input, relay-begin, relay-poll create/send one harmless canary; (5) record ui/message acceptance and true chat.reply; (6) prove automatic restoration and 10/10; (7) fix false ready state. Never retry historical pending+ui-forwarded messages.


## Verified 2026-10-30 live MCP host experiment
Following 2026-10-30 experiment: 1) Start from installed 0.2.37, source main, keep pending+forwarded older messages untouched. 2) Read experimental report and isolate why chat.ui.open yielded ok but no relay polling. 3) In a safe diagnostic normal-chat thread, test a live initialized Thread iframe sending ui/message from a local queue, with real answer via chat.reply, without DOM or Work. 4) Explore resources/subscribe + notifications/resources/updated as event delivery within an active Host iframe. 5) Upgrade/assess MCP SDK separately for 2026-07-28 MRTR/Tasks; current server negotiated 2025-11-25, tasks/get and subscriptions/listen method-not-found. 6) Test approved Global Deep Link only if it will not route messages through Work; no unverified mode assumptions. After E2E, run 10/10 and multi-PC acceptance.


## Долгосрочная дорожная карта после ближайшей приёмки чата
Фаза A (действующая, P0): восстановление MCP Host-сеанса и настоящий двусторонний локальный чат, 10/10 сообщений и ответов, new/existing, restart, multi-PC без ручных действий.
Фаза B: укрепление общих MCP-инструментов и транспорта Windows, надёжная передача больших файлов/прогресс, управление задачами, явный STOP и подтверждение реального завершения; сохранить один продукт.
Фаза C: переход Router Lite к равноправным автономным узлам без единственного обязательного сервера, с независимым доступом и обнаружением других машин.
Фаза D: независимые бесплатные автономные STT/TTS-модули для голосового общения через общий туннель.
Фаза E: подключение иных MCP-совместимых LLM/агентов, включая локальные Goose/Ollama, без изменения Windows-инструментов.
Сквозные требования каждой фазы: безопасность локальных машин, защита соединений, Windows 10, VPN Happ не выключать, в перспективе убрать зависимость от Happ, без платного model API/Work/Codex для чата; локальные сборки, GitHub Actions выключены. Это приоритеты, а не разрешение безусловно начинать все фазы одновременно.


## ПРИОРИТЕТНЫЙ ПЛАН С 30.10.2026 (замещает старые версии ниже)
1. Перед действиями подтвердить установленную v0.2.37, bridge_ping, Router Lite DOMKAT/MKATUSHONOK, Git status, очереди/ACK. Не переустанавливать программу просто при восстановлении капсулы.
2. Исследовать Host lifecycle и способы безручного запуска MCP Apps из обычного ChatGPT; проверить, что вызывает iframe, сохраняет ли ChatGPT его после ответа и как допустимо инициировать новое сообщение. Учитывать host-контекст при target:new (исключить Work).
3. Получить фактический MCP round-trip без DOM: ui/message → генерация ответа моделью → chat.reply на правильный chat_id. Прямой ручной вызов chat.reply не считать автоматическим тестом.
4. Затем проверить 10/10 сообщений, новый/существующий чат, обрыв, перезапуск и MKATUSHONOK; исключить дубли и не потерять attachments/старую очередь.
5. После успешной сквозной приёмки собрать и проверить релиз локально на DOMKAT, опубликовать в GitHub без Actions; безопасно обновить с контрольными хешами и откатом.
6. При невозможности host-активации зафиксировать воспроизводимый результат и оставаться в статусе НЕ ПРИНЯТО.


## 30 Oct 2026: next engineering plan
1. Do not reinstall solely from context restore. DOMKAT is on v0.2.33; v0.2.35 published and downloadable.
2. After authorized installation, compare installed ProductVersion, bridge_ping, agent/router processes and run new/existing chat two-turn E2E, visually confirm normal ChatGPT not Work.
3. If mode, ACK, binding, or delivery fails, retain local pending messages; record sanitized host results before patching. Only then consider automatic host activation.
4. Continue local-first development and GitHub storage/release only, Actions off.


## 2026-10-30 05:25 MSK: stable v0.2.18 relay restoration
First validate compiled local v0.2.30 candidate, restore only the proven v0.2.18 manual chat.ui.open relay, isolate experimental Hub, and preserve pending messages. Next install under owner authorization and verify two consecutive local messages and replies in ordinary ChatGPT, then multichat. Keep auto-start separate.


## 30.10.2026 — запланированное улучшение идентификации версии
- Для следующей версии добавить читаемую метку **установленной версии ChatGPT MCP Tunnel** в окно уведомления/индикатор управления, рядом с названием программы или заголовком. Версия должна соответствовать реально запущенному агенту, а не последнему тегу GitHub. Сохранить компактность индикатора, кнопки STOP и отображение исполнителя.
- Не выполнять сейчас сборку/установку только ради этой заметки. При следующем утверждённом функциональном цикле включить в план, локальные тесты UI, контроль версии и регрессию.

## 2026-10-30 04:14 MSK — current routing plan
1. Source baseline: DOMKAT `D:\Dev\ChatGPT-MCP-Tunnel`, `main`, commit `e80e3ea`; local candidate release `v0.2.29` is built, hashed, uploaded as GitHub pre-release, not installed on DOMKAT. No further CI/Actions.
2. When owner has manually installed v0.2.29, refresh MCP tool schema if needed. Open existing **ordinary** `/c/` ChatGPT thread and invoke `chat.bootstrap.open`, never `chat.hub.open` for sending. If host has `openai/message` but no deep link (observed), click the one-time normal-chat confirmation in the embedded component.
3. Use exactly one controlled local message for end-to-end acceptance, inspect actual ChatGPT thread/mode, ensure reply via `chat.reply`. Four existing pending messages must not be replayed blindly; preserve queue and coordinate safe test conditions. If candidate fails, leave queued messages intact and capture sanitized lifecycle diagnostics.
4. Future improvements only after verified failure analysis; use local batch scripts with fail-fast and meaningful summaries, ~1s GUI mutations, no forced CLI delay. GitHub remains source/release storage only.

## План менеджера 30.10.2026, 03:46 MSK
1. Не собирать/не переустанавливать `v0.2.28`: релиз опубликован, установленный на DOMKAT агент уже `0.2.28+ad28e28`; владелец устанавливает обновления самостоятельно, если отдельно не распорядится иначе.
2. При следующей пользовательской приёмке запросить/вызвать инструменты со схемой `executor` и проверить на реальном DOMKAT отображение имён для manager/supervisor и дефолт ChatGPT; убедиться, что параллельные чаты не наследуют чужую роль. Не утверждать верификацию Host identity, поскольку параметр self-declared.
3. Для дальнейших задач разрабатывать и тестировать **только локально** `D:\Dev\ChatGPT-MCP-Tunnel` укрупнёнными fail-fast PowerShell/Python сценариями; GUI ~1 с между действиями, CLI без искусственных пауз. GitHub только история/готовые Releases; Actions не запускать.
4. Следующие отдельные изменения — достоверные состояния индикатора «работаю / ожидаю модель / завершено», улучшение отмены фоновых операций; только по утверждённому поручению. Не переводить чат в Work/Codex/API и не выключать Happ.
5. Постоянный менеджер: `project-manager`. Исправление прежнего ошибочного слова «супервизор» не создаёт новый агент/контекст.

## 30.10.2026 — executor attribution implementation
1. Add per-tool optional \`executor\` metadata with default ChatGPT; normalize to display-only safe identifiers.
2. Propagate over Router Lite, GUI batch nested calls and file transfer wire operations; display with active/recent event in overlay.
3. Verify WinForms actual HWND + displayed label; tests for default/nonpersonalized, project-manager/supervisor, remote forwarding; preserve STOP and data secrecy.
4. Build release locally on DOMKAT; push source and direct-release upload only, NEVER GitHub Actions. No production update without appropriately authorized controlled installation.

## Рабочий план от 30.10.2026 (актуальнее устаревших CI-планов)
1. Продолжить незакоммиченные исправления Unicode v0.2.28, проверить все точные русские тексты, убрать CS0162. Одним локальным сценарием выполнить анализ → патч → `dotnet build` → WinForms/MCP тесты → краткий итог.
2. При прямом поручении на выпуск: только локальный `scripts/Build-LocalRelease.ps1 -Version 0.2.28`, SHA-256, Git push исходников, прямой GitHub Release upload; без GitHub Actions.
3. Следующим отдельным техническим этапом разработать достоверную индикацию «действия / ожидание решения модели / завершение задания», соблюдая локальный STOP; не приравнивать временное отсутствие команды к завершению.
4. Прежние ChatGPT/MCP Apps задачи не считать закрытыми без живого подтверждения обычного режима, чата и восстановлений. Не переводить в Work/Codex/API, не выключать VPN Happ.
5. Не стирать старую рабочую папку, установленную программу и существующие незакоммиченные файлы. Следовать `.context/decisions/operating-model-2026-10-30.md`.

## 2026-10-30: v0.2.24 return to ordinary ChatGPT instead of Work
1. DONE investigation: visible Work/Astra UI and results in newest chats, normal Chat in v0.2.18. Official ui/message target:new has no independent mode selector.
2. DONE code v0.2.24: chat.bootstrap.open normal-thread only (non-global), Global Hub observer no pending/ui/message, separate versioned resources; activator requires /c/<uuid>, chatbridge-only heartbeat. Local builds and tests PASS.
3. NEXT Windows release v0.2.24 and verified installer; refresh plugin tools in ChatGPT. One-time invoke chat.bootstrap.open from normal /c conversation; record its URL to local settings; close browser entirely and submit local message.
4. NEXT confirm auto browser /c activation, chatbridge heartbeat, target:new creates a normal ChatGPT mode thread, actual chat.reply and continued replies, no Work/Astra. If archived normal /c does not reload iframe, investigate explicit bootstrap path without falling back to global Work.

## 2026-10-30: v0.2.23 automatic Windows Global Hub activation
1. DONE: v0.2.21 Global Hub from entirely closed Yandex, new local chat and real answer ХОЛОДНЫЙ-УСПЕХ end-to-end.
2. DONE: v0.2.23 source ChatHostActivator, C# builds + isolated selftest. Implemented server-only tray timer polling every 12s, pending-unforwarded check, hub heartbeat TTL, 120s launch throttle, strict per-user ChatGPT URL.
3. NEXT: CI release v0.2.23 (including committed v0.2.22 context restoration), verified installer DOMKAT; create local per-user hub-entrypoint.url using observed installation identifier and start actual test with browser processes zero.
4. NEXT: without opening browser manually, send a fresh local chat message; check activator log, Hub ui-initialize, message forwarded and local answered. Check follow-up memory; repair missing chat.reply separately if persists.
5. NEXT: supervised Windows reboot and testing of independent remote computers, no extra tunnels or scheduled ChatGPT automation.

## 2026-10-29: v0.2.22 local chat context recovery after Global cold start
1. DONE v0.2.21 cold browser no-process start and automatic first local chat model reply via hidden browser. Previous answer АВТОЗАПУСК-21 stored with chat.reply.
2. OBSERVED second message forwarded into a new cloud thread after stale route, cloud model lacked prior answer and did not invoke chat.reply, so local stays pending with ui-forwarded ACK. Need server-side context recovery.
3. DONE v0.2.22 source: bounded history field on Pending(discover), Global target:new prompt adds previous local transcript and explicit chat.reply; isolated tests pass. NEXT CI release/install.
4. NEXT repeat existing second pending message after removing ONLY its ui-forwarded file (user says old test chats disposable), observe ChatGPT response АВТОЗАПУСК-21 and chat.reply Status=answered. Then sustained hidden / reboot.
5. NEXT single-owner Global lease, retry/reconcile cloud responses, integrated Windows Host Activator. No Work/Codex/API/Sign in.

## 2026-10-29: v0.2.21 Global Hub cold-start and auto-relay experiment
1. DONE Global and Thread actual iframe lifecycle on v0.2.20; global works in fullscreen, thread inline; deep link from Windows relaunches global with fresh instance. Browser hidden tab continues heartbeat.
2. DONE full cold browser closure test: browser.exe=0, Global Windows URI starts Yandex and a brand-new authenticated ChatGPT MCP App (proof in event log).
3. SOURCE v0.2.21: separate Global Hub HTML auto-polls unbound local chats and initiates target:new, ack, no manual button. Local C#/Python/JS test pass. NEXT CI release and controlled DOMKAT install.
4. NEXT repeat cold browser with v0.2.21; create Windows local chat and send unique request without touching browser. Verify new ChatGPT thread, hub-forwarded ACK and real chat.reply. Then follow-up and minimized browser.
5. NEXT robust owner lease for multiple Global instances, Windows Agent automatic host link startup, local context capsules and reboot test. Old test chats disposable.

## 2026-10-29: v0.2.20 Global/Thread host compatibility correction
1. DONE: v0.2.19 CI+installer+controlled deployment and tool refresh; both ui resources fetched, private-tool widget warning observed.
2. DONE source fix v0.2.20 visibility=model+app, global/thread exclusivity, new versioned ui:// resources; isolated tests PASS.
3. NEXT v0.2.20 CI, verified install DOMKAT, refresh plugin, open global, verify tool-open + handshake + heartbeat; test thread tab and browser deep link.
4. NEXT close test ChatGPT tab, check teardown; then OS-initiated global app activation and actual model reply when browser is not visible. Preserve other user tabs.

## 2026-10-29: v0.2.19 Global/Thread lifecycle diagnostic implementation
1. DONE: implement lifecycle probe Global-only + Thread-only tools, isolated HTML, technical event journal, tests. Build and old protocol tests pass.
2. NEXT: Windows CI v0.2.19 full/update installers, data preservation, controlled deployment to DOMKAT and refresh plugin tool catalog.
3. NEXT: manually activate Global from navigation and Thread from chat tab; verify real events and host capabilities, no model prompts.
4. NEXT: deep link route test with authentic plugin ID; controlled browser-closed Host Activator test. Avoid force-killing user's browser unsaved sessions.
5. NEXT: only after host actual response prove controlled reboot, local chat state recovery, then implement persistent context snapshots/leases.

## 29.10.2026: normative MCP 2026-07-28, MCP Apps and OpenAI extensions re-analysis
1. DONE: primary 2026-07-28 MCP + MCP Apps 2026-01-26 + OpenAI extension normative research, with three direct read-only protocol probes on DOMKAT; 52 modern tools, subscription ack, MCP Apps resource metadata and HTML verified.
2. NEXT: test Global entrypoint and Thread entrypoint independently in ordinary ChatGPT; record exact ui/initialize hostCapabilities/hostContext and static args {}. Preserve v0.2.18.
3. NEXT: test app-side ui/update-model-context context capsule and target:new bootstrap, then deep-link route and Host autostart after reboot using the existing Windows Messenger GUI only.
4. ARCH: persistent local chat DB/context, explicit server chat handles, events/notifications and per-chat iframe lease; no second MCP Tunnel, hourly planner, Work/Codex/API.

## 2026-10-29: Deep MCP Apps autonomy research, external GitHub implementations
1. DOCUMENTATION DONE: deep review of official MCP Apps/OpenAI extensions + 6 real GitHub designs/issues; report in docs/mcp-apps-autonomy-deep-research-2026-10-29.md; no production changes.
2. NEXT: isolated tests of Global/Thread entrypoints and deep links in ordinary ChatGPT/Yandex on DOMKAT; record hostContext, actual tools/call and whether global has its own chat.
3. NEXT: decisive browser-closed/reboot bootstrapping experiment—queue local message, close all normal ChatGPT host processes, try OS level global/deep-link activation, verify real ui/initialize AND model response, not just page launch. Do not conflate background UI with independent model computation.
4. Architecture: local SQLite event-sourced chat state, context capsule, MCP route manager, global hub + thread relay, atomic per-chat leases and message idempotency. Keep one tunnel, ordinary ChatGPT subscription path, no API/Codex/Work or hourly task.

## 2026-10-29: Study of MCP Apps global and thread entrypoints
- NEXT: explore global sidebar entrypoint, thread tab and deep links in ordinary ChatGPT (non-destructive). Then split chat.ui.hub global and chat.ui.thread per-conversation from chat.ui.open bootstrap; add hostContext route processing and iframe ownership leases, preserving single tunnel.

## 29.10.2026: two distinct live ChatGPT threads and switch-back PASS
- New second local session chat_id 58f8d4f5ab3c4fea88382a4ab029967f received its own new ChatGPT thread via ui/message target:new, route waiting -> ready, and local message 477156c8b341421ebb5866f0bc4188d5 returned Status=answered, Reply=ВТОРОЙ.
- Switched GUI back to first local session chat_id c8d881eb8f0f4ff1aac24dcfe21a0a0c. Message 755051abb067448a9ff138bd2d082371 was forwarded and answered Reply=ПЕРВЫЙ in that same first session. Previous messages ПРИНЯТО and ПРОДОЛЖЕНИЕ remain visible only there. Thus 2 independent new ChatGPT threads, 2 associated local chat histories and switch-back have live end-to-end PASS.
- Browser open, ordinary ChatGPT MCP Apps, no Work/Codex/API or hourly cloud planner. Browser-closed recovery and MKATUSHONOK own local GUI not tested.

## 29.10.2026: v0.2.18 real ordinary ChatGPT multi-chat acceptance PASSED
1. DONE v0.2.18 CI, install, UI cache fix and live normal ChatGPT ui/message target:new test with two successive answered messages and isolated chat histories.
2. NEXT test multiple live chats interleaved with independent ChatGPT thread contexts and auto iframe startup. Prioritize initialization robustness and duplicate forwarding prevention.
3. NEXT native installed tray ShowChat test, then MKATUSHONOK local chats and per-computer routing.
4. NEXT browser lifecycle recovery without hourly automation; primary/standby iframe leases and reattachment.

## 29.10.2026: v0.2.18 MCP Apps cache-bust after live 0.2.17 test
1. DONE v0.2.17 CI and DOMKAT install; no data loss, alive and bridged. Real new-ChatGPT session successfully invoked chat.ui.open, but browser rendered stale HTML (0.2.16) despite updated server.
2. DONE source v0.2.18 versioned MCP Apps UI resource URI and tests; local builds and protocol tests clean.
3. NEXT v0.2.18 CI release and controlled update on DOMKAT, browser refresh, new ChatGPT session and live target:new via new local chat.
4. AFTER real handoff verify chat.ui.open(chat_id) auto-start, reply to correct local chat, context preservation when switching and no cross-chat mix. Keep hourly cloud scheduler disabled.

## 29.10.2026: v0.2.17 multi-chat source implemented, pending Windows CI and real ChatGPT
1. DONE: ChatSessionStore per-local-computer session index; backward-compatible legacy chat; WinForms New Chat, list, session selection, isolated drafts and history, last MCP reply display.
2. DONE: ChatId in chat.receive and chat.ui.pending, ui/message target:new with chat.ui.open(chat_id) bootstrap, per-session heartbeat metadata and routing, legacy widget compatibility.
3. DONE: local builds, isolated UI selftest, browser protocol simulators and live WinForms preview on DOMKAT with two chats.
4. NEXT: Windows GitHub Actions v0.2.17 full/update builds/installer acceptance and code signing/hash verification; do not disrupt active 0.2.16 until verified.
5. NEXT: DOMKAT update with chat history preservation, refresh MCP tools in authenticated ChatGPT browser, real target:new cloud conversation -> new iframe -> chat.reply test; test two-way switching and file attachments.
6. LATER: MKATUSHONOK local GUI and cross-computer chat routing, concurrent iframe ownership/duplicate claim, closed-browser recovery.

## 29.10.2026: REAL ordinary ChatGPT ui/message acceptance 19:36-19:42 MSK
1. DONE: ordinary ChatGPT 52 tools; chat.ui.open renders real MCP Apps iframe.
2. DONE: manual nonce 58ewppk5, synthetic queue 43830fd3, GUI GUI8C42 and inactive-tab GUI BGTAB42; all generated ChatGPT model replies, and queued/GUI messages were saved via chat.reply.
3. NEXT: fix WinForms ChatWindow footer to reflect ui/message replies, rather than relying only on legacy cycle-status.json.
4. NEXT: test browser minimized/closed/reopened, discarded tab, long lifetime, sleep/reconnect and automatic recovery WITHOUT browser automation or an API.
5. NEXT: verify MKATUSHONOK GUI, then evaluate disabling hourly fallback task with owner.

## 29.10.2026: DOMKAT production update 0.2.16 and ChatGPT browser tool refresh
1. DONE: Owner-authorized DOMKAT update v0.2.13 -> v0.2.16; SHA-256 validated; manual controlled stop/restart overcame Inno RestartManager failure.
2. DONE: ChatGPT browser: Plugins > ChatGPT MCP Tunnel > Manage > 'Обновить инструменты'; UI returned to ready, no visible error.
3. NEXT: fresh ChatGPT tool discovery confirms chat.ui.open, chat.ui.pending, chat.ui.ack and count 52. Invoke chat.ui.open, test direct-click and automatic ui/message, verify chat.reply in local window.
4. NEXT: fix Inno update pre-install coordination to pause watchdog and gracefully shut agent before replacing binaries; regression test from running install.

## 29.10.2026: v0.2.16 stable Windows release verified (not live ChatGPT accepted)
- Complete: stable v0.2.16 Windows Setup+Update verified in Actions 40907216014. Next is owner-controlled install, refreshed MCP tools and real click-vs-timer ui/message test, then chat.reply local GUI confirmation.

## v0.2.16 diagnostic two-case MCP acceptance
1. Done: add explicit-click ui/message test which does not touch the local queue; show hostCapabilities message.text/openai/message. Simulator validates click success/failure with no queue ack.
2. Done: local .NET builds zero warnings/errors; MCP resource and tool metadata tests pass.
3. Next: publish stable Windows installer v0.2.16, verify CI and hashes.
4. Next: owner-controlled update on DOMKAT, plugin tool refresh; test click and timer separately, then real chat.reply. Neither mode requires paid API, browser automation, Work, Codex or ChatGPT chat scheduler.

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

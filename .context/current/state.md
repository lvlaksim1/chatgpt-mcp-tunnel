## АКТУАЛЬНО 31.10.2026 — менеджер проекта, поколение 60
Источник: прямое поручение владельца 31.10.2026; проверка MCP bridge_ping, версии установленного GUI, PID, локальных моделей, журнала развёртывания, результат однократной автоматической задачи восстановления.

**Подтверждено:** DOMKAT, GUI ChatGPT MCP Tunnel **v0.2.60.0**, PID 27916; MCP runtime оставлен **v0.2.58.0** намеренно; tunnel-client.exe PID 24884, ChatGptMcpTunnel.exe PID 28372, bridge_ping=OK. Журнал .tools/deploy-v0260-status.json: phase=verified, 31.10 06:37 MSK, GUI SHA-256 62B17A3DDBD0380219172797DEC68074DC1F5BE2F5B37E8CD8FA5F31BA1EF84D. Установщики v0.2.60 Setup/Update собраны локально, проверены SHA. Backup v0.2.59 сохранён в .tools/deployment-backups/pre-v0260-safe-20261031 и pre-v0260-v0259. Встроенный профиль WebView2 не изменялся. Задача автоматического мониторинга после успеха отключена.

**Реализовано:** основной экран — настоящий ChatGPT внутри WebView2, вкладки/адресная строка/тёплая тёмная тема; никакого отдельного RichTextBox чата. MCP остаётся транспортом для Windows-инструментов. Голосовой worker ChatGptMcpVoice: локальный Sherpa-ONNX **1.13.8** (2.0 несовместим с данной Piper TTS), русская Zipformer ASR sherpa-onnx-zipformer-ru-2024-09-18, Silero VAD, русская Piper Irina TTS, NAudio. Микрофон: удержание кнопки и непрерывный режим; пауза 300–1500 мс, default 700; автоотправка по умолчанию OFF, автоозвучивание отдельно. Голос в штатный composer выбранной WebView2 вкладки; ответ для TTS через DOM текущей вкладки.

**Доказанные проверки:** русский автономный TTS WAV затем ASR восстановил текст; изолированный WebView2 mock DOM проверил вставку текста без автоматической отправки и один ответ без дублирования; сборка и packaged selftests PASS; установленные модели --check-models Ready/Stt/Vad/Tts=true. **Не проверено и не принято:** живой голосовой E2E в обычной беседе, физический микрофон USB2.0 MIC (статус Windows Unknown), воспроизведение динамиками с участием пользователя, 2/2 и 10/10 диалогов, barge-in/эхо/смена вкладок/реальный restart для голоса. Тесты на синтетическом аудио не заменяют живую приёмку.

**Исходники:** D:/Dev/ChatGPT-MCP-Tunnel, feature/webview2-primary-ui-v0248 HEAD 33d114b, все изменения WebView2+голоса v0.2.60 НЕ ЗАКОММИЧЕНЫ. Сохранять их без reset, затем прогнать регрессию и сохранить в GitHub. GitHub Actions не применять. Семь стратегических целей проекта остаются; равноправные узлы, другие LLM и автономность Happ — backlog.

**Опасная процедура:** прежний вызов ChatGptMcpAgent.exe --shutdown при принятом собственном tunnel-client оборвал MCP. После этого безопасная установка закрывала только GUI по PID, оставив те же PID клиента и runtime. Для следующих обновлений запрещено использовать старый --shutdown пока он может останавливать транспорт; обязательны backup, PID check и rollback.

**SUPERSEDED:** ниже исторические записи о текущей v0.2.37, WebView2 только для авторизации и MCP Apps как единственном пользовательском чате. Владелец выбрал настоящий ChatGPT WebView2, он единственный интерфейс переписки. Экспериментальные MCP Apps выводы 30 октября — архив, а не текущий план.

---

## Актуально 30.10.2026 — подтверждённый E2E и блокер повторного сообщения
2026-10-30 current: temporarily installed experimental v0.2.41, discovered and invoked ChatGPT Plugins Manage Обновить инструменты, then in second fresh ordinary ChatGPT chat verified new tool, resource-read, bound relay-poll, one true answered user message. Second ACKed message remained pending with no reply. Technical route was visible in ChatGPT user message despite assistant-only annotation. Experimental code/report saved to origin/experiment/thread-relay-host-20261030 commit 0f6af99. Returned DOMKAT to stable agent/runtime v0.2.37+45d3289 with matching SHA256 to original local publish and successful stable MCP tests; both DOMKAT and MKATUSHONOK connected, ping OK. Extra experimental HTML and canary-selection file archived to .tools/experiments/thread-relay-20261030/archived-after-rollback. Refreshed plugin tools again after rollback; page temporarily failed to load but Ctrl+R recovered. Fresh-chat validation after rollback is UNVERIFIED due platform safety blocking further UI read; do not claim tool snapshot confirmed.

## Приоритетное обновление — 30.10.2026, поколение 58
30.10.2026 18:29+03 phase: experiments v0.2.39/40/41 performed, now installed DOMKAT returned to stable 0.2.37 (verified both EXEs), both DOMKAT and MKATUSHONOK connected. Source main clean before capsule; separate experiment branch origin/experiment/thread-relay-host-20261030 at b7c527e holds new MCP Thread relay, tests and report. In existing ChatGPT thread new resourceUri was returned by tool but cached old HTML ran. Sending tools/list_changed did not visibly refresh models' tools nor load new UI. Full automatic text chat not accepted; no queued private messages touched. One-time new normal conversation experiment remains necessary.

## Verified 2026-10-30 live MCP host experiment
2026-10-30 experiment confirmed: diagnostic v0.2.38 was built locally, installed temporarily, real ChatGPT Thread iframe returned fullscreen for both pip requests (14:34:22 and 14:37:16 UTC). Initial host declared inline/fullscreen; hidden iframe logged heartbeat. One test local message was queued and chat.ui.open returned ok; no forwarding ACK, no reply and no fresh route polling after almost 1 minute. The undelivered test-owned record was archived out of live queue, no original messages changed. Current installed DOMKAT RETURNED to stable v0.2.37 after verified rollback SUCCESS; both agent/runtime 0.2.37, MCP ping OK, DOMKAT+MKATUSHONOK connected. Main source clean and unchanged except this capsule; experimental source/report lives at origin/experiment/mcp-host-pip-20261030 commit de43821.

## Подтверждение миссии владельцем — 30.10.2026
Владелец подтвердил все семь стратегических направлений: MCP-управление Windows; много компьютеров и файлы; независимые равноправные узлы; полноценный автоматический текстовый чат; собственные бесплатные голосовые STT/TTS; независимость от конкретной LLM с поддержкой Goose/Ollama; безопасность, автономность и минимальные внешние зависимости. Главное предназначение — единая среда взаимодействия человека, ИИ и компьютеров. Это зафиксировано как принятый scope, а НЕ как реализованный функционал. Реальная установка DOMKAT по-прежнему v0.2.37, основной блокер — полностью автоматический MCP-чат.

## ПРИОРИТЕТНЫЙ СТАТУС 30.10.2026, 16:43 МСК — v0.2.37
- DOMKAT агент и MCP Runtime имеют версию 0.2.37+45d328923502b7770f828f0c2b27fae9f999601f. bridge_ping=OK; подключены DOMKAT и MKATUSHONOK. Git main, исходный код v0.2.37 — 45d3289, предыдущий контекстовый коммит dec5430. GitHub v0.2.37 pre-release: Setup/Update/SHA256. Резервная копия v0.2.36 имеется в .tools/local-release/v0.2.37/deployment/backup-before-v0237.
- Работают базовые компоненты: локальные чаты, сообщения/вложения и JSON-очереди, ack/idempotency, привязки чат-ID, WinForms WebView2 auth-only, MCP Apps HTML relay-v0237, ui/message с target active/new, модельная маршрутизация chat.reply.
- Node/CDP браузерный отправитель удалён из установленного пакета. WebView2 не используется как транспорт сообщений, реальный постоянный вход на аккаунт не подтвержден.
- Тесты MCP и UI на моделируемом Host прошли; настоящая цепочка «локальное сообщение → обычный ChatGPT → ответ в локальном окне» не прошла приёмку; устойчивые 10/10, восстановление host iframe, многокомпьютерный чат — открыты.
- На момент проверки осталось 2 старых сообщения status=pending, каждое ui-forwarded=True; не поворачивать автоматически на повтор. Новая актуализация = capsule generation 55; старые статусы ниже архивные.

## 30.10.2026 — v0.2.37 (MCP-only + WebView2 auth)
2026-10-30 16:33 MSK: v0.2.37 installed DOMKAT, agent/runtime version 0.2.37+45d3289, GitHub prerelease v0.2.37 (setup/update/SHA256) published, origin/main source commit 45d3289. Independent offline installer SUCCESS; backup v0.2.36 retained. MCP ping OK and DOMKAT/MKATUSHONOK connected. Node.js/CDP browser transport removed and old packaged files deleted; WebView2 auth-only form compiled, packaged preview started with persisted profile. MCP Apps iframe tool called from this normal ChatGPT conversation and embedded UI displayed. Model messages via ui/message with assistant-only routing annotations in source; actual two-way host acceptance not yet verified.

## 30 October 2026: automatic browser transport preview
2026-10-30 MSK: PREVIEW v0.2.36 installed on DOMKAT and GitHub prerelease published. Source commit 7367ae5. Agent, MCP runtime, tunnel-client, bundled Node background worker running. Browser transport profile opened at chatgpt.com using localhost-only CDP. Browser worker heartbeat reports waiting_login (new isolated profile not authenticated). Two MCP computers connected. Rollback snapshot v0.2.35 retained. No real chat E2E accepted yet.

# Current state

## 2026-10-30 MSK: stable GitHub v0.2.35 release
PUBLISHED: git commit 729838e, branch main, GitHub release v0.2.35 (full and update EXE, verified SHA256 manifest). Packaged dotnet, WinForms, MCP, Unicode, new/existing chat route, retry/ACK tests PASS. Source includes explicit new-cloud session marker, atomic discovery lease, shortened visible return marker, encoding fixes, overlay close fix and version label. LIVE ChatGPT UI E2E pending. DOMKAT runtime v0.2.33 deliberately untouched.


## 2026-10-30 05:25 MSK: stable v0.2.18 relay restoration
LOCAL v0.2.30 stable-mode candidate compiled and packaged on 30 October 2026. The accepted v0.2.18 ChatRelayApp.html is source-equivalent to Git eb1845f. Concurrent bootstrap dispatch and tray host activation were disabled. Manual chat.ui.open is the sole sender; the installed DOMKAT remains v0.2.29 until an update. Ten regression suites and packaged tests passed. See docs/STABLE-CHAT-RESTORE-2026-10-30.md.


## ACTUAL STATE — 2026-10-30 04:14 MSK — chat mode restoration
- Owner asked to restore automatic opening of **ordinary ChatGPT** dialogs from local tunnel chat instead of Work/Codex. Work-regression root cause remains partially inferred from transport origin, and no new live dialog has yet been accepted as ordinary.
- On DOMKAT source Git HEAD `e80e3ea13f6eeaaa7611d9a065201f4edaba28ca`; local suite passed: both .NET builds, Windows MCP/Router Lite, lifecycle resources, UIA, old ordinary bootstrap positive test, negative Work/extension-missing test, Global non-dispatch test and tray selftests. Pre-release v0.2.29 built via local Inno and uploaded directly with 4 assets; GitHub Actions not used.
- Real ChatGPT thread diagnostic today reported `hasOpenAiMessage=true` but `hasDeepLink=false` (in contrast to a previous diagnostic). Candidate supports a **user-confirmed** bootstrap in an existing ordinary chat when host context link is absent; once confirmed, forwarding is automatic while context remains stable.
- DOMKAT live installed agent version **0.2.28+ad28e28** at 04:13 MSK; v0.2.29 not installed automatically. Local message queue had **four pending** old messages, preserved untouched. Live visual test of newly created ChatGPT thread and reply path remains OPEN.
- Details: `docs/ORDINARY-CHAT-ROUTING-2026-10-30.md`, GitHub pre-release `v0.2.29`. Older state describing 0.2.27 as installed is archival.

## ТЕКУЩЕЕ СОСТОЯНИЕ 30.10.2026, 03:46 MSK — сверено на DOMKAT
- `manager_id=project-manager`, manager version `2.1.0` — постоянный менеджер проекта, **не супервизор**. В этом диалоге пользователь обращается к менеджеру; предыдущее заявление ассистента о «капсуле супервизора» признано ошибкой. Новую identity/capsule не создавать.
- Основной путь `D:\Dev\ChatGPT-MCP-Tunnel`; Git `main`, HEAD `ad28e28218a404a645f0df805f8d4f205fd2e079`, чистое рабочее дерево на момент сверки. Рабочий установленный путь `D:\ChatGPTMCPAgent`; версия исполняемого файла `0.2.28+ad28e28218a404a645f0df805f8d4f205fd2e079`. Обновление v0.2.28 пользователь выполнил сам.
- `v0.2.28` локально собрана и протестирована с Unicode-исправлениями и `ControlExecutor`, опубликована на GitHub Releases прямой загрузкой; установщики и SHA256 сохранены в `.tools/local-release/v0.2.28/dist`. GitHub Actions не использовались и ранее выключены по прямому решению владельца. `v0.2.27` более не актуальная установленная версия.
- Описание исполнителя в уведомлении: на каждом MCP-вызове необязательный `executor`: `project-manager`/«Менеджер проекта», `supervisor`/«Супервизор», без параметра/«ChatGPT». WinForms/Router Lite тесты прошли, однако экранная приёмка всех трёх вариантов на *установленной* версии пока не подтверждена.
- Действующие правила: разработка пакетами на DOMKAT; ~1 сек между управляющими GUI-действиями; CLI/файловые операции без искусственных задержек; при неожиданной ошибке fail-fast; STOP должен блокировать управление. Следующая функциональная идея — отличать ожидание модели от реального завершения — ещё не внедрена.
- Источник: владелец 30.10.2026, локальные Git/version/build; детали `docs/EXECUTOR-ATTRIBUTION-2026-10-30.md`, `docs/PROJECT-MANAGER-HANDOFF-2026-10-30.md`. Ниже в файле — исторические версии состояния и ранее открытые шаги, они не отменяют текущую сверку.

## 30.10.2026 — новая функциональность исполнитель
- Реализовано в локальных исходниках для v0.2.28: \`Executor\` в ControlActivity и подпись в WinForms overlay, необязательный \`executor\` во всех Windows MCP-вызовах, Router Lite, desktop.batch, передача файлов; fallback \`ChatGPT\`.
- Тесты .NET/WinForms/Windows MCP/Router Lite проходят без предупреждений. Новое поле объявляется вызывающей моделью, Host сам не гарантирует персонализацию. Релиз/установка ещё под отдельным подтверждением.
- Старая установленная v0.2.27 на DOMKAT не затронута в ходе текущей разработки. Прежняя Unicode-исправляющая работа v0.2.28 включена в этот код.

## ТЕКУЩЕЕ на 30.10.2026 — читать перед историей
- Менеджер `project-manager` v2.1.0, Git `main`, проект `lvlaksim1/chatgpt-mcp-tunnel`, основная локальная папка `D:\Dev\ChatGPT-MCP-Tunnel`, `D:\ChatGPTMCPAgent` — установленный агент, `D:\ChatGPT-MCP-Tunnel` — прежняя копия.
- DOMKAT установленная `v0.2.27`, агент/runtime/tunnel-client и bridge_ping после обновления подтверждены. MKATUSHONOK сейчас не перепроверялся. В GitHub v0.2.27 размещена прямой загрузкой локальных Inno установщиков; Actions (три workflow) выключены.
- Четыре незакоммиченные правки v0.2.28 исправляют буквальные `?` в меню и 4 unsafe-category подписях; первый C# build прошёл с CS0162; release ещё нет.
- Требование владельца: планировать и выполнять **пакетами** на DOMKAT, GUI ~1s, CLI без задержки, безопасный STOP. Индикация ожидания и достоверного завершения — backlog, не реализована.
- Полная последовательность и решения: `.context/decisions/operating-model-2026-10-30.md`, `docs/PROJECT-MANAGER-HANDOFF-2026-10-30.md`. Ниже — исторические срезы предыдущих версий.

## 2026-10-30: v0.2.24 return to ordinary ChatGPT instead of Work
- DOMKAT installed v0.2.23. Confirmed newest cloud chat 'Проверка предыдущего ответа' is Work/Astra (GPT-6 Astra and results sidebar) despite /c URL; old 'Приёмка MCP Tunnel v0.2.18' is ordinary ChatGPT (composer High). Separate Global tool invocation inside ordinary chat displayed widget unavailable, confirming need for a dedicated non-entrypoint tool.
- Source v0.2.24 created chat.bootstrap.open with own ui://chat-bootstrap-v0224 and no static entrypoint; its iframe auto-polls queue and sends target:new. Global chat.hub.open ui://hub-observer-v0224 no longer sends messages; agent accepts only /c/<uuid> URL and chatbridge-* heartbeat. Local build + contract/multichat/Router Lite/observer JS tests PASS; not installed or cloud accepted yet.

## 2026-10-30: v0.2.23 automatic Windows Global Hub activation
- 30.10.2026 on DOMKAT user-authorized full Yandex shutdown (browser.exe=0), Windows Start-Process Global ChatGPT URI cold-start confirmed ui-initialize and fresh hub instance. New local message 30OCT-9D41 was delivered via Global Hub and received reply ХОЛОДНЫЙ-УСПЕХ with status answered. Second follow-up forwarded but pending as of inspection. Source v0.2.22 context recovery was concurrently added and preserved.
- Added v0.2.23 source Windows ChatHostActivator: server-tray monitor pending unforwarded messages, global event heartbeat TTL, auto launch already validated Global URI from per-user configuration, rate limit 2m. Private plugin identifier not checked into repo. Agent/MCP .NET builds and selftest PASS. Installed DOMKAT still v0.2.21 pending CI/install. Details docs/mcp-hub-host-activator-v0.2.23.md.

## 2026-10-29: v0.2.22 local chat context recovery after Global cold start
- DOMKAT installed v0.2.21. Successfully closed all Yandex processes, cold-started Global from OS URI, then used local GUI to send test; hidden browser host produced real ChatGPT reply АВТОЗАПУСК-21 via chat.reply in matching chat. Follow-up into new cloud conversation lacked previous context and no chat.reply; pending ACK retained. v0.2.22 source context-restoring Global prepared/tested, not deployed yet. Report docs/mcp-global-hub-context-recovery-v0.2.22.md.

## 2026-10-29: v0.2.21 Global Hub cold-start and auto-relay experiment
- DOMKAT installed v0.2.20, readyz=200, both computers connected; user authorized closure of all Yandex Browser processes. Successfully closed all browser.exe (zero) and cold-started ChatGPT Global via system URL with fresh iframe initialize. Source v0.2.21 auto Global Hub exists, local isolated tests pass, pending CI. Detail: docs/mcp-global-hub-autorelay-v0.2.21.md.

## 2026-10-29: v0.2.20 Global/Thread host compatibility correction
- DOMKAT running v0.2.19 with 146 message files and functioning MCP readyz. In ordinary ChatGPT after tool refresh Global/Thread app-only entrypoints were reported as private tools without widgets; only resource-read events captured. Source v0.2.20 fixes UI visibility to model+app and removes overlapping static entrypoints from legacy chat.ui.open. Source builds and tests pass, CI/release not yet done. docs/mcp-apps-entrypoints-v0.2.20-host-adaptation.md.

## 2026-10-29: v0.2.19 Global/Thread lifecycle diagnostic implementation
- Working tree (not yet released): v0.2.19 isolated lifecycle diagnostics: chat.hub.open global-only, chat.thread.open thread-only, chat.lifecycle.event/status app-only, two ui:// resources, static UI handshake/heartbeat/teardown, privacy-filtered lifecycle log. Existing chat.ui.open and message delivery unchanged. Local C# build PASS, Python and JS probe tests PASS, old routing tests PASS. Installed DOMKAT still v0.2.18. Source docs/mcp-apps-lifecycle-experiment-v0.2.19.md.

## 29.10.2026: normative MCP 2026-07-28, MCP Apps and OpenAI extensions re-analysis
- Investigation re-centered on primary technical specifications (not third-party failures). MCP 2026-07-28 stateless server/client differs from MCP Apps iframe 2026-01-26: former has server/discover and per-request _meta, latter still requires ui/initialize/initialized. OpenAI extension adds global/thread entrypoints, deep-link hostContext and ui/message target:new/active. Full technical report: docs/mcp-2026-core-apps-openai-protocol-review-2026-10-29.md.
- Direct, isolated on DOMKAT current server v0.2.18 ModelContextProtocol NuGet 2.2.0: server/discover returned supportedVersions=[2026-07-28], capabilities tools.listChanged=true, resources.listChanged=true, logging; tools/list returned 52 tools using modern protocol. subscriptions/listen for toolsListChanged/resourcesListChanged returned notifications/subscriptions/acknowledged with both filters and subscriptionId=7. Modern resources/read for ui://chatgpt-mcp-tunnel/relay-v0218 returned resultType=complete, mime=text/html;profile=mcp-app and 13803 HTML bytes. Tool chat.ui.open lists global+thread entrypoints, model+app visibility, chat_id schema.
- Design implication: preserve single Tunnel, separate Windows persistent logical chat from MCP network session and MCP Apps View instance; use explicit (computer_id,chat_id,message_id), per-iframe instance leases, context capsule and ui/update-model-context. Modern subscriptions announce actual list/resource changes; to push chat-specific events introduce subscribed resources + resources.subscribe rather than misusing tools/list_changed. The UI View's supported standard MCP subset has tools/call/resources/read, so retain polling fallback for app.
- The model request itself is initiated by ui/message on a live ChatGPT Host; stateful Tasks and MRTR are for server operations/continuation, not a replacement for invoking the Host. Focus next experiments on global/thread entrypoint lifecycle, deep links, full context bootstrap and automatic Host activation after Windows reboot, without Work/Codex/API.

## 2026-10-29: Deep MCP Apps autonomy research, external GitHub implementations
- Deep MCP Apps entrypoint research saved as docs/mcp-apps-autonomy-deep-research-2026-10-29.md. No installed code or server modifications; DOMKAT production remains v0.2.18 with two-cloud-thread live handoff previously accepted. The official protocol requires a living Host for ui/message; static entrypoints initiate UI inside Host and do not themselves supply model inference.
- Official OpenAI spec says Web refers to Work browser excluding classic ChatGPT; test actual ordinary Yandex ChatGPT host separately. Deep links are global only; thread instance binding via server lease needed. GitHub comparisons/open issues documented.

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
- v0.2.17 source and Windows release 40924254070 SUCCESS. Installed on DOMKAT version 0.2.17.0 with official SHA256 c971f2f0652ec8e766181bfad7394d31f5a72c053a4b08e09ba0b845a34148e9; MCP readyz 200, DOMKAT/MKATUSHONOK connected, 142 messages retained, Windows watchdog Ready, cloud hourly automation still disabled.
- Ordinary ChatGPT tool refresh confirmed new chat.ui.open mode mcp_apps_multi_chat. However displayed MCP Apps iframe remained old 0.2.16 resource from ui://chatgpt-mcp-tunnel/relay; live target:new never actually ran. Server's installed HTML contained correct new JS.
- v0.2.18 source now changes UI resource URI to ui://chatgpt-mcp-tunnel/relay-v0218, tests adjusted; .NET builds and protocol tests pass. Pending CI release, install, real target:new test.

## 29.10.2026: v0.2.17 multi-chat source implemented, pending Windows CI and real ChatGPT
- 29 Oct 2026 owner-approved v0.2.17 code implementation completed for classic multi-chat on each computer. ChatWindow sidebar has '＋ Новый чат', titled session list with previews, click selection, separate message histories and retained local drafts/attachments. ChatSessionStore stores session metadata separately; existing messages unchanged in 'История до обновления'. GUI footer now reports the latest actual MCP chat.reply response instead of stale hourly scheduler.
- Server sends chat_id on chat.receive; chat.ui.open(chat_id), pending(chat_id,discover), ack(new_chat) and new iframe auto-start support ui/message target:new handoff without changing total 52 MCP tool names. Legacy no-chat-id widgets restricted to legacy chat.
- Selftests + .NET builds PASS; WinForms preview tested on DOMKAT in isolated root with two chats sent and switched; production DOMKAT still v0.2.16. v0.2.17 source pending commit/CI/deployment and real ChatGPT automatic new-cloud-chat acceptance.

## 29.10.2026: REAL ordinary ChatGPT ui/message acceptance 19:36-19:42 MSK
- Live 0.2.16 acceptance PASS: ordinary ChatGPT new session after plugin tool refresh exposed 52 tools; invoked chat.ui.open, real iframe opened, host advertised ui/message text plus OpenAI extension.
- Manual probe 58ewppk5 triggered model turn and exact answer. Auto relay enabled once: queue nonce 43830fd3 forwarded with durable ACK, answered via chat.reply; real DOMKAT WinForms text message GUI8C42 forwarded and replied; answer visibly displayed in GUI. Inactive ChatGPT browser tab: GUI message BGTAB42 also forwarded/acked/replied. All JSON statuses answered.
- Test window 19:36-19:42 MSK, not during planned hour-minute 44 scheduler. Existing scheduled fallback left enabled. Real iframe required to remain alive; closure/suspend behavior untested.
- Technical debt: GUI footer falsely claims model inactive/next hourly schedule after immediate successful MCP responses. MKATUSHONOK local GUI still untested.

## 29.10.2026: DOMKAT production update 0.2.16 and ChatGPT browser tool refresh
- Production DOMKAT v0.2.16.0 confirmed after controlled update from v0.2.13; SHA256 official verified; readyz 200, existing single tunnel bridge_ping OK, both DOMKAT and MKATUSHONOK connected, watchdog Ready, ChatRelayApp.html installed.
- In Yandex Browser authenticated ChatGPT browser settings, exact UI button 'Обновить инструменты' for the existing ChatGPT MCP Tunnel plugin was clicked; button disabled during execution and re-enabled, no visible error. Static 45-tool catalog in current pre-refresh ChatGPT session remains an unresolved cache question, not proof of failed update. New runtime 52 commands proven in Windows CI, real ChatGPT ui/message still NOT tested.
- First two silent Inno updates aborted with exit 5 because running agent/runtime and recovery respawn blocked RestartManager; controlled agent --shutdown and temporary watchdog pause permitted Inno success. Installer automatic update coordination requires fix.

## 29.10.2026: v0.2.16 stable Windows release verified (not live ChatGPT accepted)
- v0.2.16 stable GitHub release SUCCESS: 40907216014; source commit a853992; setup SHA256 efda9954ec9a519ebfca5f0364276e4c0bb58386b50bff63b1e690d11dca23ab; update SHA256 fc05177a63eec63e28bee080247ed7b7835a3758098d77d072efe9253696dba0. Full/update installation-preservation acceptance passed. DOMKAT still last observed running v0.2.13; ChatGPT current tool catalogue stale 45. Actual ui/message host behavior not proven.

## 29.10.2026: ui/message two-case diagnostic source tested (v0.2.16)
- 0.2.15 source + stable GitHub release confirmed; installed DOMKAT still v0.2.13, current ChatGPT exposed tool catalogue 45 not 52. Could not test real ChatGPT host with stale catalogue.
- Added ui/message test button for direct user-click probing (no queue ack), capability advertisement display and independent automatic polling comparison. JS simulated-host tests verify both success/rejection. .NET builds 0 errors/warnings and MCP stdio 52 tools / HTML resources PASS.
- Pending v0.2.16 CI installer release, owner-controlled deployment, refresh new MCP actions, real ChatGPT click and automatic timer verification.

## 29.10.2026: v0.2.15 released successfully; real ChatGPT behavior pending
- GitHub Actions 40904368593 SUCCESS (including Windows Inno Setup full/update and preservation); stable release https://github.com/lvlaksim1/chatgpt-mcp-tunnel/releases/tag/v0.2.15. Update SHA-256 2d4b44f88fcefb4560c750d09148b2795fcdd2dba12b7d44afc83babd60f94bd; Setup SHA-256 be1ce88211544aec8447034e5ba92a1b0bd573c8b9a73c762e71c7485ee61f03.
- Source implements MCP Apps ui/message 52 tools and server UI resources. Simulated host tests succeeded. No actual ChatGPT iframe host test and no installed DOMKAT update; still observed v0.2.13. Existing WSS tunnel healthy.

## 29.10.2026 — MCP Apps ui/message source prototype (not physically accepted)
- ui-message bridge in repo: chat.ui.open with _meta.ui.resourceUri ui://chatgpt-mcp-tunnel/relay and global/thread entrypoints, resource HTML via resources/read, app-only chat.ui.pending and chat.ui.ack; forwards local chat messages via host ui/message with target=active,send=true when user enabled.
- Confirmed locally: 52 tools, actual resources/read HTML, message+attachment metadata, durable idempotent ack, no ack on denied host, UI teardown handling. Test host simulator is not real ChatGPT. .NET builds zero warnings/errors, stdio chat, large file/Router Lite 23 Windows remote tool/reconnect tests pass.
- Version v0.2.15 source in preparation. Live ChatGPT host behavior and installed desktop GUI unverified; existing installed product and WSS router untouched. No scheduler, browser automation, Codex, Work or separate API added.

## 29.10.2026: v0.2.14 STABLE RELEASE verified (not deployed)
- GitHub Actions 40866882778 completed success on 21a1a0c: application and runtime build, GUI attachment selftest, real MCP image/text/integrity tests, 49 tool catalog and Router Lite 23 tools, file transfer, TLS, Inno Setup full/update/uninstall and preservation checks.
- GitHub Release v0.2.14 published with Setup/Update/legacy update alias; Setup SHA256 7ff5afc44db3a535b71e11b6f174141684f666dd0396c303fa03ee64d2d8174d, Update SHA256 ef4899062bde3caecb85fc29d23c03a155ea78d66902d6d49da761bf4f4504ef.
- Live DOMKAT on v0.2.13 was untouched and healthy; MKATUSHONOK v0.2.13 unchanged. Owner-side physical Ctrl+V, file-send GUI acceptance and ChatGPT plugin refresh to reach chat.attachment.read still pending.

## 29.10.2026: Chat attachment feature — SOURCE E2E PASS
- Added multi-file picker, drag/drop, Ctrl+V image, preview cards and removal, attachments-only message support to WinForms chat. Files copied to LocalAppData/ChatGPTMCPAgent/Chat/attachments/message_id/attachment_id; message JSON becomes visible only after copy and SHA-256; chat.receive returns attachment metadata, chat.attachment.read sends real MCP image or bounded text or binary local path.
- Verified Windows .NET builds with zero warnings/errors; isolated GUI selftest PASS; tests/test_chat_attachments.py validates image content block, UTF-8 text, invalid identifier, tampering and reply; original MCP 49-tools and Router Lite 23-tools/reconnect tests PASS.
- Version source v0.2.14 in preparation; Windows CI, stable release and live deployment are NOT proven. Both physical hosts currently installed v0.2.13, ChatGPT plugin catalog still shows 45 old tools.
- Evidence: local test/build and owner instruction 29.10.2026.

## 2026-10-29: PHYSICAL FILE TRANSFER PASS (MKATUSHONOK to DOMKAT)
- Real Router Lite WSS transfer job 4afa737122d1400dbacfa3e48cd39bee reached completed 100%, 324820992 bytes, from C:\Users\m.katushonok\Downloads\MAX.msi to C:\Users\Maksim\Downloads\MAX.msi.
- Source and destination SHA-256 independently match: 14A528C373B19B35B3C9E77B5FAEE88DC34A073F15D8801673DDB7C040B2D48F; destination Authenticode Valid (OOO Mnogo Prilozheniy).
- Progress checkpoints 26.47%, 37.12%, 51.65%, 76.18%, 80.70%, reconnecting at 80.70%, 85.22%, completed. Approx. speed 2.6-2.7 MB/s. No final partial or metadata files.
- DOMKAT v0.2.10 CLI same-user control pipe, MKATUSHONOK v0.2.9 client. 41 tools remain exposed in ChatGPT plugin snapshot; the new 3 MCP tool registrations still need a plugin refresh. CLI status reports valid data but nonzero exit code due response parser bug. GUI not visually inspected during this real transfer.


## File transfer feature developed and locally tested — 29.10.2026
- Feature for v0.2.9: authenticated 1 MiB Router Lite chunks, resume sidecars, SHA-256, explicit overwrite, MCP status/cancel/start and WinForms tray progress.
- Isolated Windows tests PASS: 15 MiB both directions, 90 MiB transfer, cancel and resume, overwrite, 45 MCP tools, LAN/WSS regressions, GUI creation.
- Physical MKATUSHONOK to DOMKAT transfer of MAX.msi (324820992 bytes) still awaits deployment; acceptance is NOT YET VERIFIED.
- Protocol: docs/file-transfers.md; decision .context/decisions/streaming-file-transfer-2026-10-29.md.


## Direct WAN TLS implementation: v0.2.8 released — 29.10.2026
- Решение владельца 29.10.2026: DOMKAT имеет статический публичный IPv4 на WAN-интерфейсе маршрутизатора; удалённый клиент расположен в другой локальной сети. Развивается прямой защищённый WSS без обязательного Tailscale/посредника. Локальный адрес DOMKAT не заменяется внешним IP. Источник: прямая команда владельца; код основной ветки f8cc66848a475030b63ad5c15e25ec6c68e76308.
- Сервер Kestrel обслуживает защищённый порт TCP 55202 в дополнение к прежнему локальному TCP 55201. Сервер создаёт собственный TLS-сертификат, шифрует пароль PFX через DPAPI Windows, клиент проверяет точный SHA-256 отпечаток до передачи общего ключа. Новый режим требует минимум 32 символа общего ключа. Проверены сборки, 42 инструмента, 23 направляемых инструмента Windows, запрет неверного сертификата, выполнение удалённой команды по WSS, восстановление связи, сохранение старого LAN-подключения; GitHub Actions 40781905752 завершён успешно; v0.2.8 опубликована с установщиками Setup и Update.
- Проверка 29.10.2026 через работающий MCP Tunnel: активный процесс ChatGptMcpAgent.exe PID 14536 имеет FileVersion 0.2.7.0 (запущен 2026-10-29 00:33:36 по системному времени Windows); Router Lite слушает 192.168.0.3:55201 (PID 2992), TCP 55202 не слушает. v0.2.8 не устанавливалась менеджером; bridge_ping успешен. Этот факт supersedes прежний текущий статус v0.2.6, но не меняет вывод о непроверенном межсетевом соединении. На маршрутизаторе не создавался проброс TCP 55202, правила брандмауэра не менялись. Физическая связь с MKATUSHONOK через Интернет и 23 удалённых инструмента остаётся НЕ ДОКАЗАНА. Публичный WAN IPv4 подтверждён чтением роутера; постоянство адреса основывается на словах владельца.
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

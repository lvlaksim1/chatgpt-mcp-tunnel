# MCP Tunnel: нормативная архитектура MCP 2026-07-28 + MCP Apps 2026-01-26 + OpenAI Extensions

Дата: 29.10.2026
Предмет: только технические спецификации и прямые протокольные проверки собственного MCP Tunnel. Чужой опыт не используется как критерий работоспособности.
Действующая версия: ChatGPT MCP Tunnel v0.2.18 (DOMKAT), исходный код с SDK ModelContextProtocol 2.2.0; установленную программу не изменяли.

## 1. Три независимых протокольных слоя

**Слой A — MCP Core 2026-07-28.**
Host/Client обращается к MCP Server через JSON-RPC. Новая редакция stateless: запрос самодостаточен, вместо initialize есть server/discover, вместо server-initiated requests используется MRTR, подписки через subscriptions/listen, отдельное расширение Tasks.

**Слой B — MCP Apps 2026-01-26.**
Host загружает ui:// ресурс по resources/read, создаёт sandboxed View. Между View и Host действует отдельный UI handshake: ui/initialize -> ui/notifications/initialized. Даже если слой A stateless, UI handshake слоя B сохраняется. View может запрашивать tools/call, resources/read, ui/message, ui/update-model-context и получает tool-input, tool-result, host-context-changed и teardown.

**Слой C — OpenAI MCP Extensions.**
В metadata инструмента _meta["openai/ui"]["entrypoints"] можно объявить global, thread, file. Расширение ui/message позволяет target:new/active и send:true/false; deep-link относительный маршрут поступает в hostContext["openai/deepLink"].url. Background context (annotations.audience:["assistant"]) не показывается пользователю, но может быть передан модели.

Это не один набор методов: слой A обрабатывает транспорт/инструменты, слой B жизненный цикл iframe, слой C поведение конкретного ChatGPT Host.

## 2. MCP 2026-07-28 — детальный список доступных механизмов

### 2.1 Stateless и capability negotiation

Любой современный request включает params._meta:
- io.modelcontextprotocol/protocolVersion: "2026-07-28";
- io.modelcontextprotocol/clientCapabilities: объект доступных клиентских возможностей;
- io.modelcontextprotocol/clientInfo: рекомендуемый идентификатор клиента.

Не требуется сетевой initialize/initialized и Mcp-Session-Id. server/discover возвращает supportedVersions и capabilities, может содержать cache TTL. Состояние прикладного чата НЕ исчезает вместе с сетевым протоколом: приложение явно передаёт server-minted state handle / chat_id как аргумент инструментов. Переносимая модель: любой вызов tools/call(chat_id, message_id, ...) может быть обслужен независимо от прежнего сетевого подключения.

### 2.2 Tools / Resources / Prompts

- tools/list, tools/call: операции модели и действия приложения; app-only visibility через _meta.ui.visibility=["app"] скрывает tool из списка модели, но остаётся доступна MCP Apps через Host.
- resources/list, resources/templates/list, resources/read: канонические адреса состояния, вложений и капсул; ресурс с параметрическим URI вида chat://computer/{computer_id}/session/{chat_id}/context. Для динамических ресурсов использовать templates/list. У ресурса есть MIME и метаданные.
- prompts/list и prompts/get: формализованный шаблон восстановления контекста, с параметрами chat_id и context_version. Вызов зависит от клиента; не считать prompts/get автоматическим началом нового model turn.
- tools/list и resources/list допускают ttlMs/cacheScope и детерминированные результаты; не плодить 1 tool на каждую историю или компьютер. Для динамики использовать аргументы.

### 2.3 Subscriptions/listen: события без постоянного polling

Подписчик отправляет subscriptions/listen с фильтром notifications:
- toolsListChanged, resourcesListChanged, promptsListChanged;
- resourceSubscriptions для конкретных resources/updated.

Server отвечает notifications/subscriptions/acknowledged с subscriptionId, далее отправляет события указанного вида. Это клиентско-серверная связь. Чтобы доставлять события чатов, следует объявить конкретные MCP resources (chat://.../events) и capability resources.subscribe, обновлять ресурс и отправлять notifications/resources/updated. Само событие НЕ тождественно созданию user/model turn. Список изменений инструментов не следует переиспользовать как чат-шину.

В расширении MCP Apps 2026-01-26 гарантированный список вызовов View включает tools/call и resources/read, но не обещает прямую поддержку modern subscriptions/listen в iframe. Поэтому подписку следует использовать на основном MCP client link/в собственном Windows-подписчике, а в UI иметь fallback polling через app-only tool.

### 2.4 MRTR: Multi Round-Trip Requests

В новом протоколе server-initiated запросы sampling/createMessage, elicitation/create и roots/list инкапсулируются в InputRequiredResult:
resultType="input_required", inputRequests={...}, requestState="...".
Клиент исполняет требуемое и повторяет исходный запрос с inputResponses и requestState. Это механизм продолжения *уже инициированного* вызова tool/resource/prompt, а не автономный генератор бесконечной переписки. Sampling в MCP 2026-07-28 находится в deprecated, поэтому новой архитектуре туннеля не стоит от него зависеть.

### 2.5 MCP Tasks extension

Расширение io.modelcontextprotocol/tasks поддерживает task resultType="task" с долговечным taskId, status/ttlMs/pollIntervalMs; tasks/get, tasks/update, tasks/cancel; статусы working, input_required, completed, failed, cancelled. Использовать для длительных команд на компьютерах, загрузок и восстановления после обрыва. Отличать это от модельной генерации: task может пережить потерю канала, но для нового обращения к модели нужен действующий ChatGPT Host. В текущем server/discover tasks НЕ объявлены; это архитектурное расширение будущей версии.

### 2.6 Стандартные транспорты

stdio + Streamable HTTP остаются стандартными; custom transports допускаются при соблюдении формата RPC. В 2026-07-28 Streamable HTTP POST-only, нет GET-stream или Last-Event-ID для восстановления; обработка повторного запроса должна быть идемпотентной. Один существующий Router Lite/WSS туннель остаётся допустимым внутренним custom transport, не требуется второй туннель.

## 3. MCP Apps — технический жизненный цикл

(1) tools/list содержит tool _meta.ui.resourceUri.
(2) resources/read отдаёт text/html;profile=mcp-app.
(3) Host создаёт View; View отправляет ui/initialize (протокол Apps 2026-01-26); Host возвращает hostInfo, hostCapabilities и hostContext.
(4) View отправляет ui/notifications/initialized.
(5) Host отправляет ui/notifications/tool-input с полными аргументами (если они есть); tool-result (если есть).
(6) View может вызывать tools/call (для внутренних сервисов), resources/read, ui/update-model-context и ui/message.
(7) Host может направить ui/notifications/host-context-changed при обновлении маршрута/отображения/контекста.
(8) Перед уничтожением Host отправляет ui/resource-teardown, View сохраняет состояние и освобождает lease. В случае внезапной потери процессе действует TTL + heartbeat на сервере.

Важное различие: ui/initialize относится к iframe/Host даже при stateless MCP Core 2026-07-28. Нельзя удалять его при обновлении C# SDK.

## 4. Global, Thread, Deep Link: точные роли

### Global entrypoint

Метаописание отдельного инструмента chat.hub.open, entrypoints:[{type:"global"}], с UI ресурсом ui://.../hub-v1. При открытии Host вызывает tool с arguments={}. На Desktop current-thread операции ui/message и ui/update-model-context привязаны к отдельному thread глобального приложения. Это кандидат для диспетчера, который получает события очереди и инициирует target:new. Не путать с обычным model tool bootstrap.

### Thread entrypoint

Метаописание отдельного chat.thread.open, entrypoints:[{type:"thread"}], ресурс ui://.../thread-v1. При открытии тоже arguments={}; у каждого ChatGPT thread своя View. chat_id надо установить через серверный реестр привязки или через отдельный model-invoked chat.ui.open(chat_id). Возврат в чат — повторное монтирование и claim lease, без потери истории.

### Deep link

Глобальное приложение может открываться по схеме, описанной OpenAI; относительный маршрут /chat/{chat_id}?computer=DOMKAT приходит через hostContext["openai/deepLink"].url; последующие изменения через host-context-changed. Конкретный URI зависит от host/platform; для Web документация показывает https://chatgpt.com/plugins/<plugin-id>/app/<tool-name>?path=... . Реальный plugin-id получать из установленного плагина, не придумывать. Адресное открытие можно вызывать из Windows-приложения, а затем регистрировать фактически созданный instance_id и его состояние.

Platform note: в текущей OpenAI спецификации столбец Web означает Work browser, а не classic ChatGPT. Это никак не отменяет уже принятую в ordinary ChatGPT отправку ui/message target:new; Global, Thread, deep-link следует отдельно испытать именно в используемом host.

### Разделение инструментов

- chat.hub.open — global-only, app-visible; технический диспетчер.
- chat.thread.open — thread-only, app-visible; технический транспорт конкретной беседы.
- chat.ui.open(chat_id) — существующий model-visible bootstrap, сохраняем контракт v0.2.18.
- chat.state.get/chat.queue.claim/chat.queue.ack/chat.route.heartbeat/chat.context.snapshot — app-only сервисы.
- chat.reply — model-facing точная запись ответа к исходному message_id.

Визуальная панель MCP Apps остаётся технической детализацией host; пользователь работает только с WinForms.

## 5. Контекст: корректное сочетание ui/message + ui/update-model-context

ui/update-model-context обновляет контекст **того экземпляра View**, который делает вызов. Это не универсальная память всех ChatGPT threads. Контекст может быть отложен до следующего модельного хода, и каждый вызов заменяет предыдущий контекст текущего View. Background annotations.audience:["assistant"] позволяют прикладывать системную капсулу без видимой пользователю инструкции.

При переключении локального чата:
1. Windows сохраняет событие и snapshot капсулы в постоянной DB. Каждый chat_id имеет context_version и связь с cloud thread.
2. Если облачный thread уже активен, его View получает актуальную капсулу через app-only tools/call и выполняет ui/update-model-context, затем ui/message target:active.
3. Если создаётся НОВЫЙ cloud thread, вызов ui/update-model-context в старом View не перенесёт контекст в новый thread. Поэтому начальный ui/message target:new должен включить необходимые сведения сам: краткую капсулу, chat_id и/или ссылку на ресурс context. После нового chat.ui.open(chat_id) новый View устанавливает полную актуальную капсулу для будущих turns.
4. Ответ пишется только по точной паре (computer_id,chat_id,message_id) с idempotent check. Затем Windows обновляет context_version.
5. Длинная история хранится локально; в запрос включается минимально достаточное state summary + последние сообщения и ссылочные документы. Оригинальные файлы никогда не перезаписываются.

## 6. Реальная проверка текущего v0.2.18

Изолированный server runtime (не затрагивал рабочую очередь) на DOMKAT:
- Проект использует ModelContextProtocol NuGet 2.2.0 / .NET 8.
- server/discover на MCP 2026-07-28: supportedVersions=["2026-07-28"]; capabilities: tools.listChanged=true, resources.listChanged=true, logging.
- tools/list с современным per-request _meta возвращает 52 инструмента и обязательный resultType.
- subscriptions/listen с notifications.toolsListChanged=true, resourcesListChanged=true возвращает notifications/subscriptions/acknowledged и subscriptionId=7 с обоими фильтрами.
- Существующий chat.ui.open содержит _meta["openai/ui"].entrypoints global + thread, обе роли представлены одной записью; default chat_id пустого инструмента ведёт в legacy.
- ChatRelayApp.html уже использует ui/initialize, tools/call, chat.ui.pending, ui/message target:new/active, ui/resource-teardown, но не использует ui/update-model-context, host-context-changed и собственный event-driven subscriber.
- В действующей версии двукратная реальная приёмка independent ChatGPT threads/возврат/ответ ChatGPT через chat.reply успешна.

## 7. Предлагаемая серверная модель чата

Persistent ChatSession:
  computer_id, chat_id, title, created_utc, context_version, context_hash, last_message_id;
  browser_thread_binding, resume_route, archived, sequence_number.

Message:
  message_id, chat_id, computer_id, role, created_utc, payload, attachment_refs,
  delivery_state[queued|claimed|host_accepted|answered|failed], attempt_no,
  reply_to, idempotency_key, chat_context_version.

ViewLease:
  instance_id, chat_id, computer_id, mode[global|thread|bootstrap],
  owner_epoch, lease_until, last_heartbeat, host_capabilities, host_context_route.

ContextSnapshot:
  chat_id, version, conversation_summary, active_task, accepted_decisions,
  last_user_turns, relevant_resources, unresolved_actions, tool_results_digest.

TaskStore:
  task_id, computer_id, chat_id, state, poll_interval, context_version, result_pointer.

Техническая гарантия: не требовать продолжения одного и того же процесса iframe после перезагрузки. Локальные данные и binding переживают reboot; новая View получает состояние по chat_id. Атомарный claim/ack + epoch защищают от двух параллельных View.

## 8. Поток после перезагрузки Windows

1. Windows запускает ChatGPT MCP Tunnel без участия пользователя, читает DB и показывает прежние чаты.
2. Менеджер поднимает один Router Lite/WSS transport и проверяет связность компьютеров.
3. Background Host Activator инициирует заранее выбранный Global entrypoint/deep-link; способ активации ordinary ChatGPT Host должен быть отдельным проверяемым протокольным действием, а не предположением.
4. После handshake Global View регистрирует instance_id/lease, читает pending, восстанавливает context_version.
5. Для старого thread восстанавливается его Thread View или создаётся новый через ui/message target:new с контекстной капсулой.
6. Новый Thread View выполняет tools/call, ui/update-model-context и далее ui/message target:active.
7. Модель вызывает chat.reply, Windows обновляет DB и UI; все промежуточные состояния сохраняются.
8. При потере Host исходное сообщение остаётся в DB и возобновляется после нового handshake; задача и контекст не теряются.

В этой архитектуре MCP управляет инструментами, ресурсами, состоянием и lifecycle. Генерация ответа всё ещё выполняется ChatGPT Host; именно его автоматическую активацию нужно доказать экспериментом без использования отдельного API или квоты Work/Codex. Это не вывод из чужого поведения, а разделение функций по нормативной спецификации.

## 9. Приоритет новых технических экспериментов

P0. Инструментально прочитать реальные hostCapabilities/hostContext обычного ChatGPT для ui/initialize, сохранить только технические значения и события.
P1. Разделить metadata global и thread в тестовом сервере; запустить оба entrypoint без user prompts, проверить tool arguments={} и регистрацию instance_id.
P2. Проверить deep link с фактическим plugin ID, получение пути /chat/{id}, host-context-changed.
P3. Протестировать ui/update-model-context с background annotations.audience:["assistant"] + target:new bootstrap при новой сессии. Проверить, что восстановлен контекст из DB.
P4. Добавить в тестовый server resources.subscribe для chat resource и проверить notifications/resources/updated/обратную доставку; во View оставить tools/call polling fallback.
P5. Проверить штатный OS-launch и реконнект ChatGPT Host при выключенной браузерной вкладке. Обязательно смотреть фактический ui/initialize, ui/message и chat.reply, а не только факт запуска URL.
P6. Рестарт Windows: chat state DB, два независимых чата, очередь и вложения; тест не должен зависеть от планировщика ChatGPT, Work/Codex/отдельного API.

## 10. Первичные документы

- Core changelog 2026-07-28: https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/changelog.mdx
- MCP stateless/transport: https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/basic/transports/streamable-http.mdx
- Core architecture: https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/architecture/index.mdx
- MRTR: https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/basic/patterns/mrtr.mdx
- Tools: https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/server/tools.mdx
- Resources: https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/server/resources.mdx
- Prompts: https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/server/prompts.mdx
- MCP Tasks: https://github.com/modelcontextprotocol/ext-tasks/blob/main/specification/2026-07-28/tasks.md
- MCP Apps 2026-01-26: https://github.com/modelcontextprotocol/ext-apps/blob/main/specification/2026-01-26/apps.mdx
- OpenAI extensions: https://github.com/openai/mcp-extensions/blob/main/docs/spec.md

## 11. Третий прямой протокольный тест

- Modern MCP 2026-07-28 tools/list metadata: chat.ui.open advertises global+thread, visibility model+app and inputSchema chat_id.
- Modern MCP 2026-07-28 resources/read: ui://chatgpt-mcp-tunnel/relay-v0218, resultType=complete, mimeType=text/html;profile=mcp-app, HTML=13803 bytes; no error. Static UI resource and model tools are served by the same server in the modern protocol.

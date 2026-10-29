# Исследование MCP Apps: автономность ChatGPT MCP Tunnel без пользовательского браузера

Дата исследования: 29.10.2026. Исходная работающая система: ChatGPT MCP Tunnel v0.2.18, Windows 10 / DOMKAT и MKATUSHONOK, обычный ChatGPT. Исследование не меняет установленную программу.

## 0. Цель и условия

Пользователь взаимодействует только с Windows-приложением ChatGPT MCP Tunnel. Список независимых чатов, история, вложения и контекст сохраняются после перезагрузки. Браузерная вкладка ChatGPT не является обязательным пользовательским интерфейсом. Рабочий транспорт — единый MCP Tunnel, без платного API и без расходования квот Work/Codex.

Критерий: полностью перезагрузить Windows, не открывать браузер руками, открыть старый чат локального приложения, отправить сообщение и получить ответ с корректным предыдущим контекстом.

## 1. Официальная архитектура: Server ≠ Host ≠ View

Стандарт MCP Apps 2026-01-26 различает:
- MCP Server: инструменты и ui:// HTML-ресурсы.
- Host: AI-клиент, который вызывает инструменты, рендерит iframe и обслуживает модель.
- View: iframe с postMessage JSON-RPC; через Host вызывает tools/call и ui/message.

Global и Thread entrypoints объявляются в _meta["openai/ui"]["entrypoints"] инструмента и вызывают tools/call с аргументами {} без необходимости инициирующего обращения модели. Они упрощают запуск интерфейса *в уже работающем Host*, но сами по себе не являются генератором ответов или безхостовым транспортом. В частности, собственный MCP AppBridge на Windows может быть реализован, но без model backend ui/message будет поступать только этому собственному Host.

Источник: https://github.com/modelcontextprotocol/ext-apps/blob/main/specification/2026-01-26/apps.mdx
Описание расширений OpenAI: https://github.com/openai/mcp-extensions/blob/main/docs/spec.md

## 2. Возможности global, thread, deep links

### Global
- Отдельный fullscreen-приложение в основном интерфейсе ChatGPT; его tool принимает {}.
- В Desktop-запуске обращения ui/message target:active и ui/update-model-context относятся к thread этого глобального приложения.
- Подходит для логического диспетчера и выполнения MCP tools/call без отдельных model turns.
- UI HTML и навигационный элемент кэшируются; результат инструмента нет. Официально рекомендовано stale-while-revalidate и localStorage для Desktop, но не хранить в localStorage единственную копию пользовательского контекста (проблема общего стандарта #417).
- Наличие статической точки входа не доказывает автоматическую загрузку iframe при старте ОС и не доказывает генерацию без Host.

### Thread
- Вызов {} в панели конкретного ChatGPT-диалога, отдельный экземпляр на thread.
- Подходит для подключения и переподключения транспорта конкретного локального chat_id.
- Восстановление связи требует серверной регистрации instance_id / chat_id / компьютера; аргумент chat_id при ручном открытии сам не поступает.
- После model-invoked tool call может появиться отдельный iframe даже в том же thread. Нельзя предполагать совпадение идентификаторов.
- Разработчик в issue #54 воспроизвёл дубли, а сотрудник OpenAI рекомендовал WebMCP tools для действий на конкретном открытом экземпляре; видимую моделью серверную команду нужно отделять от UI-инстанса.

### Deep links
- Ссылки адресуют страницы глобального приложения. Web: https://chatgpt.com/plugins/<id>/app/<tool>?path=<percent-encoded-relative-path>; Desktop использует схему codex://; iOS — chatgpt://.
- Host получает расшифрованный путь через hostContext["openai/deepLink"].url; изменения — ui/notifications/host-context-changed.
- Это хороший маршрут восстановления локального окна/конкретного чата, но deep links не гарантируют привязку к существующему thread/iframe, reuse вкладки или отсутствие открытого Host. Из issue #37 видно, что глобальная ссылка на Web может открыть отдельную вкладку. Не строить на ней гарантию reuse.
- В спецификации столбец Web относится к Work browser и прямо исключает classic ChatGPT; именно поэтому каждый сценарий обязан быть проверен в обычном ChatGPT/Yandex, несмотря на то, что ui/message target:new там уже успешно подтверждён.

Источник: https://github.com/openai/mcp-extensions/blob/main/docs/spec.md
Практические рекомендации: https://github.com/openai/mcp-extensions/blob/main/docs/patterns.md
Issue #37: https://github.com/openai/mcp-extensions/issues/37

## 3. Ключевой жизненный цикл

- tools/list объявляет доступность UI и static entrypoints.
- resources/read загружает UI HTML, который Host может кэшировать.
- View посылает ui/initialize; Host возвращает действительные capabilities и hostContext.
- View сообщает ui/notifications/initialized; получает tool-input и tool-result.
- View может делать tools/call (без model turn), ui/update-model-context (обновление контекста без model turn), ui/message (инициирует обращение к модели), ui/open-link.
- Host посылает ui/resource-teardown перед закрытием View; View сохраняет курсор и освобождает lease.
- Чтобы не ждать корректного teardown при аварии, сервер контролирует heartbeat и TTL/fencing.

ui/message с target:new и target:active подтверждены в нашей v0.2.18 в обычном ChatGPT: созданы два независимых локальных чата, оба получили ответы, возвращение к первому также сработало.

MCP Apps не переносит память модели между независимыми ChatGPT threads автоматически: нужно хранить локальную историю/резюме решений и формировать context capsule для нового thread. ui/update-model-context заменяет ранее установленный контекст конкретного UI и не начинает генерацию без ui/message.

Источник: https://github.com/modelcontextprotocol/ext-apps/blob/main/docs/overview.md
Расширения: https://github.com/openai/mcp-extensions/blob/main/docs/spec.md

## 4. Практический опыт разработчиков

1. open-mcp-apps, https://github.com/2nd1st/open-mcp-apps — SQLite-постоянное хранилище отдельно от iframe, append-only change_event, идемпотентные command_id и OCC. В standalone browser viewer отображение и редактирование данных работают, но без присоединённого chat sendMessage не вызывает модель. Для нашего туннеля важно: локальная история и индекс чатов не должны зависеть от Web UI.
2. goose-chatgpt-web, https://github.com/luke-m-selway/goose-chatgpt-web — внешний менеджер логического сеанса, loopback daemon, Electron BrowserHost для доступного ChatGPT Web, собственный порядок запуска tunnel/BrowserHost/daemon. Сессия Goose восстанавливается даже когда следующая модельная операция открывает свежий Temporary Chat. Архитектурный образец разделения контекста и облачного thread, но модель всё ещё требует работающего BrowserHost, и опубликованный reboot proof не завершён.
3. ChatCmd, https://github.com/int04/ChatCmd — локальный чат и задачи, браузерное расширение с авторизованной вкладкой ChatGPT. Полезен для независимого локального UI, журнала и завершения задач, но не является браузер-независимым транспортом.
4. PC-Bridge, https://github.com/Martin123132/PC-Bridge — Windows MCP, локальный аудит, постоянное веб-представление ChatGPT внутри десктопного приложения. Прикладной пример скрытия веб-инфраструктуры от пользователя, но не отсутствия браузерного движка.
5. codex-trajectory, https://github.com/icesixgod/codex-trajectory/blob/main/docs/interface.md — отдельные app-only helpers и глобальная/внутридиалоговая точка входа, единый viewer и локальный индекс. Проверен в другой host-поверхности, поэтому не переносить результат без проверки ordinary ChatGPT.
6. PMX Canvas, https://github.com/pskoett/pmx-canvas/blob/main/docs/mcp.md — self-contained UI ресурс с global+thread entrypoint и server-side состоянием, поддержка host-dependent display modes. Показывает жизнеспособность statically advertised entrypoints.

### Важные GitHub Issues
- https://github.com/openai/mcp-extensions/issues/29 — 02.10.2026 reported: thread entrypoint виден, но tools/call не отправляется; сотрудник OpenAI указал на исправление 21.10.2026, issue закрыта. Надо перепроверить в нашем обычном клиенте.
- https://github.com/openai/mcp-extensions/issues/54 — 12.10.2026 reported дубли UI при повторных model calls; 27.10.2026 ответ OpenAI про WebMCP; закрыто not planned. Требует своего instance registry.
- https://github.com/openai/mcp-extensions/issues/37 — адресная ссылка на глобальную app может открыть новый таб вместо возврата в конкретный thread; запрос на instance targeting остаётся важен.
- https://github.com/modelcontextprotocol/ext-apps/issues/417 — хранение состояния View через localStorage не является гарантией общего протокола; server-side хранилище обязательно.
- https://github.com/modelcontextprotocol/ext-apps/issues/744 — предлагаются preload и ui/close, пока обсуждение; нельзя считать реализованным.
- https://github.com/modelcontextprotocol/ext-apps/issues/754 — запрос на persistent/pinned UI, показывает проблему lifetime даже у других разработчиков.

## 5. Архитектура следующего этапа

- Windows GUI — единственный пользовательский интерфейс: chats, messages, attachments, drafts, search, statuses.
- Chat State DB (желательно SQLite) — локальные chat_id, computer_id, сообщения, авторство, курсоры, snapshots, context capsules, события, model reply, идентификатор и статус cloud thread при наличии подтверждённого поля.
- MCP Tunnel (один существующий) — единственный канал инструментов компьютеров; все обращения от host через существующий MCP server.
- Global Hub App (скрытая техническая роль) — обнаружение новых чатов, создание новых ChatGPT threads через ui/message target:new, health, reconnect.
- Thread Relay App — привязан к chat_id, закрепляет lease, отправляет target:active, пишет chat.reply.
- Lifecycle Supervisor — отслеживает ОС / host / route leases, может запускать проверенную точку входа. Его UI полностью отсутствует из пользовательского мессенджера.
- Local context engine — после каждого подтверждённого ответа обновляет capsule: цель, решения, последние сообщения, указатели на вложения, незавершённые задачи. При новом thread передаёт capsule и логический chat_id.
- Queue ownership: claim(chat_id,instance_id,epoch,ttl) атомарно; ack проверяет epoch, reply — message_id и exact chat_id. Одновременное выполнение нескольких iframe не даёт двойной отправки. При статусе forwarded-but-unanswered нужно сначала reconcile с тем же thread, а не слепо дублировать запрос.

## 6. Как может выглядеть восстановление после reboot

A. Windows запускает наш агент; State DB читается до попытки создать Host. GUI немедленно показывает все чаты и сообщения.
B. Менеджер проводит readiness existing tunnel; видит отсутствие активных Thread/Global компонент.
C. Проверить, можно ли системным URI/deep link активировать существующий ChatGPT host без видимой вкладки и без потребления другой квоты. Это гипотеза: глобальная точка входа и deep links не предоставляют документированной гарантий запуска inference при остановленном Host.
D. После подтверждения handshake Host / Global Hub Hub объявляет ready; связывает первый ожидающий chat_id с новым или восстановленным thread.
E. UI отправляет ui/message, ответ привязывается к локальному message_id; предыдущий контекст берётся из State DB.
F. Если Host недоступен, сообщение остаётся в очереди и GUI показывает честное состояние ожидания; потеря пользовательских данных исключена.

## 7. Решающий эксперимент

Цель: проверить не просто открытие MCP App, а получение ответа, когда все обычные ChatGPT browser tabs/процессы сначала закрыты.

1. Выгрузить браузер, оставив работающими Windows MCP Tunnel и агент.
2. Создать сообщение с уникальным nonce в локальном чате.
3. Инициировать системное открытие Global entrypoint или поддерживаемой deep link из Windows, без открытия браузера пользователем. Протоколировать процесс-хост, реально созданную iframe, ui/initialize, hostCapabilities, tools/call, ui/message, chat.reply, foreground window.
4. Повторить после завершения всех процессов host. Если запуск не происходит, это конкретное доказательство необходимости отдельного runtime/host; не считать открытый URL равнозначным готовой модели.
5. Повторить с двумя чатами, закрытием/восстановлением, несколькими iframe и после reboot.
6. Проверить, что используются ресурсы именно обычного ChatGPT, а не другая модельная квота; проверять по фактическому режиму ChatGPT и опубликованным условиям.

## 8. Вывод

Global и Thread entrypoints — сильные механизмы управления жизненным циклом и архитектурой транспорта. Они не являются сами по себе автономным вычислительным endpoint для модели. Полную независимость UI, истории, восстановления контекста и хранения очередей мы можем сделать в Windows уже сейчас. Для полной независимости генерации от работающего ChatGPT Host требуется отдельное испытание реального сценария restart/no-browser.

Основное инженерное решение: **развести логический чат и транспорт**. Не делать iframe источником истины. Не вносить Work/Codex/API в основную архитектуру. Не создавать второй туннель и не включать почасовую автоматизацию.

Источники:
- https://github.com/openai/mcp-extensions/blob/main/docs/spec.md
- https://github.com/openai/mcp-extensions/blob/main/docs/patterns.md
- https://github.com/modelcontextprotocol/ext-apps/blob/main/specification/2026-01-26/apps.mdx
- https://github.com/modelcontextprotocol/ext-apps/blob/main/docs/overview.md
- https://github.com/modelcontextprotocol/ext-apps/blob/main/examples/basic-host/README.md

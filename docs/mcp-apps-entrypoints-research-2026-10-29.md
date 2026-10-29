# MCP Apps: global, thread и deep-link — исследование для ChatGPT MCP Tunnel
Дата: 2026-10-29. Проверенная версия туннеля: v0.2.18.
Статус: аналитика без изменения работающего приложения.

## Проверенные источники
- OpenAI MCP Extensions spec: https://github.com/openai/mcp-extensions/blob/main/docs/spec.md
- OpenAI implementation patterns: https://github.com/openai/mcp-extensions/blob/main/docs/patterns.md
- Plugin Extensions: https://developers.openai.com/plugins/build/extensions
- MCP Apps stable specification: https://github.com/modelcontextprotocol/ext-apps/blob/main/specification/2026-01-26/apps.mdx

## Основные механизмы
- Global entrypoint: полноэкранное приложение в основной навигации; открывается пользователем без команды модели. На Desktop у него собственный thread для ui/message и ui/update-model-context. При открытии инструмент получает пустой объект {}.
- Thread entrypoint: отдельный экземпляр MCP Apps для каждой ChatGPT-беседы, открываемый пользователем в панели диалога. При ручном открытии инструмент также получает {}.
- Deep link: https://chatgpt.com/plugins/{plugin-id}/app/{tool-name}?path={percent-encoded-relative-url}. Маршрут с ведущим / поступает в hostContext["openai/deepLink"].url при ui/initialize, а изменения пути — в ui/notifications/host-context-changed. Реальный plugin-id следует извлекать из установленного плагина.
- UI-инструмент объявляет entrypoints в _meta["openai/ui"]["entrypoints"], UI-ресурс — в _meta["ui"]["resourceUri"]. Можно объявить несколько entrypoints, но OpenAI рекомендует отдельный инструмент для каждого.
- Для entrypoints ChatGPT выбирает fullscreen; для вызовов модели доступны fullscreen и inline через resource metadata.
- Sidebar/icon и UI HTML кэшируются на Desktop, tool result — нет; интерфейс может сразу отобразить локальный кэш и загрузить свежие данные через tools/call. Версионировать ui:// resourceUri для обновления HTML.
- ui/message поддерживает target:new/active и send:true/false. В v0.2.18 реальные сценарии target:new/active с двумя независимыми локальными чатами проверены.
- ui/update-model-context может обновлять контекст текущего экземпляра приложения без нового model turn. tools/call работает без отдельного model turn.
- ui/open-link может запросить открытие адреса у хоста; это перспективный механизм для перехода между global/deep-link страницами.

## Что сейчас реализовано в v0.2.18
В ChatUiTools.cs у chat.ui.open одна запись tools/list с двумя entrypoints: global и thread, resourceUri = ui://chatgpt-mcp-tunnel/relay-v0218. При вызове без chat_id сервер назначает LegacyId, то есть оба статических входа пока не представлены полноценными самостоятельными режимами. В ChatRelayApp.html уже работают polling, ui/message, tools/call и подтверждение ACK.

Приёмка двух новых диалогов и переключения обратно — успешна, но прямое открытие global/sidebar, thread-entrypoint вручную и deep-link ещё не проверялось именно в обычном ChatGPT в Яндекс Браузере.

## Предлагаемая архитектура в едином MCP Tunnel
1. chat.ui.hub — отдельный global entrypoint, полноэкранный диспетчер всех доступных компьютеров, локальных чатов, очередей и состояний. При открытии с аргументами {} вызывает tools/call и синхронизирует состояние; не требует chat_id и участия модели для чтения статуса.
2. chat.ui.thread — отдельный thread entrypoint. Каждый диалог имеет свою iframe, привязанную к существующему локальному chat_id. Если при ручном открытии args={}, предложить выбрать локальный чат либо восстановить привязку на сервере; не назначать архивный чат автоматически.
3. chat.ui.open(chat_id) — сохранить совместимый инструмент для начальной bootstrap-привязки в новом ChatGPT thread.
4. Новое сообщение в Windows: hub обнаруживает очередь и направляет ui/message target:new с bootstrap-инструкцией; новый ChatGPT thread вызывает chat.ui.open(chat_id); соответствующая iframe получает lease на очередь и далее использует target:active.
5. Навигация: глобальная точка открывает нужный локальный чат по маршруту /chat/{chat_id}?computer={computer}. Deep links открывают Hub адресно из Windows-приложения или системной ссылки (после проверки в обычном ChatGPT). Это навигация, не отдельный API сообщений.
6. Восстановление: hub и thread UI сообщают heartbeat. Сервер хранит chat_id, instance_id, epoch/fencing token и срок действия lease. Новый экземпляр после открытия подтверждает право на очередь; незавершённые сообщения сохраняются и повторяются без дубликатов.
7. После закрытия браузера Windows может повторно открыть подтверждённую deep link системным способом, а UI восстанавливает связь после handshake. Открытие ссылки само по себе не доказывает запуск модели; проверить фактическую доставку.

## Приоритеты экспериментов
A. В обычном ChatGPT нажать глобальную точку входа без сообщения модели, проверить fullscreen, initial args={}, host context и MCP tools/call.
B. В двух разных диалогах открыть thread entrypoint вручную, подтвердить независимые экземпляры и привязку.
C. Проверить реальную deep link для собственного plugin-id с path=/chat/{chat_id}; прочитать hostContext и уведомления смены пути.
D. Проверить автоматический запуск polling после входа в Hub, без кнопки «Начать передачу».
E. Проверить закрытие/открытие вкладки, затем всего браузера, и возврат к существующему чату.
F. Проверить два одновременно открытых экземпляра, аренду очереди, отсутствие двойной доставки.
G. Проверить маршрутизацию DOMKAT и MKATUSHONOK как отдельные источники с правильными ответами.

## Технический приоритет
Сначала отделить глобальный Hub от thread relay, потом ввести deep-link маршруты, после — автоматическое восстановление через системный запуск ссылки и защищённую аренду очереди. Сохранить единый туннель, старую историю, отключённый почасовой планировщик, работу в обычном ChatGPT без Work/Codex/API.

Важно: спецификация помечает Web как Work browser; наличие глобального значка, ручной thread-tab и web deep link нужно проверить именно в обычном ChatGPT. Наличие этих возможностей в SDK — не эквивалент проверке конкретного хоста.

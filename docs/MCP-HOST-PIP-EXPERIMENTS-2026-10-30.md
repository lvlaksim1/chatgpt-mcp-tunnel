# MCP Tunnel — подтверждённые эксперименты с ChatGPT Host, 30.10.2026

## Условия и методика
- Владелец поручил попробовать все MCP-механизмы, включая `pip`, несмотря на указанное в спецификации OpenAI отсутствие поддержки.
- Режим пользователя: **обычный ChatGPT**, не Work/Codex/API.
- Изолированная ветка: `experiment/mcp-host-pip-20261030`, исходный коммит `cdb6cb1`; экспериментальная сборка v0.2.38 на DOMKAT, резервная копия v0.2.37.
- Для исключения кеша HTML использован **новый** ресурс `ui://chatgpt-mcp-tunnel/lifecycle-thread-v0238-pip`.
- Отправка текстов и существующая очередь **не изменялись**. Диагностика не посылала `ui/message`, не использовала браузерный DOM или отдельный API.
- Записи: `%LOCALAPPDATA%\ChatGPTMCPAgent\Chat\lifecycle-probe\events.jsonl` с событиями и обезличенными capabilities. Секреты, cookies, пароли и содержимое переписки не сохраняются.

## Прямые данные реального ChatGPT Host

| Параметр | Фактически получено |
|---|---|
| Host | `chatgpt` |
| MCP Apps protocol | `2026-01-26` |
| Точка входа | `chat.thread.open` (thread) |
| Последовательность | `tool-open` → `resource-read` → `ui-initialize` → `ui-initialized` |
| displayMode при старте | `inline` |
| hostContext.availableDisplayModes | `inline, fullscreen` |
| hostCapabilities.message.text | `true` |
| hostCapabilities.updateModelContext | `true` |
| hostCapabilities.experimental[openai/message] | не объявлено в этом диагностическом сеансе |
| hostCapabilities.sampling | не объявлено |
| hostContext[openai/deepLink] | отсутствует |

### Pip: РЕАЛЬНЫЙ отрицательный результат (не синтетика)

Обе проверки сделаны прямым запросом `ui/request-display-mode` из живого MCP Apps iframe, который объявил свою поддержку `inline, fullscreen, pip`.

- 14:34:21 UTC: запрос `{mode:"pip"}` → ответ `{mode:"fullscreen"}` в 14:34:22 UTC.
- 14:37:13 UTC: повторный `pip` → повторно `fullscreen` в 14:37:16 UTC.
- Host **не объявлял `pip` в availableDisplayModes**. Отправка экспериментального запроса при отсутствии объявления — намеренная разовая проверка того, что механизм не работает скрыто, несмотря на опубликованное описание.
- Вывод: хост принял запрос отображения, но `pip` не включил. Статус: **PIP НЕ ПОДТВЕРЖДЁН, дважды возвращён fullscreen**. Нельзя обещать плавающий PIP-компонент.

### Жизненный цикл

- После `ui-initialized` получали `heartbeat` даже при `visibility="hidden"`. За диагностический интервал зафиксировано как минимум 12 событий от скрытого компонента.
- `ui/notifications/host-context-changed` подтверждает изменение `displayMode` на `fullscreen`.
- Нельзя выводить из этого бесконечный жизненный цикл: хост вправе приостановить или уничтожить iframe, а возможность восстановить его из сервера не доказана.
- При `chat.hub.open` в обычной беседе был вызов `tool-open`, **без нового `resource-read` и `ui-initialize`** для Global. Регистрация Global Entrypoint сама по себе не равна активации iframe.

## MCP Runtime и альтернативы (изолированный CLI, НЕ доказательство свойств ChatGPT Host)

| Проверка | Результат |
|---|---|
| initialize 2025-11-25 | договорённость успешна |
| initialize 2026-07-28 | ошибка -32602; SDK сейчас не поддерживает эту версию |
| server/discover | метод не поддерживается (старый протокол) |
| tasks/get | -32601 Method not found |
| subscriptions/listen | -32601 Method not found |
| resources/subscribe | **успех** |
| resources/unsubscribe | **успех** |
| resources capability | `{listChanged:true}` |
| tools capability | `{listChanged:true}` |
| prompts/list | -32601 |
| hostCapabilities.sampling в реальном iframe | отсутствует; Sampling не вызывали, чтобы не обходить согласование возможностей |

**Перспективная находка:** события изменения очереди можно будет реализовать через ресурсы и уведомления `notifications/resources/updated` для действующих подписчиков, если ChatGPT Host это пропустит. При этом ресурсные подписки не создают новых модельных ходов и не запускают разрушенный iframe.

## Deep Links, MRTR и Tasks

- OpenAI описывает Deep Links именно для **Global Entrypoint**: `https://chatgpt.com/plugins/<plugin-id>/app/<tool-name>?path=...` и `codex://...` для Desktop. В старых локальных данных есть маршрут `/mcp-app/<plugin-id>/chat.hub.open`, но он был отключён именно из-за риска режима Work.
- Реальное открытие Global Deep Link в данном цикле **не выполнялось**, потому что оно не подтверждено для обычного ChatGPT и могло переключить пользователя на Work. Не заявлять его работоспособность.
- Задания Tasks, MRTR/Sampling 2026-07-28 нельзя считать реализованными текущим SDK, пока он не согласует эту версию и не зарегистрирует соответствующие handlers. Для ChatGPT Host поддержка Sampling дополнительно не объявлена.
- Поддержка других MCP-клиентов, например Goose/Ollama, требует отдельного клиента и отдельной приёмки; не считать вопрос решённым.

## Вывод и следующий эксперимент

**Главный выявленный технический факт:** Thread MCP Apps iframe может выполнять периодические действия, будучи скрытым. Это наиболее перспективная зацепка для устойчивого транспорта внутри **уже созданного хостом iframe**; `pip` для этого не требуется.

Следующий приоритет: в контролируемом отдельном тестовом чате проверить, сможет ли уже инициализированный скрытый Thread Entrypoint отправить `ui/message` без нажатий и получить реальный `chat.reply` от модели. Только при успехе переходить к многократному тесту и попыткам восстановления после teardown.

**Оставшийся критический блокер:** хост-активация MCP Apps без участия пользователя после уничтожения компонента; документация сервера не даёт права создавать iframe самостоятельно.

## Источники
- https://github.com/modelcontextprotocol/ext-apps/blob/main/specification/2026-01-26/apps.mdx
- https://github.com/openai/mcp-extensions/blob/main/docs/spec.md
- https://blog.modelcontextprotocol.io/posts/2026-07-28/

## Прямой E2E пробный запрос из локальной очереди (14:43-14:44 UTC)
- Создано единственное собственное диагностическое сообщение 68bc3f71dc044066859f4994260bad8f в уже существующем тестовом chat_id=bc700ddc335d470ba79455878b5c142f. Личных данных не было.
- Из обычной беседы ChatGPT вызван chat.ui.open(chat_id=...), инструмент вернул ok=true и ресурс relay-v0237.
- Почти минуту отсутствовали файл ui-forwarded, ответ chat.reply и обновление времени ui-routes/LastSeenUtc. Это реальный неуспешный E2E.
- Собственный неотправленный тест перенесён из живой очереди в .tools/experiments/mcp-host-20261030/undelivered-roundtrip-68bc3f71.json, чтобы позднее не отправился неожиданно. Исходные сообщения пользователя не менялись.
- Возврат ok:true от инструмента сам по себе не подтверждает открытие iframe, вызов ui/message и получение ответа модели.

## Дополнительная проверка Resource Subscription
- Runtime MCP 2025-11-25 успешно выполнил resources/subscribe и resources/unsubscribe на диагностическом ресурсе, resources.listChanged=true.
- Сервер пока не реализует выпуск notifications/resources/updated при появлении новых сообщений, а ChatGPT Host не объявлял ресурсы в capabilities диагностического iframe. Поэтому подписки не являются готовой доставкой.

## Итоги и следующий эксперимент
- Подтверждены работа живого Thread iframe, два ответа fullscreen на запрос pip, heartbeat скрытого iframe и поддержка базовых ресурсных подписок в локальном сервере.
- Не подтверждены автозапуск очереди в обычном ChatGPT, Global iframe после вызова tool, безопасный Deep Link из обычного ChatGPT, Tasks/MRTR в текущем SDK, Sampling у Host.
- Следующий эксперимент: установить, почему relay-v0237 не начал polling после успешного chat.ui.open; испытать ui/message непосредственно из живого Thread iframe в безопасной отдельной беседе.

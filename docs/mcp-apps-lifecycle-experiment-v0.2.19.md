# v0.2.19 — проверка жизненного цикла Global / Thread MCP Apps

Дата: 2026-10-29. Назначение: инструментальное исследование в обычном ChatGPT на DOMKAT. Не замена работающего транспорта v0.2.18.

## Цель

Проверить активацию Global/Thread entrypoints, отдельно зафиксировать tools/call, resources/read, ui/initialize, host-context-changed, heartbeat и ui/resource-teardown. Проверить действие после закрытия вкладки и браузера. Конечная цель: локальные чаты с постоянной памятью без видимого ChatGPT.

## Реализация

1. chat.hub.open — Global-only entrypoint, отдельный ресурс ui://chatgpt-mcp-tunnel/lifecycle-global-v0219, app-only. Статическое открытие с arguments={} фиксируется в журнале.
2. chat.thread.open — Thread-only entrypoint, отдельный ресурс ui://chatgpt-mcp-tunnel/lifecycle-thread-v0219, app-only.
3. chat.lifecycle.event — app-only технический журнал событий. Сохраняет только разрешённые поля без сообщений, паролей, cookies, заголовков или полного URL. Файл %LOCALAPPDATA%/ChatGPTMCPAgent/Chat/lifecycle-probe/events.jsonl.
4. chat.lifecycle.status — app-only чтение последних 50 технических событий.
5. ChatLifecycleApp.html — отдельный instance_id, ui/initialize, initialized, host-context-changed, heartbeat раз в 15 с и teardown. Не отправляет ui/message и не читает очереди пользовательских чатов.
6. Существующий chat.ui.open(chat_id) и ChatRelayApp.html сохранены без изменений.

## Изолированные проверки

- MCP Server .NET 8 build: успешен без ошибок.
- Python контракт: metadata Global/Thread, корректные отдельные ui:// ресурсы, валидаторы событий, технический журнал, отсутствие изменений рабочей очереди.
- JS iframe emulation: handshake, host-context-changed, visibilitychange, teardown, запрет неожиданных ui/message.
- Регрессия: test_mcp_stdio, test_chat_ui_mcp, test_multichat_routing, test_chat_ui_bridge, test_multichat_widget, test_router_lite, test_router_reconnect.

## Практическая приёмка в обычном ChatGPT

A. Обновить инструменты плагина на DOMKAT. Из навигации ChatGPT вручную открыть глобальную точку входа без просьбы модели. В журнале проверить tool-open → resource-read → ui-initialize → ui-initialized → heartbeat.

B. Через вкладку беседы открыть Thread entrypoint. Проверить отдельный instance_id и mode=thread.

C. Для deep link получить фактический plugin ID из установленного плагина и проверить передачу маршрута в hostContext с hasDeepLink=true, изменение через host-context-changed.

D. Закрыть тестовую вкладку, проверить teardown и прекращение heartbeat.

E. Проверить запуск глобальной точки входа через Windows URI при закрытом браузере. Доказательством будет ui-initialize, ui/message и последующий chat.reply, не просто открывшаяся вкладка.

F. Повторить для двух локальных чатов, затем после перезагрузки Windows при сохранении пользовательского профиля.

## Безопасность эксперимента

Если для проверки E требуется закрыть весь Яндекс Браузер, сначала проверить незавершённую работу и сохранение вкладок. Не делать принудительный Stop-Process всего браузера без необходимости. Не удалять переписку, маршруты, вложения и файлы пользователя. Старый чат v0.2.18 остаётся доступен.

Документация:
https://github.com/openai/mcp-extensions/blob/main/docs/spec.md
https://github.com/modelcontextprotocol/ext-apps/blob/main/specification/2026-01-26/apps.mdx

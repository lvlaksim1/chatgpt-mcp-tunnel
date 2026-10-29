# ChatGPT MCP Tunnel v0.2.24 — возврат обычных чатов вместо Work

Дата: 30.10.2026. Тип: исправление архитектуры запуска новых диалогов.

## Подтверждённый дефект v0.2.23

Диалоги из Windows через Global MCP Apps (chat.hub.open по URL /mcp-app/...) получали поведение Work/Astra, хотя верхняя кнопка ChatGPT и ссылка /c/ сами по себе не определяют источник модельной квоты. Это подтверждено при просмотре тестового диалога в Яндекс Браузере: GPT-6 Astra, время выполнения и область результатов.

В v0.2.18 новый диалог создавался через ui/message target:new из компонента внутри обычной беседы ChatGPT (/c/...), и использовал обычный чат. Спецификация OpenAI MCP Extensions не предоставляет для ui/message самостоятельного параметра принудительного режима модели. Запрос наследует контекст вызывающего ChatGPT Host. Web в таблице Extensions относится к Work browser, а не к классическому ChatGPT.

## Исправление v0.2.24

- Добавлен model-invoked chat.bootstrap.open без Global/Thread entrypoint с ресурсом ui://chatgpt-mcp-tunnel/chat-bootstrap-v0224. Это одноразовое подключение транспортного iframe к обычной беседе ChatGPT.
- HTML использует проверенный диспетчер очереди: chat.ui.pending(discover:true) → ui/message target:new → chat.ui.ack, с автозапуском без кнопок и действий пользователя.
- Прежний chat.hub.open теперь использует ui://chatgpt-mcp-tunnel/hub-observer-v0224. Global entrypoint только показывает статус и отправляет heartbeat, но НЕ инициирует диалоги ChatGPT. Это предотвращает Work-трафик через Global.
- Экземпляры обычного чата маркируются chatbridge-*. Windows-активатор принимает только их heartbeat. Необслуживающий Global не сможет препятствовать восстановлению.
- ChatHostActivator открывает только проверенный URL обычной беседы https://chatgpt.com/c/<uuid>. Прежний /mcp-app/.../chat.hub.open отвергается.
- Приватный адрес якорной беседы хранится в %LOCALAPPDATA%\ChatGPTMCPAgent\Chat\hub-entrypoint.url, не попадает в Git.
- Для обновлённого JavaScript используются новые версионированные ui:// URI.
- Существующая история и вложения не менялись. Специальное сохранение старых чатов не добавлялось.

## Изолированные проверки

1. MCP tools/list: 57 инструментов; chat.bootstrap.open имеет только model+app visibility без global/thread entrypoints.
2. JavaScript эмуляция chat-bootstrap: автоматический polling, ui/message target:new, однократный ACK без дублей.
3. JavaScript эмуляция Global observer: ни polling, ни ui/message.
4. WinForms selftest: /c/ допустим, /mcp-app/ отвергнут, heartbeat фильтруется по chatbridge.
5. Регрессия: MCP tools, DOMKAT/MKATUSHONOK, UIA, Router Lite reconnect, multi-chat.

## Обязательная реальная приёмка

1. Установить v0.2.24 после CI release.
2. В существующей обычной беседе ChatGPT один раз вызвать chat.bootstrap.open. Убедиться, что iframe функционирует и его instance_id начинается с chatbridge-.
3. Сохранить её настоящий URL /c/<uuid> в пользовательский hub-entrypoint.url.
4. Полностью закрыть browser.exe, оставив работающим туннель.
5. Из локального Windows-чата отправить новое сообщение и проверить автоматическое открытие якорной беседы /c/ без действий пользователя.
6. Проверить, что модель создала новый ОБЫЧНЫЙ ChatGPT диалог (не Work/Astra), ответила через chat.reply и локальное сообщение получило answered.
7. Проверить второе сообщение и следующий холодный старт.

Критическая проверка: iframe bootstrap должен автоматически восстановиться при повторном открытии обычной беседы. Если нет — исследовать другой механизм и НЕ возвращаться к Work.

## Нормативные источники

https://github.com/openai/mcp-extensions/blob/main/docs/spec.md
https://developers.openai.com/plugins/build/extensions

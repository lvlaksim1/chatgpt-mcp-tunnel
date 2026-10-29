# MCP Apps Global/Thread — практическая приёмка v0.2.20

Дата: 29.10.2026. Компьютер: DOMKAT. Хост: обычный ChatGPT в Яндекс Браузере. Work, Codex и отдельный API не использовались.

## Выпуск и установка

- GitHub Actions: https://github.com/lvlaksim1/chatgpt-mcp-tunnel/actions/runs/40946906731 — success.
- Release: https://github.com/lvlaksim1/chatgpt-mcp-tunnel/releases/tag/v0.2.20.
- Пакет обновления: SHA-256 2295F7FC956039CA0B503605E525E7F44178B6C778C362CF03BA14D280EEF753 (размер 104967145 байт).
- Установка завершилась кодом 0; версия 0.2.20.0, /readyz=200, задача Windows ChatGPT MCP Tunnel Recovery=Ready; DOMKAT и MKATUSHONOK подключены.
- По решению владельца старые локальные чаты ценности не имеют: в дальнейшем не создавать специальных резервных и миграционных механизмов ради них. При испытаниях существующая история не удалялась.

## Проблема v0.2.19 и исправление

В v0.2.19 инструменты Global/Thread имели UI visibility=[app]. Обычный ChatGPT показал предупреждение о приватных инструментах без виджетов. В v0.2.20 переключено на visibility=[model,app], старый chat.ui.open остался обычным model-invoked bootstrap без static entrypoints. При обновлении инструментов и перезагрузке страницы предупреждение исчезло.

## Протокол: Global

Реальный адрес открытого global UI:

https://chatgpt.com/mcp-app/asdk_app_6ae131ee666c8191b89ee9bbfd947f52/chat.hub.open

29.10.2026 в 20:30:42–20:30:45 UTC сервер записал tool-open, resource-read, ui-initialize, tool-input, tool-result, ui-initialized. Instance: ui-u348e7q3j3-mvtzzwg3.

Содержимое отфильтрованного hostContext/capabilities:
- protocol=2026-01-26, host=chatgpt, mode=global, displayMode=fullscreen;
- hasMessage=true; hasOpenAiMessage=true; hasContextUpdate=true;
- hasDeepLink=true, pathKind=route;
- поле hasTools=false в диагностическом декодере, но tools/call фактически сработал для журнала событий; не считать это доказательством отсутствия поддержки tools/call.

Global отправлял heartbeat примерно каждые 15 секунд. После переключения в другой ChatGPT-диалог продолжал heartbeat с visibility=hidden. Это доказательство активности скрытого, но ещё живого iframe.

## Протокол: Thread

В тестовой обычной беседе ChatGPT был вызван именно chat.thread.open без аргументов. В 20:35:35–20:35:39 UTC сервер записал tool-open, resource-read, ui-initialize, ui-initialized, tool-input, tool-result. Instance: ui-k5hk40wihv-mvu066yx.

Контекст: host=chatgpt, mode=thread, displayMode=inline; hasMessage=true, hasOpenAiMessage=true, hasContextUpdate=true, hasDeepLink=false (для этой беседы). Thread отправлял heartbeat как visible, так и hidden. Не путать этот тест model-invoked chat.thread.open с отдельным ручным открытием статического Thread entrypoint через вкладку: это отдельный сценарий для следующей приёмки.

## Повторный запуск Global после закрытия вкладки

1. В Яндекс Браузере закрыта ТОЛЬКО вкладка с тестовым Global. Другие пользовательские вкладки не закрывались.
2. Из Windows PowerShell вызвано системное Start-Process по адресу:

https://chatgpt.com/mcp-app/asdk_app_6ae131ee666c8191b89ee9bbfd947f52/chat.hub.open?path=%2Fprobe%2Fwindows-launch

3. Запущена новая вкладка того же работающего Яндекс Браузера.
4. В 20:36:58–20:37:00 UTC сервер записал новый tool-open и ui-initialize, новый instance_id ui-6itesnxbbs-mvu07ygq, затем tool-input, tool-result и ui-initialized.
5. hasDeepLink=true: маршрут Windows поступил в hostContext. Старый экземпляр не требовался.

Таким образом системный Windows deep link уже умеет активировать Global после закрытия вкладки, при условии что браузерный хост в системе ещё работает.

## Границы текущего эксперимента и следующий шаг

- **Подтверждено:** модельный запуск не нужен для Global; Thread работает как отдельный экземпляр; оба используют MCP tools/call и host lifecycle; Global сохраняет активность в скрытой вкладке; Windows может открывать Global через URI; после закрытия тестовой вкладки новая инициализация происходит с новым instance_id.
- **Не проверено:** запуск с нуля при полностью завершённых процессах браузера; автоматическая генерация ответа после такого запуска; контекст после reboot; ручное открытие статического Thread entrypoint без вызова моделью.
- Пользовательский Яндекс Браузер содержит множество несвязанных вкладок. Они не закрывались ради эксперимента, чтобы не уничтожить несохранённые данные других приложений.
- **Следующая техническая разработка:** Global Hub с автоматическим чтением очереди и отправкой ui/message target:new (без кнопок и действий в интерфейсе ChatGPT), плюс локальные капсулы контекста для новых thread. Для теста полной независимости хоста нужен отдельный контролируемый сеанс/профиль, позволяющий полностью завершать его процессы без вмешательства в пользовательские вкладки.

Журнал событий: %LOCALAPPDATA%/ChatGPTMCPAgent/Chat/lifecycle-probe/events.jsonl.

Источники спецификаций:
- https://github.com/modelcontextprotocol/ext-apps/blob/main/specification/2026-01-26/apps.mdx
- https://github.com/openai/mcp-extensions/blob/main/docs/spec.md

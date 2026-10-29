# Chat in ChatGPT MCP Tunnel (v0.2.13)

## Scope
The tray application has a new **Чат** command (and tray double click).
The independent Windows Forms window supports typing messages, Enter to send
(Shift+Enter for newline), and an on-disk conversation log.

This is not a web browser, Codex, Work, or a paid model API. A connected ChatGPT
session must read the queue through MCP and write responses through MCP.
Messages do **not** initiate a new ChatGPT turn by themselves.

## MCP protocol
- `chat.receive(wait_seconds=0..45, max_messages=1..30)`: read pending
  messages, optionally long poll, return `count` and `messages`.
- `chat.reply(message_id, reply)`: save the reply under the supplied message ID.
  A second reply to an answered message is idempotent.

On the DOMKAT server, the tray application and MCP runtime share the queue
in `%LOCALAPPDATA%\ChatGPTMCPAgent\Chat\messages`.
Each user-written message is persisted atomically as a separate JSON file.
Messages not yet answered remain pending after restarts. Responses are
saved in the same record. This first version addresses the local server
window, not remote-agent windows.

## Hourly automation
An hourly ChatGPT automation can invoke `chat.receive` and `chat.reply`
for as long as each execution is permitted. It cannot guarantee a
continuously active 60-minute model turn; tasks are bounded by ChatGPT
runtime limits. When the next run starts, the outstanding messages remain
available in the queue.

## Security
Message content and replies are local plaintext in the current Windows
user profile; they are not stored in Git. Do not paste passwords or
secrets in the chat window. No browser sign-in is copied or reused.

## Verification
- `dotnet build src/ChatGptMcpAgent/ChatGptMcpAgent.csproj -c Release`
- `dotnet build src/ChatGptMcpTunnel/ChatGptMcpTunnel.csproj -c Release`
- `ChatGptMcpAgent.exe --test-chat-window`
- `python tests/test_mcp_stdio.py` (includes queued-message exchange)

## Cyclic five-minute activity (v0.2.13)

The hourly ChatGPT automation starts with chat.cycle(action="hello", signal="я тут").
It receives a unique session_id and task_id, plus a 300-second diagnostic assignment.
The model repeatedly calls chat.cycle(action="step", session_id, task_id, wait_seconds=35).
Each step samples total CPU utilization, memory pressure, process count, and critical
executables. Pending user messages are returned by step; the assistant responds
with chat.reply before continuing the cycle.

After 300 seconds, chat.cycle(action="complete", session_id, task_id, report="...")
saves the assistant's diagnostic report in the chat history and issues the next
five-minute assignment in the same MCP result.

Session state is persisted at
%LOCALAPPDATA%/ChatGPTMCPAgent/Chat/cycle-status.json.
When 120 seconds elapse without a step, the app marks the session offline.
The server cannot initiate a new ChatGPT turn; the hourly automation starts
the next run. A new hello replaces the previous session token.

Calls wait at most 40 seconds; the monitor is a sequence of bounded samples.
The test fast-forwards expiration only in an isolated temporary folder,
never in the production data directory. ChatGPT execution remains subject
to independent platform limits, so an hour of continuous uptime is not guaranteed.


## Вложения в сообщения (v0.2.14)

В окне «Чат» доступны кнопка «📎», выбор нескольких файлов, перетаскивание файлов
из Проводника и вставка изображения из буфера обмена клавишами Ctrl+V.
Перед отправкой отображаются карточки вложений с именем, размером, миниатюрой
(для изображений) и кнопкой удаления. Сообщение может содержать только
вложения без текста. В одном сообщении поддерживается до 20 файлов,
каждый размером не более 2 ГиБ.

При нажатии «Отправить» файлы копируются в папку
%LOCALAPPDATA%\ChatGPTMCPAgent\Chat\attachments\[message_id]\[attachment_id],
а метаданные (Id, Name, MediaType, SizeBytes, Sha256) сохраняются в
JSON сообщения. Запись становится видимой в очереди только после
успешного сохранения всех вложений; копирование выполняется вне потока
интерфейса. Исходный файл после отправки больше не нужен.

Инструмент chat.attachment.read(message_id,attachment_id,max_chars) возвращает:
- текстовые файлы в ограниченном текстовом блоке;
- изображения в настоящем MCP-блоке изображения (image/png), с уменьшением
  до 1600 пикселей по большей стороне;
- другие форматы (PDF, DOCX, XLSX, ZIP, большие двоичные файлы): метаданные
  и проверенный локальный путь для дальнейшей обработки на DOMKAT без
  помещения содержимого в текст сообщений модели.

chat.receive отдаёт метаданные вложений вместе с текстом, без двоичных
данных. При чтении проверяются размер и SHA-256. Неверный идентификатор,
отсутствующий или изменённый файл отклоняются. Данные не публикуются на
GitHub и не отправляются в отдельное облако. В пользовательском профиле
Windows вложения сохраняются без дополнительного шифрования.

Ограничение ChatGPT: локальное сообщение само не запускает ответ модели.
Активная модель или почасовой планировщик должны вызвать chat.receive,
при необходимости chat.attachment.read, а затем chat.reply.
Поскольку ChatGPT может сохранять предыдущий перечень инструментов,
для новой команды может потребоваться обновление подключения плагина.

Проверки:
- ChatGptMcpAgent.exe --test-chat-attachments — карточки, копирование
  двух вложений, SHA-256 и сохранённые метаданные.
- python tests/test_chat_attachments.py — MCP-приём, UTF-8, изображение,
  отказ для неверных ID / подмены SHA-256 и ответ с сохранением вложений.

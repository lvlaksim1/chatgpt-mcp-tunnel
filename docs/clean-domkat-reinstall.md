# Чистая переустановка DOMKAT с сохранением существующего OpenAI Tunnel ID

**Правило:** логический OpenAI-туннель не создаётся и не удаляется. В ChatGPT сохраняется прежний плагин и Tunnel ID. Переустанавливается только локальная программа.

## Состояние до замены

Старый туннель запущен из `D:\ChatGPT-MCP-Tunnel` через задание Windows `ChatGPT MCP Tunnel`; репозиторий содержит исходный код и должен остаться на месте. Старый пароль защищён средствами Windows в `%LOCALAPPDATA%\ChatGptMcpTunnel`.

## Подготовка (без остановки старой программы)

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File D:\ChatGPT-MCP-Tunnel\scripts\Prepare-Clean-Reinstall-DOMKAT.ps1 -Version 0.2.1
```

Этот сценарий проверяет существующий Tunnel ID, скачивает полный установщик из официального GitHub Releases `lvlaksim1/chatgpt-mcp-tunnel`, сверяет SHA256, когда digest опубликован, и создаёт в `D:\ChatGPTMCP-Migration` автономный `Start-Clean-Install.cmd`. Никакие ключи OpenAI не копируются. На этом этапе старый туннель остаётся подключённым.

## Запуск чистой установки

После готовности комплекта пользователь запускает **`D:\ChatGPTMCP-Migration\Start-Clean-Install.cmd`** непосредственно на DOMKAT. Это отдельный процесс, который не зависит от текущего MCP-подключения ChatGPT.

Он отключает и удаляет старое задание автозапуска, останавливает старый `tunnel-client.exe`, удаляет прежнюю локальную авторизацию и профили, оставляет исходники Git в неприкосновенности, устанавливает **тот же продукт** в `D:\ChatGPTMCPAgent` с ролью «Сервер» и автозапуском. Затем переносит только неизменный Tunnel ID в новые настройки и открывает окно программы.

**После остановки старого клиента подключение ChatGPT к компьютеру временно прекращается.** Результаты автономного сценария записываются в `D:\ChatGPTMCP-Migration\clean-install-result.log`. Если возникла ошибка, следует прочитать журнал и исправить причину; сценарий не пытается создать новую регистрацию OpenAI.

## Ввод токена

В серверном окне уже указан существующий Tunnel ID. Пользователь указывает свой новый **токен доступа к этому туннелю**, локальный IP-адрес и сохраняет настройки. Токен защищается DPAPI. Отдельно программа генерирует ключ подключения удалённых клиентов Router Lite (это локальный секрет агентов, **не новый OpenAI Tunnel**).

После подключения проверяем 42 MCP-инструмента и вход клиента MKATUSHONOK.

## Уточнения

- Удалять или создавать объекты туннелей в OpenAI Platform не требуется.
- Не меняем ID существующего туннеля и не регистрируем новый плагин в ChatGPT.
- До подтверждённой сборки и автономной подготовки старое подключение не трогаем.
- Права на управление брандмауэром Windows для порта 55201 могут потребовать дополнительной настройки в доверенной локальной сети.

## Recovery of installed v0.2.1 (28.10.2026 MSK)
After the clean DOMKAT installation, the OpenAI Tunnel ID and new runtime credentials were correct. However, the server launcher invoked the native tunnel-client with 2>&1 under PowerShell 5.1 ErrorActionPreference=Stop. MCP informational startup messages in stderr were interpreted as terminating NativeCommandError and the launcher plus tunnel-client quit about every seven seconds.

Via restored Remote Desktop Commander this was reproduced independently, then the installed D:\ChatGPTMCPAgent\ServerStart.ps1 was corrected to run tunnel-client directly under temporary ErrorActionPreference=Continue, with exit-code checking. No cloud tunnel creation, token replacement, or reinstall was needed.

Post-fix verification: original ChatGPT plugin bridge_ping=CHATGPT_MCP_TUNNEL_PING_OK, system.info=DOMKAT, GitHub authenticated, Excel windows listed, 42 tools in local MCP list, Router Lite listening on 192.168.0.3:55201, healthz and readyz at port 55198 HTTP 200. Source corrected for upcoming v0.2.2.

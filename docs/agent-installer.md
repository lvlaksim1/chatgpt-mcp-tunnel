# ChatGPT MCP Agent — установка и обновление

Дата: 28.10.2026. Архитектурный образец: проверенные исходники MailRu Desktop, `installer/full.iss`, `installer/update.iss`, процесс сборки GitHub Releases, отдельный помощник обновления.

## Назначение

ChatGPT MCP Agent работает на дополнительном компьютере Windows 10/11 x64 и подключается по исходящему WebSocket к одному Router Lite на DOMKAT. Отдельный OpenAI MCP Tunnel на каждом компьютере **не требуется**. Внутри установленного агента лежит штатный исполненный MCP Runtime, умеющий 23 локальных инструмента Windows.

## Установщики

- `ChatGPT_MCP_Agent_Setup_vX.Y.Z.exe`: первая установка с обычным мастером. Не нужны права администратора, .NET Runtime или SDK. После установки запускается окно первоначальной настройки, ярлык в меню «Пуск» создаётся автоматически, значок на рабочем столе — по выбору.
- `ChatGPT_MCP_Agent_Update_vX.Y.Z.exe`: только поверх существующей установки. Скрытый минимальный мастер; обновление не изменяет конфигурацию или значки на рабочем столе. После обновления агент запускается снова.

Оба установщика собираются Inno Setup 6 по схеме MailRu Desktop, но с отдельным AppId. Из репозитория публикуются **только исходники**; бинарные EXE хранятся в GitHub Releases.

## Каталоги и настройки

- Исполняемые файлы: `%LOCALAPPDATA%\Programs\ChatGPTMCPAgent`
- Исполняющий модуль: `%LOCALAPPDATA%\Programs\ChatGPTMCPAgent\Runtime`
- Настройки: `%LOCALAPPDATA%\ChatGPTMCPAgent\settings.json`
- Ключ: `%LOCALAPPDATA%\ChatGPTMCPAgent\credential.dpapi` (Windows DPAPI, CurrentUser)
- Журналы: `%LOCALAPPDATA%\ChatGPTMCPAgent\Logs`
- Временные загрузки обновления: `%LOCALAPPDATA%\ChatGPTMCPAgent\Updates`
- Автозапуск: пользовательский `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\ChatGPTMCPAgent` (в командной строке **нет ключа подключения**).

Установщик не записывает изменяемые данные в каталог программы. При обновлении данные сохраняются. При полном удалении удаляются программные файлы, созданные ярлыки, автозапуск, папки настроек/журналов/ключей и собственный раздел реестра. Секрет не попадает в Git, ярлыки или реестр.

## Окно и трей

При первом запуске: имя компьютера (по умолчанию Windows computer name), адрес маршрутизатора `wss://router.example/agent` либо адрес зашифрованной частной сети вида `ws://100.x.y.z:55201/agent`, ключ подключения, автозапуск при входе. Агент остаётся в трее; двойной щелчок открывает настройки. Контекстное меню: «Настройки подключения», «Переподключиться», «Проверить обновления», «Выход». Разрыв соединения обрабатывается штатным переподключением; при аварийном завершении дочернего процесса приложение запускает его вновь.

Для работы с мышью, клавиатурой, скриншотами и UI Automation запуск выполняется **в пользовательском сеансе**, а не в службе Windows Session 0. Чтобы агент мог управлять рабочим столом, пользователь должен войти в Windows.

## Обновления

Агент проверяет опубликованный `releases/latest` в `lvlaksim1/chatgpt-mcp-tunnel`, предлагает новую версию и скачивает только URL вида `https://github.com/lvlaksim1/chatgpt-mcp-tunnel/releases/download/vX.Y.Z/ChatGPT_MCP_Agent_Update_vX.Y.Z.exe`. Для обновления текущий процесс копирует помощника в папку данных. Помощник показывает прогресс, скачивает установщик, через локальный именованный канал сообщает основной программе, когда загрузка завершена; основная программа корректно завершает агента и закрывается. Помощник ждёт её завершения и запускает Inno Setup с `/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS`. Новая программа стартует после завершения установки, а прежняя конфигурация остаётся в силе.

Это соответствует раздельному обновлению MailRu Desktop без диалогов PowerShell, без отдельной установки .NET.

## Сборка

На Windows с .NET 8 SDK:

```powershell
dotnet publish src/ChatGptMcpAgent/ChatGptMcpAgent.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:Version=0.1.0 -o publish
dotnet publish src/ChatGptMcpTunnel/ChatGptMcpTunnel.csproj -c Release -r win-x64 --self-contained true -p:Version=0.1.0 -o publish/Runtime
& publish/ChatGptMcpAgent.exe --selftest
```

На CI GitHub Actions: `.github/workflows/agent-installer-release.yml` (ручной запуск, вход `version` вида `0.1.0`). После сборки и автоматических тестов Inno Setup создаёт Setup и Update, запускает их на чистом тестовом Windows-пользователе, проверяет сохранность данных при обновлении, удаление через uninstaller, публикует оба EXE в GitHub Release. Старые релизы **только агентской** программы удаляются после удачного нового выпуска; чужие GitHub Releases не трогаются.

## Доступность маршрутизатора

В установленном агенте настройки не заменяют запуск центрального маршрутизатора на DOMKAT. Там должен работать один официальный OpenAI tunnel-client с включённым `CHATGPT_MCP_ROUTER_ENABLED=1` и тем же ключом в `CHATGPT_MCP_AGENT_TOKEN`; адрес `CHATGPT_MCP_ROUTER_LISTEN` должен быть доступен дополнительному компьютеру через защищённую сеть. Общее имя ключа в текущем прототипе — временная минимальная архитектура. Перед расширением доверия на несколько компьютеров желательно индивидуализировать ключи.

Нельзя открывать обычный `ws://` в публичный Интернет. Для публичной сети используйте `wss://`, для Tailscale — частное сетевое соединение. Компьютеры пока реально не объединялись в двухфизический контур.

## Статус проверки

28.10.2026: локально на DOMKAT собраны self-contained Windows x64 GUI и runtime, пройдены `--selftest` (DPAPI, проверка адресов), `tests/test_mcp_stdio.py`, `tests/test_router_lite.py` и `tests/test_router_reconnect.py` на собранном runtime. Реальная автоматическая сборка и установка через Inno Setup будет подтверждена отдельно в GitHub Actions, проверка подключения на втором физическом ПК ещё не проводилась.

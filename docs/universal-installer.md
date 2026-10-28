# Универсальный установщик ChatGPT MCP Tunnel

Решение пользователя от 28.10.2026: одна полная установочная программа для основного и удалённых Windows-компьютеров, различие определяется ролью во время установки.

## Мастер установки
- Роль «Сервер»: основной компьютер с единственным туннелем OpenAI, маршрутизатором Router Lite и локальными инструментами Windows.
- Роль «Клиент»: дополнительный компьютер, исходящее подключение к маршрутизатору, без второго туннеля OpenAI.
- Галочка «Запускать автоматически при входе в Windows» — по умолчанию отмечена, пользователь может снять. Это отдельная задача установки, работает для любой роли.
- Галочка «Создать значок на рабочем столе» — необязательная.
- Установка в профиль текущего пользователя, без требований администраторских прав и установленного .NET.

После установки выбор роли хранится в HKCU\Software\ChatGPTMCPAgent. Выбор автозапуска отражается в HKCU\Software\Microsoft\Windows\CurrentVersion\Run. Автозапуск происходит при входе в Windows, а не до входа пользователя. Обновление сохраняет оба значения и ключи.

## Настройка
Сервер настраивает ID OpenAI Tunnel, адрес локального интерфейса (например, 192.168.0.3) и секрет агентов. Для первого сервера секрет генерируется и защищается DPAPI; при необходимости можно скопировать его из окна настройки. На DOMKAT программа распознаёт сохранённые ID и прежний DPAPI-ключ OpenAI без публикации в GitHub. Если прежний tunnel-client уже запущен, второй туннель не запускается: миграция требует отдельного контролируемого переключения.

Клиент вводит название компьютера, адрес маршрутизатора и ключ подключения. Существующий MKATUSHONOK получает обновление с сохранением настроек.

## Сборка и приёмка
Версия 0.2.0: один полный установщик ChatGPT_MCP_Tunnel_Setup_v0.2.0.exe и один обновляющий ChatGPT_MCP_Tunnel_Update_v0.2.0.exe. Для совместимости с ранее установленным клиентом также публикуется второй URL того же обновления под прежним именем ChatGPT_MCP_Agent_Update_v0.2.0.exe.

Сборка Inno Setup через GitHub Actions; для серверной роли официальный полный OpenAI tunnel-client v0.0.18 берётся из открытого релиза с проверкой SHA256. Проверяются обе роли, выбор автозапуска, сохранение роли при обновлении, установка и удаление. Действующий OpenAI Tunnel на DOMKAT не перезапускается ради проверки установщика.

## Сетевое ограничение
Серверный слушатель Router Lite привязывается к выбранному локальному IPv4 и порту 55201. На Windows при необходимости нужно разрешить соединение через брандмауэр в доверенной частной сети. Для подключений через Интернет требуется защищённая сеть или WSS.

## Выбор папки установки

После выбора роли «Сервер» или «Клиент» мастер показывает стандартную страницу выбора папки с кнопкой «Обзор». Разрешён произвольный доступный каталог.

Сервер: D:\ChatGPTMCPAgent по умолчанию, если D: доступен; при отсутствии D: — безопасный пользовательский каталог. Клиент: %LOCALAPPDATA%\Programs\ChatGPTMCPAgent, обычно расположенный на диске C:.

Ручной выбор не перезаписывается при переключении роли, а InstallDir хранится в HKCU\Software\ChatGPTMCPAgent. Обновление использует сохранённый путь, как и удаление. Старый клиент v0.1.0 обнаруживается по прежнему каталогу.

Windows CI проверяет серверный и клиентский пути по умолчанию, пользовательский путь клиента, обновление и удаление в нём.

## Результат приёмки v0.2.0 (28.10.2026)
GitHub Actions 40696034380 завершился успешно. В релизе v0.2.0 доступны универсальный Setup, Update и совместимый обновляющий файл для прежнего клиента. Реальным запуском Inno Setup проверены оба режима, выбор папки, серверный и клиентский пути по умолчанию, пользовательский путь клиента, сохранение каталога при обновлении и удаление. Существующий DOMKAT ещё работает на старой версии: чистая переустановка будет отдельной операцией.

## Existing OpenAI Tunnel ID only
The server role never creates a new OpenAI cloud tunnel. It creates only a fresh local tunnel-client configuration pointing at the pre-existing Tunnel ID. After clean local reinstall the Owner enters a new runtime key into the UI. See docs/clean-domkat-reinstall.md.

## Windows icon, server status and Settings repair in v0.2.3
On DOMKAT v0.2.2 the tray displayed «Нет соединения» despite the original bridge ping, readyz HTTP 200, and router port 55201 all being healthy. The source had incorrectly applied client stderr status detection to the Server role. The new Server status uses bounded asynchronous HTTP readiness and local Router Lite TCP checks, while Client continues its separate logic.

The Settings menu was unresponsive. Win32 enumeration found an already-created but invisible Server Settings Form. The old implementation opened a modal ShowDialog during Application.Idle with a guard that blocked reopening. New code opens a modeless persistent window, reuses and foregrounds it, and correctly releases it when closed. A --settings local named event directs the running tray process to open its window without starting a second tunnel.

An original, high-contrast, multi-resolution Windows icon in Resources/App.ico is embedded in the application and used in both Inno installers and Settings forms. Connection-status dots are green when ready, amber while connecting, red on error. The build and icon tests are included in the Windows release workflow.

## Follow-up: guaranteed native settings-window visibility (v0.2.4)
After v0.2.3 installed, the new modeless Settings handler ran, but native Windows UI inspection still found a hidden Server Settings HWND (native WS_VISIBLE absent). A direct ShowWindow(hwnd, SW_SHOW) on DOMKAT immediately made the window visible, foregrounded with normal size. v0.2.4 therefore calls Win32 IsWindowVisible / ShowWindow(SW_SHOW) when necessary after Form.Show and when reactivating an existing Settings window. This change is limited to the GUI process and does not modify tunnel credentials or OpenAI registration.

## v0.2.5 — согласованные формы подключения (28.10.2026)
Подтверждённые пользователем HTML-макеты реализованы в Windows WinForms:
- Сервер: статус существующего OpenAI Tunnel без изменения ID в обычных настройках, отдельная кнопка «Сменить токен» (временная форма), адрес Router Lite без смены текущего 192.168.0.3, порт 55201 в виде текста, ключ клиентов управляется кнопками «Указать ключ», «Создать новый», «Копировать ключ». Копирование только на сервере, из DPAPI либо подготовленного к сохранению нового значения. При первом запуске ID может быть введён в первоначальной настройке.
- Клиент: имя ПК, адрес сервера без схемы/порта, автоматический порт 55201, статус ключа и кнопка «Указать ключ». Ни просмотр, ни копирование сохранённого ключа не доступны в интерфейсе клиента.
- В обеих формах секреты отсутствуют в постоянных полях. Ввод по нажатию кнопки открывает небольшую форму SecretInputDialog и закрывается при подтверждении/отмене. На основной форме выводится только статус. DPAPI секреты сохраняются только после нажатия «Сохранить» на основной форме; если пользователь изменил обычные настройки, ключи не перезаписываются.
- Кнопки «Проверить» делают сетевую проверку: на сервере local readyz и Router Lite, на клиенте TCP-доступность указанного сервера, без проверки клиентского ключа до фактического подключения.
- Работают прежние пользовательская иконка, надёжное Win32-отображение окна настроек, автозапуск и штатные обновления.
- Автоматические тесты: --selftest, --test-icons, --probe-server и --test-secret-forms. CI собирает полный и обновляющий Inno-установщики с проверкой сохранения настроек и удаления.

Владелец явно указал: установить обновление самостоятельно через штатные средства. Не обновлять приложение на DOMKAT автоматически. Текущий IP 192.168.0.3 сохраняется; вопрос статического DHCP-резервирования отложен.

## v0.2.6: устранение ошибки Windows при обновлении (28.10.2026)
- v0.2.5 ошибочно переименовывала загруженный файл .part, пока FileStream его записи ещё был открыт. Поэтому при 100% загрузки появлялась ошибка The process cannot access the file because it is being used by another process.
- v0.2.6 закрывает FileStream до File.Move, проверяет полную загрузку (ContentLength), очищает неполный .part и не перезаписывает исправный существующий установщик при обрыве.
- Тест --test-updater-download проверяет замену скачанного установщика, неполную загрузку, очистку .part, отсутствие оставленных открытых дескрипторов.
- CI Windows запускает этот тест перед выпуском. Из неисправной v0.2.5 требуется однократно вручную запустить v0.2.6 update EXE; после этого встроенный механизм обновления исправлен.
- По требованию владельца приложение DOMKAT не обновлять автоматически, текущий IP оставить 192.168.0.3.

## v0.2.6 — доступ клиента из локальной сети и ошибки подключения
- На DOMKAT (28.10.2026 ~20:20 МСК) Router Lite слушает 192.168.0.3:55201, OpenAI readyz отвечает HTTP 200. Wi-Fi Беспроводная сеть имеет профиль Public, default inbound Block, для приложения и TCP 55201 нет явного разрешения Windows Firewall. Доступность Router Lite извне нельзя утверждать без теста со второго компьютера.
- В настройках сервера добавлена кнопка «Разрешить доступ клиентам в локальной сети»: пользователь явно подтверждает действие и запрос Windows UAC; правило ограничено приложением Runtime/ChatGptMcpTunnel.exe, локальным адресом сервера, входящим TCP 55201 и удалённой подсетью текущего сетевого интерфейса (например 192.168.0.0/24). Глобальные профили брандмауэра и сеть не изменяются.
- На клиенте ошибки WebSocket теперь распознаются: HTTP 401 -> «Ключ отклонён», HTTP 409 -> «Имя компьютера занято», прочие ошибки соединения -> «Нет соединения», успешное соединение -> «Подключён». Это не заменяет проверку подключения с физического клиента.
- На сервере зафиксировано обновление защищённого файла общего ключа 28.10.2026 20:17:08 МСК. Если клиент хранит прежний общий ключ, требуется скопировать актуальный на сервере, указать его в клиенте, нажать «Сохранить и подключиться». Никакие секреты не помещать в журналы.
- Локальные тесты --test-lan-firewall и --test-client-errors завершены с кодом 0.

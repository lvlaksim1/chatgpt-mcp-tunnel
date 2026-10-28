# ChatGPT MCP Tunnel

**Независимый проект** для прямого вызова инструментов Windows из обычного ChatGPT через OpenAI Secure MCP Tunnel и протокол MCP.

Это самостоятельный сервер, **не является компонентом другого десктопного приложения и не использует чужие маршрутизаторы команд**.

## Состав

- `src/ChatGptMcpTunnel` — сервер MCP на .NET 8 и официальном C# MCP SDK.
- `scripts/Start-Tunnel.ps1` — подключение к OpenAI Secure MCP Tunnel.
- `scripts/Start-Tunnel.cmd` — запуск двойным нажатием в Windows.
- `tests/test_mcp_stdio.py` — локальный тест протокола без сторонних библиотек Python.
- `docs/E2E-2026-10-28.md` — результаты ранее выполненного испытания прямого вызова.

Сервер предоставляет **41 MCP-инструмент**: диагностический `bridge_ping`, восемь инструментов для системы, файлов и процессов, **11 базовых инструментов мультимедиа** (экран, окна, мышь, клавиатура) и **четыре инструмента Windows UI Automation**. Сквозные проверки выполнены через MCP на `DOMKAT`.

## Требования

- Windows 10/11, .NET SDK 8, Python 3 (для теста).
- Официальный `tunnel-client.exe` версии **v0.0.18** из [OpenAI tunnel-client](https://github.com/openai/tunnel-client/releases/tag/v0.0.18).
- Созданный в OpenAI Platform туннель и ключ с разрешениями Tunnels Read + Use.
- Пользовательский MCP-плагин в ChatGPT, привязанный к этому туннелю.

## Установка

Клонировать репозиторий в **отдельную папку**. Пример:

```powershell
git clone https://github.com/lvlaksim1/chatgpt-mcp-tunnel.git D:\ChatGPT-MCP-Tunnel
cd D:\ChatGPT-MCP-Tunnel
dotnet build src/ChatGptMcpTunnel/ChatGptMcpTunnel.csproj -c Release
python tests/test_mcp_stdio.py
```

Скопировать `tunnel-client.exe` из официального дистрибутива OpenAI в каталог:

`<корень репозитория>\.tools\tunnel-client-v0.0.18\tunnel-client.exe`

Каталог `.tools` игнорируется Git и предназначен исключительно для локального исполнения.

## Запуск

`scripts\Start-Tunnel.cmd` или:

```powershell
.\scripts\Start-Tunnel.ps1 -TunnelId tunnel_...
```

При первом запуске скрипт запросит идентификатор туннеля и секретный ключ. После проверки реального доступа сохраняются:
- не являющийся секретом Tunnel ID — в профиле пользователя;
- ключ — только как **зашифрованный текущим пользователем Windows DPAPI** файл;
- диагностика и локальная конфигурация — вне Git.

Для последующих запусков повторно вводить ключ не нужно. Файлы учетных данных пользователя не помещаются в GitHub.

`doctor` проверяет конфигурацию; скрипт также отдельно проверяет доступ к метаданным конкретного туннеля. Локальная панель диагностики постоянно доступна по адресу `http://127.0.0.1:55198/ui`, а проверки `/healthz` и `/readyz` — на том же адресе. Все эти адреса доступны только на компьютере, где работает сервер.

Один экземпляр туннеля должен иметь **один активный** локальный клиент; перед переходом на новый каталог следует остановить прежний процесс `tunnel-client.exe`.

## Инструменты Windows

| MCP-инструмент | Назначение |
|---|---|
| `system.info` | Windows, ЦП, оперативная память, диски, время работы; по запросу — установленные программы |
| `fs.list` | Список содержимого каталога |
| `fs.read` | Чтение файлов: поле `text` для текста, поле `base64` для двоичных данных; размер и признак обрезки |
| `fs.write` | Создание или перезапись текстового файла |
| `fs.search` | Поиск файлов по маске с обходом вложенных папок |
| `shell.exec` | Выполнение команд PowerShell и CMD с возвратом вывода, кода завершения и поддержкой тайм-аута |
| `process.list` | Список процессов с идентификаторами и потреблением памяти |
| `process.kill` | Завершение процесса по идентификатору |
| `bridge_ping` | Проверка связи с сервером |

### Категория «Мультимедиа»

Все **15** графических инструментов (11 базовых и 4 UI Automation) относятся к разговорной категории **«Мультимедиа»**. Можно писать в ChatGPT просто: **«Используй мультимедиа: сделай снимок экрана»**, **«Через мультимедиа открой нужное окно»**, **«Мультимедиа — введи текст»**. Модель должна выбрать нужный инструмент по его русскому названию и описанию.

Эта категория реализована в стандартных MCP-полях `title` и `description` каждого инструмента, поскольку MCP не предусматривает обязательной отдельной папки категорий в интерфейсе клиентов. Имена и параметры прежних 11 инструментов `desktop.*` **не изменены**. Новые UI Automation инструменты имеют отдельные имена `desktop.uia.*`. Всего сервер содержит **41** инструмент (24 прежних и 17 GitHub).

| MCP-инструмент | Назначение |
|---|---|
| `desktop.screen.info` | Размеры экранов, положение курсора и активное окно |
| `desktop.screen.capture` | PNG-снимок всех экранов или выбранной области; изображение возвращается через MCP |
| `desktop.window.list` | Список видимых окон с заголовками, PID, координатами и идентификаторами |
| `desktop.window.activate` | Перевод окна на передний план, восстановление свёрнутого |
| `desktop.window.move` | Перемещение и изменение размера окна |
| `desktop.window.close` | Штатный запрос закрытия окна (сохранение документа решает приложение) |
| `desktop.mouse.move` | Перемещение указателя |
| `desktop.mouse.click` | Щелчок левой, правой или средней кнопкой, двойной щелчок |
| `desktop.mouse.scroll` | Вертикальная прокрутка |
| `desktop.keyboard.type` | Ввод текста Unicode в активное приложение |
| `desktop.keyboard.hotkey` | Отправка клавиш и сочетаний, например `CTRL+S`, `ENTER`, `ALT+TAB` |

### UI Automation — работа с элементами интерфейса

| MCP-инструмент | Назначение |
|---|---|
| `desktop.uia.tree` | Получить дерево доступных элементов окна: имена, типы, идентификаторы, координаты, путь |
| `desktop.uia.find` | Найти элементы по названию, `AutomationId`, типу или классу |
| `desktop.uia.get` | Прочитать свойства, текст, значение и поддерживаемые действия элемента |
| `desktop.uia.action` | Вызвать действие элемента: `invoke`, `set_value`, `select`, `toggle`, `expand`, `collapse`, `focus`, `scroll_into_view` |

Использование: сначала `desktop.window.list` для получения `hwnd` нужного окна, затем `desktop.uia.tree` либо `desktop.uia.find`; возвращённый путь `path` передать в `desktop.uia.get` или `desktop.uia.action`.

Режимы обхода: у `desktop.uia.tree` и `desktop.uia.find` есть параметр `view="control"` (по умолчанию) или `view="raw"`. При чтении или выполнении действия обязательно передавать **тот же `view`**, в котором найден `path`: идентификаторы пути различаются между представлениями. `RawView` раскрывает дополнительные элементы, **если приложение их предоставляет**.

Совместимость Windows Forms: если `UIAutomationClient` видит стандартный элемент как `Pane` без шаблонов, исполнитель отдельно читает Microsoft Active Accessibility (MSAA) через штатную `oleacc.dll`. Определение `type` уточняется по роли MSAA (`type_source="msaa"`), исходный тип сохраняется в `uia_type`. `desktop.uia.get` показывает `msaa`, `native_hwnd`, `patterns`, `available_actions` и `client_side_providers`. При выполнении `desktop.uia.action` сначала используется штатный UIA-шаблон, а если его нет — поддерживаемое действие MSAA; поле `backend` указывает реально применённый механизм. Типы и действия **не назначаются догадкой по визуальному виду**.

Примечание: управляемый `UIAutomationClient` на используемом .NET Framework не раскрывает `LegacyIAccessiblePattern` напрямую. Поле `legacy_iaccessible` фиксирует это ограничение; поле `msaa` отражает независимое чтение соответствующего `IAccessible` по HWND. Штатные клиентские поставщики Windows зарегистрированы платформой; явная регистрация встроенной библиотеки не нужна. В Калькуляторе `RawView` по результатам испытаний может по-прежнему возвращать только корень — данные о внутренних элементах невозможно восстановить одним переключением режима обхода.
 Путь представляет индексы элементов в дереве, например `0/1`; при перестройке интерфейса путь может измениться, поэтому после изменений структуры лучше найти элемент заново.

Внутри сервера UI Automation выполняется штатной библиотекой Windows через вложенный исполнитель `AutomationWorker.ps1`, поставляемый вместе с бинарной сборкой. Установка других программ не требуется. Если приложение не предоставляет соответствующий шаблон UI Automation, ответ сообщает об отсутствии поддержки; в таком случае использовать существующие инструменты мыши и клавиатуры.

Координаты инструментов используются в физических пикселях Windows. Снимки экрана возвращают описание области исходного экрана и отдельный PNG-блок. Для мыши и клавиатуры нужен доступный интерактивный рабочий стол Windows; инструменты не предназначены для экрана входа и окна UAC. Ввод в приложения с повышенными правами может ограничиваться Windows.

## Собственный GitHub-коннектор

**17 инструментов категории «GitHub»** дополняют ранее существовавшие 24 инструмента MCP (всего **41**). Модуль использует `https://api.github.com` (REST/GraphQL), `https://uploads.github.com` (файлы релизов) и локальный `git`. Внешний коннектор GitHub для этих операций не требуется.

**Авторизация на DOMKAT:** установленный `gh` (GitHub CLI) уже авторизован под нужной учётной записью. Наш сервер получает ключ через `gh auth token -h github.com` внутри процесса и использует только для обращения к API GitHub. Ключ не передаётся через MCP, не пишется в репозиторий или журналы. GitHub CLI хранит учётные данные в защищённом локальном хранилище. Также поддерживаются переменные `GH_TOKEN` или `GITHUB_TOKEN`, если они заданы пользователем в окружении процесса сервера. Для самостоятельной авторизации на новом ПК: `gh auth login`.

| Инструмент | Назначение |
|---|---|
| `github.auth` | Проверка учётной записи и лимитов API |
| `github.api` | Универсальный GitHub REST API, GET/POST/PATCH/PUT/DELETE |
| `github.graphql` | Универсальный GraphQL API |
| `github.repositories` | Репозитории: список, чтение, создание, настройка, удаление |
| `github.files` | Папки, файлы, чтение, создание/обновление с SHA, удаление |
| `github.branches` | Список, создание, чтение и удаление веток |
| `github.commits` | История, содержимое коммита, сравнение |
| `github.batch_commit` | Один атомарный коммит для нескольких текстовых/двоичных файлов |
| `github.issues` | Issues: список, чтение, создание, обновление, комментарии |
| `github.pulls` | Pull Requests: список, создание, изменение, слияние, файлы, reviews |
| `github.actions` | Workflow: список, запуск, состояние, отмена, повторный запуск |
| `github.jobs` | Jobs, сведения и загрузка журнала |
| `github.releases` | Релизы: список, создание, обновление, удаление |
| `github.release_assets` | Файлы релизов: список, загрузка, скачивание, удаление |
| `github.artifacts` | Артефакты Actions: список, скачивание ZIP, удаление |
| `github.search` | Поиск кода, задач, репозиториев, коммитов, пользователей |
| `github.git` | Локальный Git: clone/status/fetch/pull/checkout/log/commit_push |

`github.api` работает с относительными путями GitHub API, например `/repos/lvlaksim1/chatgpt-mcp-tunnel/actions/runs`. Произвольные внешние URL не принимаются. Через него или `github.graphql` доступны дополнительные операции GitHub API без добавления новых инструментов. Для массивных ответов предусмотрена обрезка и возможность запрашивать страницы через `page`/`per_page`.

**Безопасность публикации:** `github.files` может принимать ожидаемый SHA содержимого; `github.batch_commit` принимает ожидаемый SHA вершины ветки и обновляет ссылку только fast-forward. Для `github.git commit_push` файлы указываются явно (не выполняется `git add .`); для новых локальных коммитов используется GitHub `noreply` адрес авторизованного пользователя, чтобы не раскрывать приватный email. Глобальные настройки Git не изменяются.

**Тестирование миграции:** `python tests/test_github_tools.py` проверяет обнаружение, авторизацию, чтение REST/GraphQL, поиск и другие операции. С `GITHUB_E2E_WRITES=1` на DOMKAT создаётся временная ветка, выполняются создание/обновление/удаление файлов, атомарный коммит и локальная отправка, затем ветка удаляется. Данные основной ветки не меняются. В CI без локальной авторизации выполняются только протокольные проверки. Дополнительная проверка скачивания — `python tests/test_github_downloads.py`: небольшой публичный файл релиза проверяется всегда при наличии авторизации, журналы и артефакты Actions — при наличии ID из отдельной тестовой среды. Результаты расширенных испытаний Issues, Pull Requests, релизов и Actions записаны в [`docs/github-extended-e2e-2026-10-28.md`](docs/github-extended-e2e-2026-10-28.md).

> Штатный GitHub-коннектор ChatGPT не отключается автоматически. Отключать его следует после независимого подтверждения 41 инструмента в новом чате; он пока оставлен как резервный способ доступа.

## Windows agent installer

Агент для второго Windows-компьютера поставляется по той же схеме, что MailRu Desktop:

- `ChatGPT_MCP_Agent_Setup_vX.Y.Z.exe` — полная установка без прав администратора, настройка и трей.
- `ChatGPT_MCP_Agent_Update_vX.Y.Z.exe` — обновление с сохранением DPAPI-ключа, настроек и ярлыков.
- Каталог программы `%LOCALAPPDATA%\\Programs\\ChatGPTMCPAgent`, пользовательские данные `%LOCALAPPDATA%\\ChatGPTMCPAgent`; по желанию — автозапуск при входе пользователя.
- Проверка обновлений через GitHub Releases и установка с отдельным помощником без диалогов PowerShell/Inno Setup; старые агентские бинарные релизы удаляются после публикации нового, исходные теги сохраняются.
- Комплект `.NET 8` self-contained x64; один OpenAI MCP Tunnel остаётся на DOMKAT.

Сборка выполняется из `src/ChatGptMcpAgent` и `src/ChatGptMcpTunnel`, сценарии Inno Setup — в `installer/`, workflow релиза — `.github/workflows/agent-installer-release.yml`. Подробности: [docs/agent-installer.md](docs/agent-installer.md).

## Router Lite — несколько компьютеров через один туннель

Один MCP-туннель на DOMKAT обслуживает локальные команды и подключаемые Windows-агенты. Все 23 инструмента Windows поддерживают необязательный `computer` (по умолчанию DOMKAT), новый `computers.list` расширяет прежние 41 инструмент до 42. Все 17 инструментов GitHub продолжают обслуживаться основным узлом.

**28.10.2026:** этапы маршрутизации и испытаний устойчивости прошли на двух отдельных процессах одного компьютера, включая PNG, Windows GUI, UI Automation, неверный ключ, восстановление после перезапуска и старые регрессионные проверки. Испытаний на втором физическом ПК пока не было, автоматическое развертывание агента ещё не готово.

См. [docs/router-lite.md](docs/router-lite.md).

## Испытания

`python tests/test_mcp_stdio.py` — проверка MCP initialize, обнаружения всех инструментов и `bridge_ping`.

`python tests/test_windows_tools.py` — сквозное тестирование всех восьми Windows-инструментов: запись/чтение/поиск тестового файла (включая усечение текста и двоичный формат base64), сведения о системе, выполнение PowerShell и завершение специально созданного тестового процесса.

`python tests/test_windows_uia.py` — испытание UI Automation на временном окне WPF: дерево, поиск, свойства, нажатие кнопки, изменение значения текстового поля с русскими буквами и переключение флажка.

`python tests/test_winforms_uia_compat.py` — сквозная проверка в отдельном окне Windows Forms: RawView/ControlView, уточнение типов по MSAA, реальный ввод текста, нажатие кнопки и переключение флажка.

`python tests/test_uia_real_apps.py` — только чтение UIA-деревьев в открытых приложениях Калькулятор и Excel, без изменений пользовательских файлов.

`python tests/test_gui_input_tk.py` — сквозное испытание графических инструментов на временном окне Tk: поиск, активация, перемещение, щелчок, прокрутка, клавиша Backspace, ввод текста, PNG-снимок, закрытие. Тест выполняется только на интерактивном компьютере Windows (не в серверном CI без рабочего стола).

Для появления новых инструментов в ChatGPT необходимо обновить список инструментов в настройках **ChatGPT MCP Tunnel** (действие «Обновить» / Refresh, если доступно) и открыть новый чат. Перезапуск локального сервера сам по себе не обновляет кэш инструментов текущего разговора.

## Границы текущей реализации

Плагин предназначен для личного использования владельца. Дополнительные проверки владельца или подтверждения операций в самом MCP-сервере сейчас не реализуются согласно решению владельца. `bridge_ping` не читает файлы и не выполняет команды; остальные инструменты действительно работают с Windows.

## Управление проектом

Публичный репозиторий создан через `lvlaksim1/repo-factory`, профиль `project-manager`; менеджер и контекст хранятся в этом репозитории.

### Выбор папки установки

Полный универсальный установщик позволяет самостоятельно выбрать каталог. По умолчанию сервер предлагает D:\ChatGPTMCPAgent, клиент — пользовательский каталог на C: (%LOCALAPPDATA%\Programs\ChatGPTMCPAgent); при отсутствии D: сервер предлагает доступный пользовательский каталог. Обновление сохраняет выбор. См. docs/universal-installer.md.

### Clean reinstall with the SAME existing OpenAI Tunnel ID
No new OpenAI cloud tunnel or ChatGPT plugin will be created. The old local Windows runtime and cached credentials are replaced, while the existing logical Tunnel ID remains. See docs/clean-domkat-reinstall.md for safe preparation and independent cutover.

## Windows tray icon and settings-window repair (v0.2.3)
A custom multi-resolution native icon is now embedded into the Windows executable, both full and update installers, settings dialogs and the tray. Tray adds green/amber/red connection markers.

The Server status uses an asynchronous bounded local readyz check and Router Lite TCP probe, not client-only stderr messages, fixing a false offline status. The previously hidden modal Settings Form no longer blocks opening from the tray: Settings is modeless, reusable and brought forward upon repeated selection. The read-only --probe-server and --test-icons CLI options support verification. The --settings option signals the existing tray process to open its settings without launching a second OpenAI tunnel.

## Native Win32 visibility fix for Settings (v0.2.4)
Live DOMKAT verification showed the Server Settings Form was created in the tray process and the menu callback logged opening it, but its HWND lacked WS_VISIBLE. Calling ShowWindow(hwnd, SW_SHOW) made the correct settings window appear in the foreground. The application now verifies native window visibility after WinForms Form.Show() and explicitly applies SW_SHOW for hidden HWNDs. Reopening Settings uses the same method; no second OpenAI tunnel is created.

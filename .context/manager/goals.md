# Manager goals

## ГЛАВНАЯ ЦЕЛЬ — полностью автоматический MCP-чат, 30.10.2026
- Локальное окно должно работать как мессенджер: пользователь только пишет и получает ответы, не запускает chat.ui.open, не нажимает «Начать передачу», не ищет и не поддерживает MCP Apps.
- «+ Новый чат» — новый диалог в обычном ChatGPT; прежний локальный чат продолжает точно соответствующий ему облачный диалог без потери контекста и дублей.
- Обязательная приёмка: 10 последовательных сообщений и 10 правильных ответов в одном локальном чате; затем существующий/новый чат, перезапуск, разрыв и несколько компьютеров.
- Только MCP-обмен сообщениями и chat.reply, WebView2 только авторизация. Исключить DOM-ввод, браузерные клики и чтение ответов, Node/CDP, отдельный API, Work/Codex.
- Сохранить Windows 10, VPN Happ, один туннель, Router Lite, безопасную очередь и локальный STOP. GitHub только код и готовые Releases, Actions не использовать.


## 30 Oct 2026 continuity
Keep existing-chat target:active and explicit-new-chat target:new as independent acceptance criteria. Do not claim success from isolated tests alone. Preserve current connections until controlled install.


## Актуальные цели — 30.10.2026
- Стабильный один MCP Tunnel для управления Windows и локального чата через **обычный ChatGPT** (не Work/Codex/API), с Router Lite и сохранением VPN Happ.
- Быстрая работа: вначале цельный план, затем один локальный сценарий правок, сборки, проверок и краткий результат, вместо десятков микрозапросов.
- Безопасное управление: локальный STOP, содержательные уведомления без секретов, ~1 сек между изменениями GUI, без искусственных задержек CLI; различение выполнения/ожидания модели/достоверного завершения.
- Только локальная разработка и тестирование на `D:\Dev`; GitHub — место хранения исходников и готовых релизов, **Actions отключены**.
- Authority: owner-directive 30.10.2026. Старые блоки ниже — история.

1. Maintain reliable independent ChatGPT MCP Tunnel on Windows, with functional backward compatibility for DOMKAT.
2. Deliver the owner-approved Router Lite: one OpenAI tunnel, one plugin, several named Windows computers.
3. Keep the implementation small: one binary with router/agent modes, no central database, lightweight connections; GitHub tools remain tied to the main host.
4. Test each implementation step before publication or affecting the live runtime.
5. Preserve context, reproducible builds, protected credentials and Russian-language reports with conservative evidence claims.

Source: Owner instructions and `.context/project/goals.md`. Authority: owner-directive.

# Процедурная память менеджера

## Проверка интеграции с GitHub
- Сверить SHA main и CI для этой ревизии; получить tools/list от MCP; проверить чтение через GitHub-инструменты; операции записи выполнять в специально созданной временной ветке и уничтожать только её после успешной проверки. Записывать фактические коды ответов.
  - source: tests/test_github_tools.py; tests/test_mcp_stdio.py; .github/workflows/mcp-smoke.yml
  - authority: verified-repository

## Проверка Windows UI Automation
- Для найденного элемента использовать тот же параметр view, что при поиске; если UIA-шаблоны отсутствуют, проверять доступные MSAA-действия, не угадывать тип. Применять отдельные окна WPF/Windows Forms и только чтение при работе с Калькулятором или Excel.
  - source: README.md; tests/test_winforms_uia_compat.py; tests/test_windows_uia.py; tests/test_uia_real_apps.py
  - authority: verified-repository

## Состояние Windows-туннеля
- Сначала проверить процессы сервера и клиента, затем локальные healthz/readyz. Не запускать второй клиент на том же туннеле. Ключи и DPAPI не выводить в отчёты и не добавлять в Git. После обновления списка инструментов ChatGPT проводить отдельную приёмку.
  - source: README.md; scripts/Start-Tunnel.ps1; docs/E2E-2026-10-28.md
  - authority: verified-repository

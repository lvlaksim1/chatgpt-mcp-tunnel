# Подтверждённые представления менеджера

## Архитектура и состав
- В текущем исходном коде определены 41 инструмент MCP: прежние 24 для Windows и 17 для GitHub. Собственный GitHub-модуль включает GithubTools.cs, GithubBatchTools.cs, GithubRuntime.cs. Обнаружение инструментов в новом чате требует проверки обновлённого каталога.
  - source: README.md; src/ChatGptMcpTunnel/GithubTools.cs; Git revision 322961c13ab6f20d9670bd76a61c17d7481225d6
  - authority: verified-repository

## Актуальная ревизия
- Коммит 1e9582a7e7e9316d822c3efa5143611eecc13d7a корректирует заголовок Accept для получения журналов задач и артефактов GitHub. Обе проверки GitHub Actions на этой ревизии завершились успешно; результат не гарантирует работоспособность всех инструментов в конкретном чате.
  - source: Git commit 1e9582a7e7e9316d822c3efa5143611eecc13d7a; https://github.com/lvlaksim1/chatgpt-mcp-tunnel/actions/runs/40624299744 ; https://github.com/lvlaksim1/chatgpt-mcp-tunnel/actions/runs/40624299879
  - authority: verified-repository; verified-ci

## Проверенная сборка
- На ревизии 322961c13ab6f20d9670bd76a61c17d7481225d6 завершились успешно две проверки GitHub Actions: MCP Server Smoke Test и Repository Storage Policy. Успех этих проверок не доказывает реальную работу всех инструментов в каждом чате.
  - source: https://github.com/lvlaksim1/chatgpt-mcp-tunnel/actions/runs/40622817404 ; https://github.com/lvlaksim1/chatgpt-mcp-tunnel/actions/runs/40622817373
  - authority: verified-ci

## Секреты и подключение
- Модуль GitHub работает через REST/GraphQL и локальный Git; использует авторизованный GitHub CLI либо переменные окружения. Ключ туннеля хранится вне Git в защищённом хранилище Windows DPAPI. Значения секретов не проверялись и в память не вносились.
  - source: README.md; src/ChatGptMcpTunnel/GithubRuntime.cs; scripts/Start-Tunnel.ps1
  - authority: verified-repository

## Состояние исполнения на момент осмотра
- При проверке DOMKAT 28 октября 2026 года наблюдались процессы tunnel-client.exe и dotnet с ChatGptMcpTunnel.dll из D:\ChatGPT-MCP-Tunnel. Этот факт относится только к моменту наблюдения и должен быть перепроверен перед действиями над работающим экземпляром.
  - source: read-only Windows process inspection on DOMKAT, 2026-10-28
  - authority: verified-runtime

## Неопределённость
- Наличие реализации 17 инструментов, успешных проверок и работающего сервера не гарантирует, что инструменты появились в закэшированном списке конкретного чата ChatGPT. Замена штатного GitHub-коннектора ещё требует самостоятельной проверки.
  - source: README.md, разделы про собственный GitHub-коннектор и обновление списка инструментов
  - authority: verified-repository

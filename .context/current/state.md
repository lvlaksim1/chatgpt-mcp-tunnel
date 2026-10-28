# Текущее состояние

Проверенная ревизия на 28.10.2026: 1e9582a7e7e9316d822c3efa5143611eecc13d7a (lvlaksim1/chatgpt-mcp-tunnel, main). Предыдущий базис: 322961c13ab6f20d9670bd76a61c17d7481225d6.

- Реализован самостоятельный .NET 8 сервер MCP с 24 инструментами Windows и 17 инструментами GitHub, всего 41. Состав подтверждён исходным кодом и README; видимость инструментов в чате ChatGPT зависит от обновления каталога.
- Инструменты UI Automation поддерживают control/raw, данные MSAA и действия для Windows Forms; имеются отдельные тесты WPF, Windows Forms, Калькулятора и Excel.
- В репозитории имеются испытания MCP stdio, Windows, графического интерфейса и GitHub. README описывает проверенные операции с временной веткой GitHub; каждую операцию следует оценивать по фактическому протоколу, а не по общему утверждению.
- GitHub Actions на ревизии 1e9582a: MCP Server Smoke Test — success, https://github.com/lvlaksim1/chatgpt-mcp-tunnel/actions/runs/40624299744 ; Repository Storage Policy — success, https://github.com/lvlaksim1/chatgpt-mcp-tunnel/actions/runs/40624299879.
- При локальном осмотре 28.10.2026 сервер dotnet и tunnel-client работали из D:\ChatGPT-MCP-Tunnel. Доступность позже требует новой проверки.
- Постоянный идентификатор менеджера в .context/manager/identity.json: project-manager. Источники полномочий main/main указаны в манифесте.

Это состояние не является подтверждением полного перехода на собственный GitHub-коннектор.

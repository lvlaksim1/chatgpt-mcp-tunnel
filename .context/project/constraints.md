# Project constraints

## Межсетевой транспорт (уточнение владельца 28.10.2026)
- DOMKAT и MKATUSHONOK работают в разных локальных сетях; существующий частный адрес DOMKAT 192.168.0.3 не является сам по себе доступным адресом со стороны MKATUSHONOK.
- Для подключения дополнительных агентов нужна согласованная защищённая межсетевая связь (например, Tailscale/WireGuard либо WSS). Прямые незащищённые WebSocket-порты в интернет не выставлять.
- Выбор транспорта, установка стороннего ПО, правила брандмауэра и другие изменения сети требуют отдельного решения/подтверждения владельца.


- Repository is public; API keys, DPAPI ciphertext, `.tools`, process output and binary distributions must remain untracked.
- Factory policy: max 5 MiB per tracked file; no tracked executable, installer, archive, DLL or PDB.
- Windows paths passed to tunnel-client's command parser must use forward slashes in `mcp-command`.
- Base MCP server has 41 tools (8 computer, 15 GUI/UIA, 17 GitHub, bridge_ping); Router Lite stage 1 adds `computers.list` (42 total). The historical assertion that only `bridge_ping` existed is superseded by current source and tests.
- Keep the official tunnel process running for ChatGPT discovery; do not run two clients for one Tunnel ID simultaneously.
- Maintain working environment on drive D:, with no reliance on desktop bridge, browser injection, external ToolRouter, mandatory database or additional public server.
- Keep Router Lite disabled by default. Only expose its agent listener on a protected private network or HTTPS/TLS-backed WebSocket; do not publish a plain WebSocket endpoint or credentials.
- Preserve local default behavior of existing tools; remote routing requires explicit computer name.
- The owner did not commission a separate owner-authentication/approval framework; lightweight authentication of remote agents is necessary to guard network access.
- Automated CI must not require private API keys.

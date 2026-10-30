# Project constraints

## Приоритетные ограничения владельца — 30.10.2026
- Главная локальная копия `D:\Dev\ChatGPT-MCP-Tunnel` на DOMKAT; рабочий `D:\ChatGPTMCPAgent` отдельно. GitHub только storage/source/history/Releases, **Actions не использовать** (старое требование CI отменено).
- Связные CLI/файловые задачи выполнять одним локальным пакетом, GUI изменения с ~1s интервалом, чтения и shell без принудительного ожидания. STOP локально, безопасно, с ручным возобновлением.
- Win10, системный Happ VPN, обычный ChatGPT без Work/Codex/API, один Tunnel, без TinyFish/Desktop Commander. Не терять незакоммиченный Unicode-патч v0.2.28.
- Идеи P2P равных узлов, собственного VPN и offline голоса остаются обсуждением, не санкцией на реализацию.
- Источник: owner-directive 30.10.2026; предыдущие транспортные исторические ограничения ниже действуют в непротиворечащей части.

## Approved transport decision after owner execution order — 28.10.2026
- Tailscale/WireGuard virtual network chosen for different physical LANs. Keep one OpenAI MCP Tunnel and do not expose unencrypted ws:// publicly.
- Only exact authorized Tailscale client IPv4 may receive inbound firewall rule for Router Lite at exact local Tailscale IP and port 55201. The VPN adapter must actually exist and belong to the server before firewall setup; Windows UAC approval is required.
- Tailscale installation and sign-in involve user account/Windows authorization; verified release alone does not demonstrate physical connectivity.
- Supersedes earlier 'transport not yet selected' status; retains network security and independent owner authorization requirements.


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

# Project architecture

Verified deployment (28 October 2026):
`ChatGPT custom MCP plugin → OpenAI Secure MCP Tunnel → official tunnel-client v0.0.18 → ChatGptMcpTunnel.dll (.NET 8, stdio MCP)`.

- C# MCP server: `src/ChatGptMcpTunnel/Program.cs` (official ModelContextProtocol 2.2.0 SDK).
- Windows MCP tools: `src/ChatGptMcpTunnel/ComputerTools.cs` with `system.info`, `fs.list`, `fs.read`, `fs.write`, `fs.search`, `shell.exec`, `process.list` and `process.kill`; plus `bridge_ping`.
- UIA compatibility update 28 Oct 2026: baseline saved in branch `baseline-before-uia-compat-20261028` (8051273). Worker supports `view=control|raw`, MSAA `IAccessible` diagnostics and fallback actions via `MsaaBridge.cs.txt`, and type attribution (`uia_type`, `type_source`) for WinForms. A disposable WinForms fixture demonstrates real `set_value`, `invoke`, `toggle` success; WPF fixture validates UIA pattern actions. Read-only Calculator/Excel probes: Calculator still only root in both views, Excel supplies useful elements. Full regression: stdio 24, Windows eight, GUI 11, WPF UIA four, WinForms and real-app probes.
- Windows UI Automation: `src/ChatGptMcpTunnel/UiAutomationTools.cs` exposes four `desktop.uia.*` tools in Multimedia category; `src/ChatGptMcpTunnel/AutomationWorker.ps1` uses the native .NET Framework UIAutomationClient assemblies from Windows in a bounded STA PowerShell subprocess, with UTF-8 JSON IPC and action patterns for controls. Verified on disposable WPF window: tree, find, get, invoke, set_value, toggle, close; existing mouse/keyboard tools remain fallbacks.
- Own GitHub integration (28 Oct 2026): 17 MCP commands (category «GitHub») in `GithubTools.cs` and `GithubBatchTools.cs`; shared `GithubRuntime.cs` uses authenticated GitHub REST/GraphQL with `gh auth token` from Windows keyring (or process environment), never exposes secret via MCP. Covers repos, files, branches, multi-file atomic commits, issues, PRs, Actions, jobs, logs, releases, binary assets, artifacts, search, local Git, generic endpoints. New local Git commits use authenticated account-specific `noreply` email to avoid GH007. Tests `test_github_tools.py` passed on DOMKAT including real create/update/delete, batched commit, local clone/commit/push and cleanup of temporary branch. Original 24 tools remain; server now exposes 41 total. Stock GitHub connector kept connected temporarily for validation and emergency fallback.
- Windows GUI tools: `src/ChatGptMcpTunnel/DesktopTools.cs` provides 11 new tools (`desktop.screen.*`, `desktop.window.*`, `desktop.mouse.*`, `desktop.keyboard.*`) with Win32 P/Invoke and System.Drawing.Common PNG screenshot encoding. The MCP image content is returned as `ImageContentBlock` alongside region metadata.
- Windows deployment root: `D:\ChatGPT-MCP-Tunnel`; `runner/start.cmd`, `runner/stop.cmd`, `runner/enable-autostart.cmd`, `runner/disable-autostart.cmd`.
- Scheduled task `ChatGPT MCP Tunnel`: starts at Windows sign-in after 20 seconds under DOMKAT\\Maksim (no administrator privilege required).
- Local service dashboard and probes: `http://127.0.0.1:55198/ui`, `/healthz`, `/readyz`.
- Volatile client binaries/profiles: repository-local `.tools/`, ignored by Git. Secret stored encrypted using Windows DPAPI under `%LOCALAPPDATA%\ChatGptMcpTunnel`.
- Windows E2E: `tests/test_windows_tools.py` passed for 8 system/file/process tools; `tests/test_gui_input_tk.py` passed for all 11 GUI tools using a temporary Tk window, with real text/key events and PNG verification. MCP stdio discovery: 41 tools. GUI tests require an interactive Windows session; normal headless Windows CI covers discoverability and the 8 system tools.
- After server restart ChatGPT custom plugin metadata may still be cached. Refresh the tool listing in ChatGPT plugin management and begin a new conversation for direct calls; do not mistake the prior-cached `bridge_ping`-only catalog for a server defect.
- Never import or depend on code from the separate ChatGPT Desktop Local Bridge project.
- The owner explicitly chose not to add a separate owner-authentication or approval framework at this stage. Do not silently reintroduce one.

- 2026-10-28 shell.exec stdio correction: child PowerShell/CMD standard input is explicitly redirected and closed at startup, preventing external programs (notably Git for Windows) from inheriting the persistent MCP transport stdin; stdout/stderr and timeout behavior remain unchanged. tests/test_windows_tools.py covers stdin EOF and real `git --version` in PowerShell and CMD.


## Router Lite, stage 1 (28.10.2026)
Accepted by Owner: one OpenAI tunnel, one ChatGPT plugin, several Windows computers. `RouterLite.cs` adds an optional background WebSocket listener on DOMKAT when explicitly enabled, a `--agent` run mode in the same .NET 8 executable, computer discovery and request/response correlation. Original 41 tools remain available; `computers.list` raises count to 42. Only `system.info` currently has a `computer` argument and remote execution. GitHub remains main-host global functionality.

A Release build isolated from the live runtime and local two-process tests of 42 tool discovery, agent connection, remote system info, unknown name rejection and disconnect succeeded. This is not proof of physical multi-computer operation. Next: remote routing for remaining 22 Windows tools and MCP screenshot image blocks, physical-PC connectivity, secure setup. Details: `docs/router-lite.md`.

## Router Lite stages 2–3 (28.10.2026)

All 23 Windows tools now accept the optional `computer` parameter. Reflection-based local execution of registered Windows MCP methods avoids duplicating application logic. Remote calls return strings or MCP text/image blocks through one WebSocket transport. Screenshots preserve raw PNG content. On-agent concurrent system commands and serialized GUI actions are implemented; duplicate computer names and wrong tokens fail; agent reconnects after test-router restart.

Evidence: isolated Release build, `test_router_lite.py`, `test_router_reconnect.py`, and existing `test_windows_tools.py`, `test_gui_input_tk.py`, `test_windows_uia.py`, `test_winforms_uia_compat.py`, `test_uia_real_apps.py`, all passed on DOMKAT. No second physical PC tested yet and existing tunnel not restarted. See `docs/router-lite.md`.


## Windows per-user agent packaging (28.10.2026)
A lightweight WinForms `ChatGptMcpAgent.exe` tray process manages the existing self-contained `Runtime/ChatGptMcpTunnel.exe --agent` child. It does not introduce another OpenAI tunnel, repo or MCP tool implementation. DPAPI CurrentUser protects credentials; HKCU Run autostart; settings and logs use LocalAppData; a copied single-file EXE runs updater mode outside the installed directory and waits for app shutdown via named pipe before launching the silent Inno update. Full/update installers and Release automation mirror MailRu Desktop.
- source: Owner-approved design and observed successful local Windows publish/selftest 28.10.2026.
- authority: owner-directive, verified-runtime.

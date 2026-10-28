# Manager beliefs

## B19 — Verified DOMKAT running version 0.2.6 (28.10.2026 22:20 MSK)
- statement: The running WinForms agent PID 30196 on DOMKAT is D:/ChatGPTMCPAgent/ChatGptMcpAgent.exe (FileVersion 0.2.6.0; started 20:38:25 MSK); router listens on 192.168.0.3:55201. ChatGPT MCP bridge_ping succeeds. Physical MKATUSHONOK connection remains unverified and no established client TCP connection was observed during the snapshot. Current chat exposes 41 connector tools versus 42 locally advertised earlier.
- source: original ChatGPT MCP Tunnel read-only shell.exec process/version/TCP checks; bridge_ping; GitHub v0.2.6 release and Actions run 40736310056, all 28.10.2026.
- authority: verified-runtime, verified-repository.
- supersedes: pending installation of v0.2.6 in prior handoff; does not prove network acceptance or UI quality.


## B1 — Identity and baseline
- statement: The manager's stable ID is `project-manager`, its authoritative manager and product branches are both `main`, and it governs only `lvlaksim1/chatgpt-mcp-tunnel`.
- source: `.context/manager/identity.json`, `.context/manifest.json`, live GitHub branch `main` at `e6b79a1d8977e777fc1e15faf4936d0880dd44e5` before Router Lite development.
- authority: verified-repository.

## B2 — Original Windows implementation
- statement: The existing Windows MCP server has 41 working/discoverable tools (8 system/file/process, 15 graphical, 17 GitHub, and `bridge_ping`); the original tunnel remains based on .NET 8 and official `stdio` transport.
- source: repository `README.md`, existing tests and independent live source review on 28.10.2026.
- authority: verified-repository, verified-runtime.

## B3 — Owner's Router Lite decision
- statement: One OpenAI tunnel and one MCP plugin must manage multiple computers. Add a small router to the existing project, run local commands on DOMKAT by default, connect agents from remote PCs, avoid a database, additional projects and complex infrastructure.
- source: Owner dialogue 28.10.2026, direct acceptance and instruction to start development.
- authority: owner-directive.
- supersedes: previous idea of one tunnel per computer and enterprise-style architecture.

## B4 — Prototype scope, not production readiness
statement: Router Lite stages 1-3 passed tests on DOMKAT: 42 tools, optional `computer` on all 23 Windows tools, remote files/shell/processes/GUI/UIA/PNG, concurrent requests, wrong key and duplicate name rejection, and reconnect after router restart; physical second PC not yet checked.
source: isolated Release build; test_mcp_stdio.py, test_router_lite.py, test_router_reconnect.py, test_windows_tools.py, test_gui_input_tk.py, test_windows_uia.py, test_winforms_uia_compat.py, test_uia_real_apps.py (28.10.2026).
- authority: verified-runtime.


## B5 — Agent installer reference and current status
- statement: Owner chose MailRu Desktop's per-user Inno Setup full/update design for the Router Lite agent. The WinForms tray host, self-contained runtime, Inno Setup full/update installers and CI acceptance have passed and numeric v0.1.0 is published; second physical PC is not yet proven.
- source: Owner confirmation 28.10.2026; MailRu Desktop `installer/*.iss`, `MailRuDesktop.UpdateAgent`, release workflow; local `dotnet publish`, `--selftest`, MCP/router tests.
- authority: owner-directive, verified-repository, verified-runtime.

## B7 — Installation directory
- statement: The Owner requests a user-selectable installation directory; server suggests drive D and client suggests drive C. Update must preserve the chosen directory.
- source: Owner directive 28.10.2026, installer source and CI tests.
- authority: owner-directive, verified-repository.

## B8 — universal installer release acceptance
- statement: Single Server/Client Windows full installer v0.2.0 was published after CI successfully tested the role-based default/custom installation directories and update/uninstall operations. Existing live DOMKAT installation remains unchanged.
- source: GitHub Actions 40696034380; GitHub Release v0.2.0; observed on 28.10.2026.
- authority: verified-repository, verified-runtime.

## B9 — Same OpenAI tunnel identity
- statement: Owner explicitly rejected creating new OpenAI tunnels; clean DOMKAT migration retains existing logical Tunnel ID and ChatGPT plugin while removing old local runtime, profiles and credentials.
- source: Owner correction 28.10.2026 and official tunnel-client CLI help.
- authority: owner-directive, verified-runtime.

## B10 — Recovered live DOMKAT same tunnel
- statement: The new DOMKAT Server installation preserved the original OpenAI Tunnel ID and encrypted credentials, but native stderr merging in Windows PowerShell 5.1 caused repeating early termination. Direct native invocation restored the same OpenAI MCP tunnel and Router Lite. 42 local tools tested, original bridge ping, GitHub and Windows tools work, both health endpoints return 200.
- source: direct Remote Desktop Commander investigation and actual recovered MCP calls 28.10.2026.
- authority: verified-runtime.

## B11 — Stable recovery and v0.2.2 live installation
- statement: Windows CI run 40712500651 succeeded and v0.2.2 full/update installers published. v0.2.2 update SHA256 verified, installed on live DOMKAT, existing OpenAI Tunnel ID and DPAPI credentials preserved. New app v0.2.2 responds to bridge_ping, system.info and 42 local MCP tool tests; single tunnel-client and Router Lite bound to 192.168.0.3:55201, healthz/readyz HTTP 200.
- source: GitHub Actions 40712500651, Release v0.2.2, actual Desktop Commander update result and original ChatGPT MCP tool verification on 28.10.2026.
- authority: verified-repository, verified-runtime.

## B12 — GUI defects and icon
- statement: On DOMKAT the server tray had a false offline status although the same OpenAI tunnel was healthy. The Settings menu also appeared inert because a hidden modal Server Settings Form was already present. The user additionally requested an appropriate application icon.
- source: Owner screenshots 28.10.2026, DOMKAT live bridge_ping/readyz/router checks, Win32 enumeration, .NET source inspection and local published win-x64 test.
- authority: owner-directive, verified-runtime.

## B13 — Native Win32 window visibility defect confirmed
- statement: Modeless v0.2.3 Settings callback logged opening its Form, but the native top-level HWND remained hidden without WS_VISIBLE. Manually calling user32 ShowWindow(SW_SHOW) on the actual DOMKAT Server Settings HWND changed visibility to true and desktop.window.list then found the foreground window. v0.2.4 source verifies native IsWindowVisible and applies SW_SHOW after showing and reopening.
- source: live DOMKAT Win32 EnumWindows, ShowWindow and desktop.window.list evidence 28.10.2026, self-contained win-x64 v0.2.4 build/probe tests.
- authority: verified-runtime, owner-directive.

## B14 — Approved secret UX and independent release
- statement: User approved native settings forms matching interactive HTML mockup: secret values are absent from primary forms, supplied only through an on-demand temporary entry dialog. On client only key-entry is permitted, never reveal/copy; on server separate token replacement and client-key generation/copy. No port entry, use 55201. DOMKAT current address 192.168.0.3 stays unchanged until separate DHCP discussion. User requires full and update installer release, but will self-update the installed DOMKAT application using its built-in updater.
- source: user decisions 28.10.2026, verified local v0.2.5 win-x64 build and selftest/icon/status/forms tests.
- authority: owner-directive, verified-runtime.

## B15 — Stable v0.2.5 binaries published, owner self-updates
- statement: v0.2.5 source commit 533d4ecb088957d3074bdb6da6810ffebf66e10a produced two Windows Inno binaries, full ChatGPT_MCP_Tunnel_Setup_v0.2.5.exe SHA256 57b811467bf4a79a56ae8d8f99965ca36840745b853d357ece29a5a6e105f13a and Update ChatGPT_MCP_Tunnel_Update_v0.2.5.exe SHA256 278140cb9ee4ed7a1bfcaa500a3b38d27937520185645cc30b6dc75c40ff112b. Windows GitHub Actions run 40732281198 completed success, including install/update/preservation tests. Owner explicitly performs actual DOMKAT update personally; installed app remains untouched by manager.
- source: GitHub Release v0.2.5, Actions 40732281198, owner directive 28.10.2026.
- authority: verified-repository, owner-directive.

## B16 — verified v0.2.5 updater IOException root cause
- statement: Windows file-sharing conflict in v0.2.5 downloader occurs because File.Move on staged .part happens before disposing its FileStream. User screenshot shows 100% and file-in-use error; DOMKAT Logs/agent.log has Update failed IOException 20:11:39 and 20:13:21 on 28.10.2026. DOMKAT installed v0.2.5. Fix v0.2.6 releases writer before move and adds --test-updater-download, local tests success.
- source: owner screenshot, read-only DOMKAT log/source inspection, local compilation/tests.
- authority: owner-incident, verified-runtime.

## B17 — Server LAN access likely blocked by Windows Firewall
- statement: On DOMKAT at 20:20 MSK Router Lite listened on 192.168.0.3:55201 and OpenAI readyz returned 200, while Wi-Fi used Public firewall profile with BlockInbound and no app/port 55201 inbound allow rule. This is a plausible client connectivity blocker, but independent network/authorization test from MKATUSHONOK has NOT been performed. DPAPI server key file updated 20:17:08, so clients require latest shared key. v0.2.6 adds optional on-demand UAC firewall rule scoped to current LAN subnet and client error interpretation; local tests pass.
- source: read-only DOMKAT network/firewall/process/log checks, verified code and local tests 28.10.2026.
- authority: verified-runtime, owner-incident.

## B18 — Stable Windows v0.2.6 updater/LAN release verified
- statement: Windows Actions run 40736310056 completed success on commit 37702f749cd6b391a4eda4fa7766ed9c3a59a847. Stable Release v0.2.6 contains Setup SHA256 66a12e056afa3bf3822e878ded51f8d3bd32f96f13ba12faa94575bf8f17484d and Update SHA256 2053ccadc645acfb0a79d5f7a3f4aef91453da8401db34f13bd97411805ede9b; legacy updater alias identical. Local fixes include close staging file before rename, optional LAN-only Windows Firewall rule button requiring UAC, error states for WebSocket 401/409/network. Actual MKATUSHONOK-to-DOMKAT connectivity remains UNVERIFIED until owner installs and tests; installed DOMKAT left untouched by project manager.
- source: release API, CI run and owner update instruction 28.10.2026.
- authority: verified-repository, owner-directive.

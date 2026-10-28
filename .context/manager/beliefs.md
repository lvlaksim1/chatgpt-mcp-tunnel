# Manager beliefs

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

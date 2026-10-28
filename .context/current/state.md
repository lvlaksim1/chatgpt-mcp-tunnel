# Current state

- Date: 28.10.2026 MSK; Project Manager `project-manager`, stable version 2.1.0; state/product authority branch `main`.
- Router Lite stages 1–3 implemented and locally verified in a separate Release build on DOMKAT. The working OpenAI Secure MCP Tunnel process was not restarted/replaced.
- 42 MCP tools advertised: original 41 plus `computers.list`. All 23 Windows tools support optional `computer`; if omitted, existing local behavior remains unchanged.
- Generic remote dispatcher invokes the existing Windows methods, correlates replies and preserves MCP text and PNG image blocks. GUI operations are serialized on each agent; other calls may run concurrently.
- Test evidence: `test_mcp_stdio.py`, `test_router_lite.py`, `test_router_reconnect.py`, `test_windows_tools.py`, `test_gui_input_tk.py`, `test_windows_uia.py`, `test_winforms_uia_compat.py`, `test_uia_real_apps.py` passed on DOMKAT with isolated build. Tests include incorrect token, duplicate agent, router restart and remote WPF UIA.
- External PC, remote network, installer/autostart and final release remain unverified/unimplemented. Existing two unrelated untracked local files were not changed.
- Source: Owner decision (single tunnel), verified source/build/runtime 28.10.2026.


## Installer implementation (28.10.2026 MSK)
- Added WinForms tray host `src/ChatGptMcpAgent`, typed first-run settings, protected DPAPI credentials, HKCU Run autostart, subprocess management and GitHub Releases updater.
- Added Inno Setup full/update scripts, release workflow with installer acceptance and release publication, Russian documentation in `docs/agent-installer.md`.
- Observed self-contained win-x64 publish and DPAPI/URL selftest exit 0 on DOMKAT; runtime self-contained publish, 42-tool MCP test, router test (without GUI on CI), reconnect test passed.
- Inno Setup not installed on DOMKAT: binary installers not yet compiled or installed. Live DOMKAT tunnel remains untouched.
- No second physical Windows computer tested.
- source: local build/test and owner-approved MailRu Desktop architecture.
- authority: verified-runtime, owner-directive.

## Published installer acceptance (28.10.2026 MSK)
- Binary release `v0.1.0` published with both Setup and Update assets at `https://github.com/lvlaksim1/chatgpt-mcp-tunnel/releases/tag/v0.1.0`.
- GitHub Actions run 40689966362 passed: Windows self-contained publish, agent DPAPI selftest, 42 MCP-tool checks, Router Lite integration/reconnect, Inno Setup compilation, first-install/update-preserves-data/uninstall-cleanup test.
- On DOMKAT the live MCP Tunnel was not restarted/replaced; physical second computer is still unverified.
- source: GitHub Actions release workflow 40689966362, GitHub Release v0.1.0.
- authority: verified-runtime, verified-repository.

## Editable installer folder (28.10.2026)
- Owner requirement: editable install path, server default D:, client default C:.
- Source implemented: native Inno directory page after role selection, server D:\ChatGPTMCPAgent if drive exists, client per-user program folder, fallback on no D.
- Persist InstallDir in HKCU registry; updater targets stored directory and preserves chosen path.
- CI tests were extended for default and custom directories. Real Inno acceptance pending.
- source: owner directive and local source review.
- authority: owner-directive, verified-repository.

## Universal installer v0.2.0 RELEASED (28.10.2026)
- GitHub Actions run 40696034380 completed success, including standalone WinForms and runtime build, official OpenAI tunnel-client SHA256 verification, MCP/router tests and real Inno first-install/update/uninstall acceptance.
- Universal Setup v0.2.0 published at https://github.com/lvlaksim1/chatgpt-mcp-tunnel/releases/tag/v0.2.0; Setup and Update assets both verified present (plus legacy updater alias).
- CI accepted server default D (fallback if not present), client default user-local C, arbitrary custom client directory, preservation during update and correct uninstall.
- Live DOMKAT legacy tunnel has not been removed or restarted. The owner requested a clean install and will enter the new OpenAI token; migration with temporary loss of MCP access is a separate next operation.
- source: GitHub Actions run 40696034380 and release v0.2.0.
- authority: verified-repository, verified-runtime.

## Same-tunnel clean reinstall preparation (28.10.2026)
- Owner explicitly requires same existing OpenAI cloud Tunnel ID and plugin; no create/delete cloud tunnel calls.
- New server UI uses existing Tunnel ID and accepts runtime API key; old key is not reused.
- Windows PowerShell 5.1 unsupported output redirection corrected; GUI x64 publish/selftest passed.
- Two-stage DOMKAT plan: prepare verified Setup and identity-only metadata while old tunnel works; independent local script then removes old autostart, executable, cached profiles/credentials and installs Server role on D, prompting for token.
- Old tunnel currently remains live; no destructive cutover occurred.
- source: Owner correction, source, local build and PowerShell AST checks 28.10.2026.
- authority: owner-directive, verified-runtime.

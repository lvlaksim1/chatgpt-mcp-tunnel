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

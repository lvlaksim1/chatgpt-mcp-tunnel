# Latest handoff

This is a compact working view; sealed manager BDI state takes precedence.

## Completed and verified (28.10.2026)

- Project Manager `project-manager` reinstated with stable v2.1.0 and previously published Router Lite decision.
- Router Lite stages 1–3 implemented; all 23 Windows MCP tools now have optional `computer`, while 17 GitHub tools stay global.
- One agent/one router mode in same .NET 8 binary; 42 MCP tools total. Preserved screenshot PNG blocks and original Windows behavior.
- Local DOMKAT tests passed: MCP stdio, two-process remote files/shell/processes/GUI/UIA/PNG, 23 target schemas, parallel requests, bad key, duplicate ID, unknown name, disconnect and reconnect after router restart.
- Passed original Windows, Tk GUI, WPF UIA, WinForms UIA and real-application read-only regressions using an isolated build. Live tunnel unchanged.

## Open commitment

Demonstrate and deploy a real second physical computer over protected network; add simple agent startup/credential handling, then release after full acceptance.

## Next operation

Physical PC onboarding and cross-host tests; do not claim production-ready multi-PC operation based only on DOMKAT tests. See `docs/router-lite.md`.


## Agent installer handoff (28.10.2026)
- Owner accepted MailRu Desktop conventions: full + update Inno Setup, no elevation, data/DPAPI under LocalAppData, tray/autostart, upgrade preserving state and GitHub release updates.
- Added `src/ChatGptMcpAgent` Windows Forms tray host, `installer/agent-full.iss`, `installer/agent-update.iss`, `.github/workflows/agent-installer-release.yml`, `docs/agent-installer.md`.
- Self-contained publish and DPAPI/URL selftest passed on DOMKAT. 42-tool MCP, router and reconnect tests passed on self-contained runtime.
- Next: GitHub Actions Inno Setup build and install/update/uninstall acceptance; publish numeric v0.1.0; second physical Windows PC remains not checked. Do not modify live tunnel before acceptance.

## Agent installer release acceptance
- GitHub Actions 40689966362: success on Windows, both Setup and Update v0.1.0 tested and published as GitHub Release assets.
- Installation, settings preservation during update, uninstall cleanup and agent DPAPI/MCP/router smoke checks passed in CI.
- Next: physical PC onboarding, protected network address, real remote UI/IO tests. Existing tunnel DOMKAT unchanged.
- source: GitHub Actions 40689966362 and GitHub Release v0.1.0; verified 28.10.2026.
- authority: verified-repository, verified-runtime.

## Editable installation folder
- Owner directive: one full installer with selectable install folder, server default D:, client default C:.
- Source implemented and CI tests added for custom directory, update, default paths and uninstall.
- Binary acceptance pending; existing DOMKAT tunnel still running.

## Verified universal installer release v0.2.0
- Successful Windows CI 40696034380 and published GitHub Release v0.2.0 (Setup, Update, legacy update alias).
- Role picker, default server D/client C, editable custom destination, preservation on update, autostart task on/off, clean uninstall were validated by real installer execution in CI.
- DOMKAT still has the previous active tunnel. The owner wants a clean reinstallation and will supply the token in the new server UI; do not prematurely stop the only connection.

## Owner correction 28.10.2026: existing tunnel only
- NO new logical OpenAI cloud tunnel; preserve current Tunnel ID and ChatGPT plugin.
- Replace only local DOMKAT runner, stored auth and profiles; Owner enters new token in new server UI.
- PowerShell 5.1 launcher fixed, x64 GUI selftest passed, staging/cutover scripts authored; live old tunnel unchanged.
- Next: CI release v0.2.1, stage verified installer, then autonomous cutover.

## DOMKAT tunnel is online again (28.10.2026)
- Clean server v0.2.1 installed successfully earlier, with same OpenAI Tunnel ID and new DPAPI token.
- Original failure: 2>&1 on tunnel-client run under PowerShell 5.1 Stop preference promoted normal MCP stderr INFO to terminating NativeCommandError, caused seven-second restarts.
- Direct native tunnel-client call in installed ServerStart.ps1 fixed problem; original ChatGPT bridge_ping and system/GitHub/window tools work, 55198/55201 listen, health/ready 200, 42 MCP tools registered.
- Source fix for v0.2.2 prepared. Next CI release and physical MKATUSHONOK.

## Live stable v0.2.2 confirmed 28.10.2026
- Fixed PowerShell 5.1 native stderr error was released in v0.2.2 (CI 40712500651 success).
- SHA256-verified update installed on DOMKAT without new cloud tunnel, same Tunnel ID, token in DPAPI, server role and autostart intact.
- Inno setup returned exit 0, app version 0.2.2, one tunnel-client and MCP Runtime, Router Lite 192.168.0.3:55201, health/ready 200.
- Original connector successfully calls bridge_ping and system.info; installed runtime local MCP test advertises 42 tools.
- Next MKATUSHONOK real client test; no need to reinstall DOMKAT.

# Manager plans

## Router Lite — active implementation plan
1. Preserve 41 existing tools and live tunnel; `computers.list` is tool 42. All 23 Windows tools have `computer` parameter. [complete, locally verified]
2. One executable with router/agent mode, outgoing WebSocket agents and request IDs without database. [complete, locally verified]
3. Extend routing to the remaining 22 Windows tools while preserving MCP text/image blocks and Windows interactive-session behavior. [complete, local E2E verified]
4. Connect two distinct physical PCs through a protected network; on DOMKAT reconnect, wrong key, duplicate ID, unknown target and GUI actions already passed. [next]
5. Add safe configuration/startup of agents and protected local secret storage; avoid new plugins or repositories.
6. After verified release of a version, update context and record the exact tests and commit.

Implementation references reviewed: MeshCentral agent networking, Supergateway process/JSON-RPC forwarding, Docker MCP Gateway collision and security rules. They are design references, not dependencies.

Source: accepted Owner scope, inspected repository code and verified eight test suites on DOMKAT 28.10.2026.


## Agent packaging — active implementation plan
1. Inspect MailRu Desktop Inno Setup, release automation and updater (completed 28.10.2026).
2. Create standalone Windows tray agent hosting `ChatGptMcpTunnel.exe --agent`, with DPAPI settings, autostart, reconnect and update helper (source and local build completed).
3. Create first-install and silent-update Inno scripts and Windows GitHub Actions release workflow; do not track binaries. [complete, CI verified]
4. Verify on Windows CI: self-contained publish, agent selftest, MCP smoke, first install, settings survival under update, uninstall hygiene. Published stable numeric v0.1.0. [complete]
5. Connect a second physical computer over protected private networking to DOMKAT's one-tunnel router and verify real GUI/shell and recovery (next after installer release).
- source: direct Owner mandate 28.10.2026, examined MailRu Desktop repository, local build and smoke evidence.
- authority: owner-directive, verified-runtime.

## Role-based installation paths
1. Expose Inno Setup SelectDir page after role selection. [source complete]
2. Suggest D:\ChatGPTMCPAgent for server if D exists; per-user C: program directory for client. [source complete]
3. Preserve manually chosen InstallDir in registry and reuse on update/uninstall. [source complete]
4. Verify defaults and custom folder in Windows CI. [next]
- source: owner directive 28.10.2026, Inno Setup code.
- authority: owner-directive.

## Same-existing-tunnel rollout
1. Remove new OpenAI tunnel creation and require existing Tunnel ID. [implemented]
2. Fix Windows PowerShell 5.1 server launcher and verify local WinForms x64 selftest. [complete]
3. Release/test universal installer v0.2.1 and verify existing-only workflow in Windows CI. [next]
4. Pre-stage SHA256-verified installer on DOMKAT while old MCP channel is live; preserve only Tunnel ID metadata. [next]
5. Execute independent local cutover, erase old runtime/auth and launch server UI for Owner-entered token. Keep source Git repository. [pending]
6. Verify 42 tools and MKATUSHONOK on same plugin. [pending]
- source: Owner instructions and observed local tests 28.10.2026.
- authority: owner-directive, verified-runtime.

## Live DOMKAT repaired
1. Reproduce seven-second restart and identify PowerShell native stderr error handling. [complete]
2. Hotfix installed server launcher with direct native call and verify actual original plugin, 42 tools, local health ports. [complete]
3. Update source installer launcher to permanently prevent the error, build stable v0.2.2. [source complete, release next]
4. Connect MKATUSHONOK through the original DOMKAT Router Lite. [next]
- source: DOMKAT observed 28.10.2026.
- authority: verified-runtime, owner-directive.

## v0.2.2 rollout closed
- Permanent source fix committed; CI run 40712500651 completed success, binary release v0.2.2 published and downloaded with verified SHA256.
- On DOMKAT updated installed server v0.2.1 -> v0.2.2 in-place, role/ID/DPAPI unchanged; restored original plugin connectivity (ping, system.info, healthz/readyz), verified single tunnel-client and Router Lite port 55201.
- Next: physical MKATUSHONOK client onboarding and permissions check only.
- source: verified GitHub release and live DOMKAT inspection 28.10.2026.
- authority: verified-runtime.

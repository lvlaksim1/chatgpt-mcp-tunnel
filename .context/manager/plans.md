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

## v0.2.3 visible UI repair
1. Verify actual original OpenAI tunnel despite false offline label and inspect hidden Server Settings window. [complete]
2. Replace client stderr heuristics for server with independent bounded readyz and Router Lite check. [source complete]
3. Replace blocking ShowDialog with one modeless reusable and focusable Settings window, plus named-event --settings. [source complete]
4. Embed custom original Windows ICO and green/amber/red tray dot into application and both installers. [source complete]
5. Run Windows Inno CI, publish v0.2.3 and update DOMKAT; verify actual visible settings window and status. [next]
- source: Owner requests, observed live DOMKAT, local tests on 28.10.2026.
- authority: owner-directive, verified-runtime.

## Native Settings window acceptance v0.2.4
1. Confirm modeless v0.2.3 Settings form is created but native HWND lacks WS_VISIBLE on DOMKAT. [complete]
2. Manually apply user32 ShowWindow SW_SHOW to exact Settings HWND; confirm foreground window visible via desktop.window.list. [complete]
3. In program, ensure native IsWindowVisible and apply ShowWindow(SW_SHOW) after Form.Show and when reopening; keep no-new-tunnel policy. [source complete]
4. Publish v0.2.4 after Windows installer CI, update DOMKAT in place, verify actual Settings window via --settings and app tray menu. [next]
- source: verified native Windows runtime 28.10.2026, Owner directives.
- authority: verified-runtime, owner-directive.

## Approved Settings forms — two installers
1. Replace permanently visible secret inputs by on-demand SecretInputDialog and status labels, server only copy/generate, client input-only. [source complete]
2. Preserve immutable configured OpenAI Tunnel ID, local address 192.168.0.3, automatic port 55201, DPAPI keys and native Win32 settings visibility. [source complete]
3. Verify self-contained binary, icon/health/forms tests. [local complete]
4. Run Windows Inno CI first/update/preservation/uninstall and publish full+update v0.2.5. [next]
5. Report release links and SHA256; owner performs self-update using existing application UI. DO NOT install on DOMKAT. [owner-controlled]
- source: owner approval and delivery instruction 28.10.2026.
- authority: owner-directive.

## Approved Settings installers — release closed
1. Secret-free settings forms and temporary key entry compiled and tested. [complete]
2. Windows installer CI: full, update, preservation and uninstall. [success: 40732281198]
3. GitHub stable release v0.2.5 binary assets and SHA256. [published]
4. Do NOT upgrade DOMKAT through remote tools; owner will update via application UI. [owner-controlled]
5. Await owner installation results for visual acceptance; retain 192.168.0.3. [future]
- source: verified GitHub release and owner directive 28.10.2026.
- authority: verified-repository, owner-directive.

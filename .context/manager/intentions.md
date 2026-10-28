# Manager intentions and commitments

## Router Lite implementation
- commitment_id: `router-lite-2026-10-28`
- status: accepted/active
- owner: Project Manager `project-manager`
- source: Owner 28.10.2026: «восстанови менеджера проекта chatgpt-mcp-tunnel. передай ему всю эту информацию и пусть приступает к разработке».
- authority: owner-directive
- outcome: one MCP tunnel with explicit computer selection and remote execution of the existing Windows tools.
verified partial progress: stages 1-3 passed local E2E, all 23 Windows tools remote-capable, PNG preserved, parallel calls, bad keys, duplicate IDs, reconnect, old Windows/GUI/UIA regressions passed.
- remaining: cross-machine verification on second physical PC, protected agent provisioning/autostart, operational acceptance and release validation.
- completion gate: functional second physical PC and regression of all 41 original tools; explicit verification, no completion claim from local-only test.

## Maintain independent core plugin
- commitment_id: `independent-mcp-tunnel`
- status: accepted/active
- outcome: preserve the independent project, launcher, GitHub tools and existing Windows capabilities during incremental changes.
- source: prior Owner decisions recorded in `.context/project/*` and repository README.
- authority: owner-directive.


## Agent installer and updater
- commitment_id: `agent-installer-2026-10-28`
- status: accepted/active
- owner: Project Manager `project-manager`
- source: Owner explicitly accepted the MailRu Desktop installer architecture for Router Lite and ordered implementation on 28.10.2026.
- authority: owner-directive
- outcome: a self-contained x64 per-user Windows agent with two Inno Setup installers, DPAPI credentials, tray interface, user-session autostart and GitHub Releases updates.
- verified partial progress: application and runtime built self-contained on DOMKAT; `--selftest` and 42-tool/router/reconnect smoke tests passed; installer source/workflow are written.
- remaining: second physical PC connection, protected network and real cross-host GUI/shell acceptance.
- completion gate: valid downloadable Setup and Update installer assets, CI acceptance, then physical network operation validated separately.

## Installation directory commitment
- commitment_id: install-directory-2026-10-28
- status: accepted/active
- owner: project-manager
- source: Owner requested custom folder selection and role-based D/C defaults on 28.10.2026.
- authority: owner-directive
- verified partial progress: source implementation and CI assertions complete.
- remaining: Inno Setup compile, Windows CI acceptance and stable universal installer release.

## Outcome of universal installer v0.2.0
- Source-complete and CI-verified: universal Setup/Update v0.2.0 released; verified default and custom paths, update preservation and installer cleanup.
- Remaining commitment: clean migration of DOMKAT and cross-host acceptance with MKATUSHONOK, not additional installer design.
- source: GitHub Actions 40696034380, Release v0.2.0, Owner instructions.
- authority: verified-repository, owner-directive.

## Clean local reinstall without creating any OpenAI tunnel
- commitment_id: clean-domkat-same-tunnel-2026-10-28
- status: accepted/active
- owner: project-manager
- source: Owner requested clean DOMKAT installation and token entry, then explicitly confirmed all operations must use the existing OpenAI tunnel.
- authority: owner-directive
- outcome: preserve the logical Tunnel ID, erase old local auth, install new server and enter new token in the UI; do not create another OpenAI tunnel.
- verified partial progress: removed new-cloud-tunnel code, local x64 .NET selftest passes, autonomous staging and cutover scripts written and PowerShell syntax validated.
- remaining: release CI v0.2.1, prepare verified installer staging, actual clean cutover and remote client acceptance.

## Permanent fix for server launcher
- commitment_id: recover-domkat-native-stderr-2026-10-28
- status: accepted/active
- owner: project-manager
- source: Owner requested independently checking/fixing DOMKAT using restored Desktop Commander.
- authority: owner-directive
- verified partial progress: production hotfix restored same tunnel; tests of bridge_ping, Windows/GitHub tools and Router Lite passed; source fixed.
- remaining: build and publish fixed installer v0.2.2, then connect MKATUSHONOK remotely.

## v0.2.2 recovery complete
- commitment_id: domkat-native-stderr-fix-2026-10-28
- status: completed
- owner: project-manager
- source: GitHub Actions v0.2.2 and verified live DOMKAT update 28.10.2026.
- authority: verified-runtime
- verified outcome: signed-off installer CI, live update preserved existing Tunnel ID, one tunnel-client, Router Lite, DPAPI and local autostart; original connector ping, system tools and readyz successful.
- remaining work outside this commitment: physical client MKATUSHONOK connection tests.

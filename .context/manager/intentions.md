# Manager intentions and commitments

## Post-install physical client acceptance (28.10.2026 22:20 MSK)
- commitment_id: physical-client-acceptance-after-v026
- status: active; installed v0.2.6 verified on DOMKAT, cross-machine acceptance NOT PROVEN.
- remaining: check authorized LAN firewall exception and current client key, then demonstrate MKATUSHONOK connection, remote Windows tools/UIA/screenshot and reconnect; reconcile 41 visible/42 advertised tools.
- source: live read-only process/version/TCP checks and original Router Lite owner commitment; authority: verified-runtime, owner-directive.
- Do not re-install remotely, change Tunnel ID/IP/credentials, or expose keys.


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

## v0.2.3 Settings window, server status and icon
- commitment_id: server-settings-tray-icon-2026-10-28
- status: accepted/active
- owner: project-manager
- source: Owner reported false «Нет соединения», inert Settings menu and requested a new icon on 28.10.2026.
- authority: owner-directive
- verified partial progress: source now has modeless Settings, named-event --settings, independent Server readyz/router health and new native ICO with colored tray dot. Self-contained app build, --selftest, --test-icons, --probe-server all returned 0 on DOMKAT.
- remaining: Windows Inno CI acceptance, publish v0.2.3, deploy to DOMKAT and verify visible Settings window and correct tooltip.

## Native visibility supplement to GUI fix
- commitment_id: server-settings-native-visible-2026-10-28
- status: accepted/active
- owner: project-manager
- source: Owner reported inert Settings menu; v0.2.3 callback was reached but native window stayed hidden.
- authority: owner-directive
- verified partial progress: reproducing actual hidden native HWND and successful ShowWindow SW_SHOW on DOMKAT, v0.2.4 source patch and self-contained local selftests.
- remaining: Windows CI installer v0.2.4 and live DOMKAT update with visible-menu acceptance.

## v0.2.5 two installers; do not install on DOMKAT
- commitment_id: approved-settings-both-installers-2026-10-28
- status: accepted/active
- owner: project-manager
- source: owner approved HTML settings mockup then requested full and update installers, explicitly reserving right to use built-in updater personally.
- authority: owner-directive
- verified partial progress: source implements approved server/client secret-free main forms and temporary entry, automated --test-secret-forms plus selftest/icon/probe pass in local publish.
- remaining: validate capsule, commit, GitHub Windows installer CI, publish/verify binary Release. No DOMKAT update or change of 192.168.0.3.

## Installer artifact delivery completed
- commitment_id: approved-settings-both-installers-2026-10-28
- status: completed
- owner: project-manager
- source: GitHub Release v0.2.5 and successful Windows Actions 40732281198.
- authority: verified-repository
- verified outcome: full and update EXEs published with SHA256, release not draft or prerelease, install/update/uninstall checks passed. DOMKAT update deliberately not executed; owner will use built-in program updater.

## v0.2.6 updater fix deliverable
- commitment_id: repair-built-in-updater-ioexception-2026-10-28
- status: accepted/active
- source: owner supplied v0.2.5 updater failure screenshot; source and log identify unclosed staging stream.
- authority: owner-incident, verified-runtime
- completed: scoped file writer, verify complete length, protect existing install on failure; new Windows regression test, local tests.
- remaining: stable GitHub full/update binary v0.2.6 with Windows CI; leave DOMKAT installed software untouched.

## Repair-built-in-updater-ioexception-2026-10-28
- status: completed (source/build/release), pending owner manual deployment and physical client acceptance.
- verified: v0.2.6 stable Setup/Update plus alias and Actions run 40736310056 success.
- source: verified GitHub Release v0.2.6.
- authority: verified-repository, owner-directive.

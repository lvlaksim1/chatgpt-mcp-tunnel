# Manager intentions and commitments

## 29.10.2026: v0.2.17 multi-chat source implemented, pending Windows CI and real ChatGPT
- Deliver v0.2.17 with a usable per-computer multi-chat WinForms UI and MCP chat_id model without disturbing legacy history. Keep automatic target:new handoff experimental until actual ordinary ChatGPT acceptance.
- Publish/verify Windows Setup and Update CI, deploy cautiously to DOMKAT using independently verified update, refresh tool catalog (tool count remains 52), test real new-chat transfer from GUI and chat.reply. Do not reenable hourly cloud automation; preserve Windows tunnel watchdog.

## 29.10.2026: REAL ordinary ChatGPT ui/message acceptance 19:36-19:42 MSK
- Live ui-message-2026-10-29 active host acceptance now PASS on DOMKAT 0.2.16 for manual, queued, real GUI, background-tab. Commitment remains OPEN for persistent iframe lifetime and reliable always-available behavior when browser closed/suspended.
- Fix misleading local chat footer and verify remote MKATUSHONOK GUI roundtrip in next phase. Hourly task may be disabled only after owner decision and full persistence acceptance.

## 29.10.2026: DOMKAT production update 0.2.16 and ChatGPT browser tool refresh
- commitment ui-message-2026-10-29: DOMKAT installation 0.2.16 and explicit owner-requested browser action refresh now DONE. Remaining goal: real ChatGPT chat.ui.open availability in fresh host session, user-click ui/message versus automatic local queue, chat.reply, lifecycle behavior; claim no scheduler only after live proof.
- Maintain separate issue: updater should automatically coordinate agent/watchdog shutdown to avoid silent Inno exit 5 on an active installation.

## 29.10.2026: v0.2.16 stable Windows release verified (not live ChatGPT accepted)
- v0.2.16 Windows CI/release complete (40907216014 SUCCESS). Commitment remains ACTIVE until owner installs and refreshes connector, then validates user-click and automatic ui/message separately in real ordinary ChatGPT.

## ui/message v0.2.16 diagnostic version pending production acceptance
- commitment: ui-message-2026-10-29, active. Build/release extended probe, require physical owner update and refresh ChatGPT MCP actions, then test explicit-click message and timer-based queue forwarding separately. Do not claim either real behavior tested yet; preserve existing one-tunnel network.

## v0.2.15 release completed — real host acceptance still active
- Source, simulation, Windows installation and release checks completed. Main commitment remains active until real ordinary ChatGPT host accepts the timer-initiated ui/message and sends reply via chat.reply. Do not confuse successful GitHub Actions with real ChatGPT behavior.

## MCP Apps ui/message experiment accepted 29.10.2026
- commitment_id: direct-chat-ui-message-2026-10-29. Owner authority: explicitly implement MCP-only ui/message.
- status: active; prototype and local simulated-host verification done, stable CI release and LIVE ChatGPT host acceptance remain.
- objective: send user message from existing local GUI to ordinary ChatGPT through MCP tools and ui/message, no scheduler, other API, Codex or Work. Existing OpenAI tunnel and Router Lite preserved.
- no completed claim unless actual ordinary ChatGPT ui/message invokes a new turn, invokes chat.reply, and the reply appears in local chat.

## Chat attachments v0.2.14 — CI/release complete, physical acceptance active
- commitment_id: chat-window-attachments-2026-10-29
- state: source/local tests and GitHub Windows Setup+Update stable release v0.2.14 COMPLETED, 40866882778 SUCCESS.
- active remaining responsibility: owner-controlled installed update, real clipboard screenshot insertion and file attachment send, live ChatGPT read of actual image through refreshed plugin schema.
- Do not alter installed DOMKAT/MKATUSHONOK or network credentials under this release-only step. Source: verified CI and GitHub Release 29.10.2026.

## Chat attachments v0.2.14 — accepted 29.10.2026
- commitment_id: chat-window-attachments-2026-10-29
- status: accepted/active; source implementation and local tests completed; release and physical owner-facing acceptance pending.
- owner: project-manager. Source: direct owner order 29.10.2026.
- outcome: file picker, multi-attachments preview/remove, Explorer drag/drop, Ctrl+V clipboard images, text+files or files-only message, MCP reading as real image/text blocks and local verified path for binary documents.
- completion gate: stable Windows CI/installers, owner-controlled deployment, physical Ctrl+V and file selection verification, ChatGPT tool catalog refresh and native attachment read; preserve OpenAI tunnel identity and network configuration.

## Direct WAN deployment and physical acceptance, 29.10.2026
- commitment_id: direct-wan-wss-2026-10-29
- status: code, local tests and stable v0.2.8 release completed; owner-controlled physical deployment and verification remain active.
- owner: project-manager
- Решение владельца 29.10.2026: DOMKAT имеет статический публичный IPv4 на WAN-интерфейсе маршрутизатора; удалённый клиент расположен в другой локальной сети. Развивается прямой защищённый WSS без обязательного Tailscale/посредника. Локальный адрес DOMKAT не заменяется внешним IP. Источник: прямая команда владельца; код основной ветки f8cc66848a475030b63ad5c15e25ec6c68e76308.
- Remaining: verify server/local IP, install v0.2.8 through Owner-controlled updater; configure WAN rule and router TCP 55202 forwarding on explicit Windows/router interaction; transfer and independently verify SHA-256 certificate fingerprint and 32+ character key; test MKATUSHONOK from separate network, all 23 remote Windows functions, screenshots/UIA, reconnect, invalid cert/key. Do not close before live proof.

## Direct WAN option after validated public router IP
- commitment_id: compare-direct-wss-vs-vpn-2026-10-28
- status: read-only feasibility verified; adoption pending Owner decision; no changes to server/router/firewall.
- Reassess need for Tailscale/Headscale/VPS because DOMKAT router has a verified public WAN IPv4; design secure WSS or equivalent cryptographic channel and authenticated agents before public inbound exposure.
- Explicitly keep production unchanged until new transport is selected/tested; retain v0.2.7 Tailscale feature as fallback.
- source: owner static-IP statement, verified router GetExternalIPAddress and NAT/firewall inspection 28.10.2026.


## Tailscale Router Lite integration v0.2.7
- commitment_id: tailscale-cross-network-v027-2026-10-28
- status: implementation/release completed; physical deployment and client E2E acceptance active
- verified: v0.2.7 CI run 40756470762 success, Setup/Update available, local Windows and router tests passed. One client IP is authorized per explicit firewall confirmation; LAN compatibility retained.
- remaining: Tailscale installation/sign-in on BOTH physical PCs, owner-side in-place update of DOMKAT if desired, Tailscale IP binding, peer-specific firewall rule, key entry, real MKATUSHONOK connection, 23 remote Windows tools and reconnect acceptance.
- source: owner execution command, GitHub commit c04e2ec, Windows CI 40756470762, Release v0.2.7.
- Do not claim working inter-network link before observed remote test; do not modify installed DOMKAT or IP/Tunnel ID through manager.


## Межсетевое соединение компьютеров — уточнённое обязательство
- commitment_id: cross-network-transport-after-owner-correction-2026-10-28
- status: active; технический способ ещё не согласован, проверка двух физических компьютеров не выполнена.
- Установленный владельцем факт: DOMKAT и MKATUSHONOK находятся в разных локальных сетях. Сохраняем один OpenAI MCP Tunnel и Router Lite.
- До подключения MKATUSHONOK необходимо спроектировать безопасный межсетевой транспорт. Рекомендуемый для проверки вариант — виртуальная зашифрованная сеть; альтернативой является защищённый WSS-посредник. Не считать одну кнопку LAN-брандмауэра решением.
- source: прямая поправка владельца и проверка кода v0.2.6 28.10.2026; authority: owner-directive, verified-repository.


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

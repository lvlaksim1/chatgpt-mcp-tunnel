# Next actions

## Immediate next: actual ui/message acceptance
1. Owner installs v0.2.15 update on DOMKAT and refreshes ChatGPT MCP connector actions.
2. Open chat.ui.open in ordinary ChatGPT, enable relay, send a test message through existing local chat.
3. Confirm new model turn, chat.reply and reply visible in local chat; test widget lifecycle and rejection path. Explicitly distinguish confirmed vs hypothetical.
4. Keep existing router/Tunnel ID intact; do not deploy to MKATUSHONOK without approval.

## Next for MCP Apps ui/message v0.2.15
1. Commit source/test/docs with sealed manager generation and run official Windows GitHub Actions installer release; check setup/update/uninstall preservation and status.
2. Provide Windows update link only after release succeeded; do not auto-restart production DOMKAT or remote MKATUSHONOK without explicit instruction.
3. In ordinary ChatGPT refresh plugin tools, invoke chat.ui.open; enable relay and send a local text message. Verify new chat turn and chat.reply actually fires; document refusal/teardown evidence.
4. If host does not permit unattended ui/message from active iframe, retain original queue, report exact rejection and test an MCP-only alternative.

## Next after stable v0.2.14 release
1. Offer published Setup/Update from https://github.com/lvlaksim1/chatgpt-mcp-tunnel/releases/tag/v0.2.14; hashes are recorded in current state.
2. Owner uses built-in updater or verified Update installer on DOMKAT; do not deploy remotely without explicit authorization. Preserve data, TLS pin, keys and one OpenAI tunnel.
3. Test physical file picker, drag-and-drop, pasted screenshot Ctrl+V, attachment-only message, thumbnails/remove, chat history and 49th MCP tool after connector refresh.
4. Verify actual model reads PNG as MCP image and file content via chat.attachment.read. Only then close commitment.

## Next actions: Chat attachments v0.2.14
1. Commit source/tests/docs and sealed manager generation atomically; preserve unrelated untracked files.
2. Run Windows GitHub Actions installer release v0.2.14, verify Setup/Update, installer acceptance, chat GUI/attachment test, protocol and Router Lite regression.
3. Validate release hashes and CI; report factual status.
4. Owner-controlled update of installed DOMKAT/MKATUSHONOK only upon explicit deployment permission. Verify real attachment picker and Ctrl+V screenshot; refresh ChatGPT tools and call chat.attachment.read.
5. Keep commitment active until physical acceptance; no production network/keys/Tunnel ID changes.

## Next after physical large-file WAN acceptance 2026-10-29
1. Physical original-file transfer SUCCESS: MAX.msi 324820992 bytes, full SHA-256 match on DOMKAT, resumed after brief reconnect. Copy exists at C:\Users\Maksim\Downloads\MAX.msi.
2. Maintenance: fix TransferControlPipe.RunClientAsync false nonzero exits on status JSON with error:null or an array listing; test and release.
3. Refresh the attached ChatGPT plugin schema (currently 41 exposed tools, runtime 45 incl. 3 file.transfer tools); v0.2.10 local CLI works already.
4. Optionally validate WinForms transfer window visually in another transfer. Installation of MAX is separate and not part of this acceptance task.


## Priority: publish and deploy v0.2.9 — 29.10.2026
1. Commit code, release v0.2.9 and pass GitHub Actions Windows installer tests.
2. Update DOMKAT and MKATUSHONOK while preserving WSS credentials and TLS pin. Verify the three new file.transfer tools.
3. Copy the original MKATUSHONOK C:\Users\m.katushonok\Downloads\MAX.msi to DOMKAT through Router Lite. Check 324820992 bytes, SHA-256 14A528C373B19B35B3C9E77B5FAEE88DC34A073F15D8801673DDB7C040B2D48F and tray progress.
4. Keep test PASS distinct from physical WAN acceptance; MAX installation is a separate task.


## Next: v0.2.8 direct WSS deployment and actual separate-LAN acceptance — 29.10.2026
1. Owner-controlled DOMKAT Update from verified running v0.2.7 to v0.2.8; keep old cloud Tunnel ID and local Router Lite address unchanged.
2. Configure public IPv4 or domain in Server settings, enable WSS, save/generate PFX and copy verified SHA-256 pin; use a strong 32+ char client key.
3. With owner action and Windows UAC, permit TCP 55202 for router executable, then manually forward public TCP 55202 to DOMKAT LAN TCP 55202. Never forward plaintext 55201.
4. On MKATUSHONOK (different LAN) enter public IP, select secure mode and pin, enter agent key; verify from that physical PC.
5. Validate 23 remote Windows tools, screenshot/UIA and reconnect; record live logs/results and only then close physical acceptance.
- Code: f8cc6684; Release v0.2.8; CI 40781905752 SUCCESS. Existing DOMKAT install not changed.

## Next after verified public WAN IP
1. Present verified feasibility: RT-GM2-3 has public WAN IPv4 matching external Internet egress; IP permanence reported by Owner; no CG-NAT at the router. Exact IP omitted from public repo.
2. Choose with Owner whether to develop self-hosted direct WSS/TLS + strong agent authentication instead of Tailscale. Existing v0.2.7 optional Tailscale solution stays available.
3. Only after secure server is ready and Owner authorizes network changes: configure explicit port forwarding and least-privilege Windows Firewall; never expose existing raw ws:// Router Lite on public WAN.
4. Validate inbound from remote MKATUSHONOK and 23 tools, screenshot/UIA, reconnect. No success claim before actual external proof.
- source: real router WAN IP SOAP and firewall read-only check 28.10.2026.


## Next actions after v0.2.7 release
1. On DOMKAT and MKATUSHONOK install Tailscale from the official Windows package and sign both into one permitted tailnet, completing interactive browser authorization; no public router port forwarding.
2. Read each computer's own 100.x Tailscale IP. Preserve the working OpenAI tunnel; update DOMKAT Agent to v0.2.7 via owner-approved method when ready.
3. In DOMKAT server Settings choose Tailscale interface address as Router Lite listening address (port 55201); then use the new Tailscale firewall button and approve exactly MKATUSHONOK's Tailscale IP in Windows UAC.
4. On MKATUSHONOK set the DOMKAT Tailscale IP, enter protected client key and connect. Observe Tailscale ping, client error classification and actual computer selection.
5. Run real remote tests for files, processes, PowerShell, screenshot, UIA, reconnect. Keep physical acceptance NOT PROVEN until observed success; update manager then.
- Official release v0.2.7 and CI are complete; no re-build needed for the existing scope.


## Приоритет после уточнения владельца: компьютеры в разных сетях
1. Не направлять MKATUSHONOK на 192.168.0.3:55201 без подтверждённого защищённого маршрута между сетями. Не использовать настройку доступа для локальной подсети как решение задачи связи через интернет.
2. Определить с владельцем допустимый сетевой транспорт: предпочтителен простой защищённый VPN (например, Tailscale), альтернатива — отдельный WSS-посредник. Предложение VPN не означает одобрение установки стороннего ПО.
3. Проверить/скорректировать привязку маршрутизатора к VPN-интерфейсу и сетевые ограничения; стандартная кнопка локального брандмауэра v0.2.6 не поддерживает VPN-адреса 100.64.0.0/10.
4. После наличия маршрута выполнить реальную проверку подключения MKATUSHONOK, ключа/имени и работы Windows-команд, экрана, UIA, изображений и переподключения.
5. Сохранить один OpenAI Tunnel ID, один MCP-плагин, текущие установленный DOMKAT v0.2.6 и локальную конфигурацию; без несанкционированной установки/проброса публичных портов.
- source: прямая поправка владельца и репозиторий Router Lite 28.10.2026.


## Current execution priority — 28.10.2026 22:20 MSK
1. Do not re-install v0.2.6: installed and running agent FileVersion 0.2.6.0 is verified.
2. Check server LAN firewall permission and current client key through approved UI/secure procedure; owner authorizes any UAC firewall operation; do not disclose secrets.
3. Connect real MKATUSHONOK client to 192.168.0.3:55201; inspect state/logs and distinguish network, 401 authorization and 409 duplicate-name errors.
4. On verified connection, exercise machine-targeted remote Windows file, shell, screenshot, UI Automation and reconnect; check 41 vs 42 tool exposure.
5. Persist cross-host evidence and conclude acceptance only after positive tests; preserve original cloud tunnel identity, current IP and live installed application.
- source: verified live DOMKAT and published v0.2.6 28.10.2026.


1. Prepare and test installation of Router Lite agent on a **second physical Windows computer**, using a protected private network (or WSS/TLS). Do not expose plain WebSocket with secret to the internet.
2. Verify physical PC identification, 23 tools, remote screenshots, UIA, machine-specific paths, reconnect and errors; run full regression on DOMKAT again before live replacement.
3. Add simple agent provisioning, protected local secret storage and Windows sign-in autostart, while retaining one binary/repository/plugin and avoiding a database.
4. Update launcher for explicitly configured Router Lite; release a stable numerical version only after acceptance, update manager capsule and document real cross-host evidence.

Stages 1–3 were completed and locally verified on DOMKAT; do not repeat that work without a regression or new finding. Source: Owner task and verified tests (28.10.2026).

5. Publish agent installer source in `main`, trigger Windows GitHub Actions release workflow for numeric v0.1.0, inspect output/repair failures until Setup and Update EXE are published. Verify installation, update preservation and uninstall.
6. After binary release, configure DOMKAT Router Lite and connect a second Windows PC via a protected network; perform physical multi-PC acceptance.

## Folder acceptance
- Compile and validate both role-specific paths and custom folder preservation in Windows CI; publish universal installer when passing.
- Do not remove the existing DOMKAT tunnel until the new installation and independent recovery procedure are verified. The Owner will input a new token for the clean installation.

## Next after v0.2.0 release
- Prepare a safe autonomous transition of DOMKAT from the old running tunnel to clean Server v0.2.0 installation without preserving the old credentials; let owner enter a new token directly in the UI.
- This transition will terminate the MCP channel. Ensure a recoverable local installer and startup procedure are present before shutting down the old tunnel.
- Then connect MKATUSHONOK as Client via the sole DOMKAT router and verify remote controls across physical PCs.

## Next existing-tunnel migration
- Publish and validate Windows v0.2.1 installer with PowerShell syntax checking and without new-cloud-tunnel API calls.
- Stage verified installer with Prepare-Clean-Reinstall-DOMKAT.ps1, preserving only existing Tunnel ID.
- Run independent Start-Clean-Install.cmd to remove old local files and install server; Owner enters token in new program.
- Verify connectivity of same ChatGPT plugin and MKATUSHONOK remote tools.

## Next after live repair
- Commit and build numerical v0.2.2 with safe native stderr handling via Windows installer CI, verify release assets.
- Preserve existing DOMKAT setup, token and Tunnel ID; do not clean-reinstall again.
- Connect MKATUSHONOK to 192.168.0.3:55201/agent in the protected local network using its own agent key and verify cross-host tooling.

## After verified v0.2.2 deployment
- Configure and test MKATUSHONOK as Client of existing DOMKAT Router Lite, endpoint ws://192.168.0.3:55201/agent on the trusted LAN, using the existing locally stored agent-specific connection key.
- Check Windows Firewall trusted-network reachability and do not expose ws to public Internet.
- Verify remote system.info, files, screenshot and UI Automation against MKATUSHONOK with no extra OpenAI tunnel. Preserve current DOMKAT release and working plugin.

## v0.2.3 rollout
- Commit source and original ICO, build/test full and update installers in Windows GitHub Actions, publish v0.2.3.
- Update DOMKAT in-place using verified installer while retaining one original OpenAI Tunnel ID and protected keys.
- Verify tray icon and green connected marker, correct «Сервер подключён» tooltip and Settings window via menu and --settings.
- After UI acceptance return to physical MKATUSHONOK client tests.

## v0.2.4 native visible settings
- Run Windows GitHub Actions installer build and update acceptance for v0.2.4, publish stable binaries.
- Verify SHA256 and update DOMKAT in place, preserving existing Tunnel ID/DPAPI keys.
- Run installed --settings and confirm desktop.window.list finds visible foreground Server Settings window without any external ShowWindow command; test reopen and successful server state.
- Then continue remote MKATUSHONOK acceptance.

## Release deliverable
- Commit v0.2.5 source and approved manager capsule, dispatch Windows Actions release, confirm run success.
- Verify GitHub release includes full ChatGPT_MCP_Tunnel_Setup_v0.2.5.exe and update ChatGPT_MCP_Tunnel_Update_v0.2.5.exe with SHA256 digests; built-in updater compatibility should be preserved.
- Provide links only, do not install/upgrade the active DOMKAT server. Wait for owner self-update and further acceptance feedback.

## After release
- Provide links for full Setup and Update v0.2.5 and report successful CI.
- Do not install on DOMKAT. Owner updates existing app themselves using built-in updater.
- Upon owner confirmation, inspect updated app and follow up only with requested UI fixes; keep existing OpenAI Tunnel ID, DPAPI secrets, IP 192.168.0.3.

## Deliver v0.2.6 repair binaries
- Commit source+manager capsule, dispatch Windows installer workflow for v0.2.6, verify all tests, 2 Release EXEs and SHA256.
- Explain one-time manual update necessary from installed v0.2.5; subsequent in-app updates repaired.
- Leave installed DOMKAT version, keys, tunnel, IP 192.168.0.3 untouched.

## v0.2.6 client connectivity follow-up
- Publish both full/update installers with Windows tests for staging, firewall subnet calculation and client WebSocket error states.
- User self-installs v0.2.6, uses Server Settings «Разрешить доступ клиентам в локальной сети» then Windows UAC confirmation; client's IP/hostname and shared key are entered separately, save/connect.
- Confirm connectivity from physical client. If still failing, gather its on-machine agent.log/error (network vs 401 vs 409), do not speculate.

## Post-release manual owner tasks
- Owner self-installs v0.2.6 Update on DOMKAT.
- In server Settings use LAN-only firewall permission button, accept UAC, copy current server client key.
- On MKATUSHONOK use server address 192.168.0.3, enter copied key and save/connect. Observe client status; on failure distinguish network, 401 key, 409 name and gather client logs.
- No remote DOMKAT upgrade by project manager; do not change cloud Tunnel ID, DPAPI secrets, IP.

# Latest handoff

## Handoff after v0.2.14 stable release 29.10.2026
- The new local chat attachments and Ctrl+V screenshot feature is published. GitHub Actions run 40866882778 SUCCESS, full Setup/Update installation/upgrade/uninstall acceptance passed; release v0.2.14 stable, source commit 21a1a0c.
- 49 MCP tools in new runtime, including chat.attachment.read, verified image and UTF-8 text as real tool content; 23 remote Router Lite tools/reconnect preserved.
- Setup SHA256 7ff5afc44db3a535b71e11b6f174141684f666dd0396c303fa03ee64d2d8174d; Update/legacy alias SHA256 ef4899062bde3caecb85fc29d23c03a155ea78d66902d6d49da761bf4f4504ef.
- Installed DOMKAT and MKATUSHONOK remain v0.2.13 because owner updates them personally; their installed GUI has not been tested for new feature. ChatGPT cached tool list 45 needs refreshing.
- Next: owner update and observed physical clipboard/file message; do not declare deployed without evidence.

## Handoff: chat attachments v0.2.14 code accepted, release pending — 29.10.2026
- Owner requested attach-and-send files and Ctrl+V clipboard images in existing ChatGPT MCP Tunnel chat window.
- Implemented GUI selection/drag-drop/clipboard image/preview/removal/file-only sends; queue stores copies with SHA-256; chat.receive includes attachments, chat.attachment.read emits MCP real image or UTF-8 text or verified local binary path.
- Verification: .NET zero warnings/errors, UI attachment test exit 0, tests/test_chat_attachments.py all E2E passed; original MCP 49 tools, Router Lite 23 remote tools and reconnection passed.
- Windows installer release v0.2.14 and real owner GUI test pending. Installed DOMKAT/MKATUSHONOK remain v0.2.13. ChatGPT plugin snapshot 45 needs refresh.
- Two unrelated untracked user text files and production tunnel/security/network unchanged.

## Handoff after physical 325 MB WAN transfer accepted 29.10.2026
- Source MKATUSHONOK C:\Users\m.katushonok\Downloads\MAX.msi; destination DOMKAT C:\Users\Maksim\Downloads\MAX.msi.
- Job 4afa737122d1400dbacfa3e48cd39bee completed, 324820992 bytes, SHA-256 14A528C373B19B35B3C9E77B5FAEE88DC34A073F15D8801673DDB7C040B2D48F; signature valid. Recovered after reconnect at 80.7%.
- DOMKAT v0.2.10 / MKATUSHONOK v0.2.9. Live transfer via same-user control CLI over WSS, no file payload in chat. Plugin still exposes only 41 tools.
- CLI status exit code parsing bug to fix, GUI visual live-check optional. MAX installation not part of this task.


## Direct static WAN architecture implemented and released — 29.10.2026
- Решение владельца 29.10.2026: DOMKAT имеет статический публичный IPv4 на WAN-интерфейсе маршрутизатора; удалённый клиент расположен в другой локальной сети. Развивается прямой защищённый WSS без обязательного Tailscale/посредника. Локальный адрес DOMKAT не заменяется внешним IP. Источник: прямая команда владельца; код основной ветки f8cc66848a475030b63ad5c15e25ec6c68e76308.
- Сервер Kestrel обслуживает защищённый порт TCP 55202 в дополнение к прежнему локальному TCP 55201. Сервер создаёт собственный TLS-сертификат, шифрует пароль PFX через DPAPI Windows, клиент проверяет точный SHA-256 отпечаток до передачи общего ключа. Новый режим требует минимум 32 символа общего ключа. Проверены сборки, 42 инструмента, 23 направляемых инструмента Windows, запрет неверного сертификата, выполнение удалённой команды по WSS, восстановление связи, сохранение старого LAN-подключения; GitHub Actions 40781905752 завершён успешно; v0.2.8 опубликована с установщиками Setup и Update.
- Проверка 29.10.2026 через работающий MCP Tunnel: активный процесс ChatGptMcpAgent.exe PID 14536 имеет FileVersion 0.2.7.0 (запущен 2026-10-29 00:33:36 по системному времени Windows); Router Lite слушает 192.168.0.3:55201 (PID 2992), TCP 55202 не слушает. v0.2.8 не устанавливалась менеджером; bridge_ping успешен. Этот факт supersedes прежний текущий статус v0.2.6, но не меняет вывод о непроверенном межсетевом соединении. На маршрутизаторе не создавался проброс TCP 55202, правила брандмауэра не менялись. Физическая связь с MKATUSHONOK через Интернет и 23 удалённых инструмента остаётся НЕ ДОКАЗАНА. Публичный WAN IPv4 подтверждён чтением роутера; постоянство адреса основывается на словах владельца.
- Next action: owner-controlled v0.2.8 install and WAN rule/forwarding, then physical MKATUSHONOK WSS connectivity and full tool acceptance. Tailscale remains optional fallback, not required.
- Evidence: docs/direct-wan.md, commit f8cc66848a475030b63ad5c15e25ec6c68e76308, Windows Actions 40781905752 success, GitHub Release v0.2.8.

## Verified public WAN on DOMKAT — topology re-evaluation 28.10.2026
- Owner reports a static IP; direct read-only IGD UPnP router RT-GM2-3 verification shows globally routable WAN IPv4 identical to three outside IP service readings. This is much stronger than the Xray/TUN-influenced outbound address; static assignment over time is user assertion. Exact IPv4 withheld from public repo.
- Existing router local IP is 192.168.0.1, DOMKAT 192.168.0.3. Windows Router Lite TCP 55201 bound only LAN, firewall restricted to 192.168.0.0/24. Router replied UPnP 714 for mappings TCP 55201 and 443. Manually configured forwards unknown, and no test was performed from a second network.
- Direct secure WSS remote agents without Tailscale/Headscale/cloud mediator are feasible in principle. DO NOT expose current raw ws:// and client key. Need TLS + agent authentication, explicit safe inbound port mapping, firewall and real external testing.
- Released v0.2.7 Tailscale functionality remains a working development option; Owner has not yet approved replacing that approach with direct WSS, and running installed v0.2.6 remains untouched.
- source: DOMKAT read-only shell and router GetExternalIPAddress/GetSpecificPortMappingEntry, Owner 28.10.2026.


## v0.2.7 Tailscale implementation/release handoff — 28.10.2026 22:45 MSK
- Owner clarified DOMKAT and MKATUSHONOK are in DIFFERENT local networks and then instructed to execute the proposed protected inter-network approach. Chosen Tailscale/WireGuard overlay preserves ONE OpenAI Secure MCP Tunnel and Router Lite.
- Source c04e2ec adds a narrowly scoped per-peer Tailscale Windows Firewall rule (only exact client IP → exact server IP, TCP 55201, runtime executable), validation of local Tailscale adapter, UAC confirmation, automatic server Settings button label and new selftest. No existing LAN feature removed.
- Local Windows selftests and MCP 42-tools/router/reconnect passed, Windows CI run 40756470762 completed success, v0.2.7 stable full/update installers published with SHA256. Physical VPN installation/login and real MKATUSHONOK connection not yet executed or verified.
- Working DOMKAT v0.2.6 was NOT upgraded/restarted. No firewall/IP/Tunnel ID/keys changed during development.
- Next: interactive Windows Tailscale installation/authentication on BOTH PCs, ensure same authorized tailnet, owner-side v0.2.7 in-place update, server listen on own VPN IP and exact peer firewall approval, client own key and physical E2E test.
- source: Owner directive, local runtime tests, GitHub Actions/Release 28.10.2026; authority: owner-directive, verified-runtime, verified-repository.


## Критическая поправка владельца — разные локальные сети (28.10.2026)
- Владелец уточнил: DOMKAT и MKATUSHONOK НЕ находятся в одной локальной сети. Указанный ранее клиентский адрес 192.168.0.3:55201 — лишь частный адрес DOMKAT; инструкции подключения к нему без виртуальной сети или иной маршрутизации ошибочны.
- Один OpenAI MCP Tunnel сохраняется. Для реальной связи между удалёнными агентами нужен отдельный защищённый интернет-маршрут: рекомендуемый к оценке Tailscale/WireGuard, либо WSS-посредник. Решение по установке/транспорту ещё не принято.
- Код v0.2.6 принимает частные VPN-адреса Tailscale для ws:// клиента, но сервер должен слушать правильный интерфейс, а LanFirewall.LocalNetwork не поддерживает 100.64.0.0/10; стандартная кнопка LAN-доступа не подходит для VPN-сценария без адаптации/безопасной настройки.
- На DOMKAT сейчас не обнаружены VPN-службы/адаптеры указанного типа. Нет данных о наличии VPN на MKATUSHONOK. Раздельная сеть — факт со слов владельца; реальные параметры NAT/доступности не проверены.
- Следующий шаг: согласование сетевого транспорта, затем защищённая настройка и испытание реального MKATUSHONOK; не менять работающий Tunnel ID, программу или публичную доступность пока решение не принято.
- source: owner-directive 28.10.2026, read-only проверка DOMKAT и прочитанные исходники v0.2.6.


## Reinstantiation reconciliation — 28.10.2026 22:20 MSK
- Project Manager project-manager restored from generation 16, all 8 coupled Git blob digests matched. PM 2.1.0 and Capsule 1.3.1; both authority branches main.
- Original bridge_ping responded CHATGPT_MCP_TUNNEL_PING_OK. DOMKAT is now running agent FileVersion 0.2.6.0 (PID 30196, started 20:38:25 MSK), so the older v0.2.6 manual-install pending state is superseded.
- Router Lite listens at 192.168.0.3:55201. At this snapshot no established physical client connection was observed; 41 tools visible in ChatGPT against previously advertised 42. Stable release v0.2.6 and CI 40736310056 verified.
- Physical MKATUSHONOK acceptance still NOT PROVEN. Next: approved LAN firewall/key verification followed by real second-PC connection, remote Windows/GUI tests and reconnect. No deployment, secret, IP, tunnel identity or firewall change occurred.
- source: original MCP Tunnel runtime/GitHub tools 28.10.2026; authority: verified-runtime, verified-repository, owner-directive.


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

## GUI status/Settings and icon in v0.2.3
- Owner sees generic tray icon, false «Нет соединения» and an inert Settings menu.
- Confirmed hidden Server Settings HWND. Corrected modal ShowDialog/guard to modeless reused Form, exposed named-event --settings, added async server readyz+Router TCP and custom native ICO with colored state dot.
- Local source build and win-x64 --selftest, --test-icons, --probe-server pass. Windows Inno CI and actual installed UI verification pending. Existing DOMKAT tunnel retained.

## v0.2.4 follow-up for actual native Settings visibility
- v0.2.3 Settings menu callback did execute, but its HWND was hidden. Win32 ShowWindow SW_SHOW on live DOMKAT made the Settings window appear; desktop.window.list confirmed correct foreground bounds.
- v0.2.4 patches reusable Form show logic to enforce IsWindowVisible/ShowWindow after Form.Show and when reopened.
- Local self-contained build, selftest, icon test, live read-only server probe all pass. Next Windows CI/release and real DOMKAT --settings acceptance.

## v0.2.5 Windows installer release — user self-updates
- User approved secret-free main Settings mockup for Server/Client. Current v0.2.5 source implements on-demand temporary secret popup, separate token replacement on Server, Server key generate/copy, input-only key on Client, automatic port 55201, keep configured address 192.168.0.3 and existing OpenAI Tunnel ID.
- Tests --selftest, --test-icons, --probe-server, --test-secret-forms passed with self-contained executable.
- User explicitly instructed: prepare full and update installer; do NOT update DOMKAT yourself. Owner will update via built-in UI.
- Next: release Windows GitHub Actions, verify both installer assets and report.

## Release v0.2.5 delivered; user updates DOMKAT
- Stable GitHub Release v0.2.5 and CI run 40732281198 success.
- Full Setup SHA256 57b811467bf4a79a56ae8d8f99965ca36840745b853d357ece29a5a6e105f13a; Update SHA256 278140cb9ee4ed7a1bfcaa500a3b38d27937520185645cc30b6dc75c40ff112b. Release includes third legacy-named update alias to support existing update mechanism.
- Contains accepted server/client secret-free Settings UX; inline buttons open temporary secret popup. Client key has no reveal/copy. IP still 192.168.0.3, port 55201 automatic.
- Crucial: owner requested no remote update; DOMKAT existing installation was left unchanged. Wait for owner self-update acceptance.

## v0.2.6 Windows downloader bug
- User screenshot showed 100% and file-in-use error. Cause: await using var output lasted through File.Move; .part still locked on Windows. Log has Update failed IOException twice; installed DOMKAT v0.2.5.
- v0.2.6 closes stream before rename, validates full length, tests overwriting/partial cleanup/exclusive open, --test-updater-download success.
- Owner self-updates: deliver full and update installers; must manually run v0.2.6 update installer once. Do not execute local install remotely. IP 192.168.0.3 and credentials remain.

## MKATUSHONOK connectivity regression and v0.2.6 fix
- Owner reported «клиент не подключается». Server DOMKAT works: 192.168.0.3:55201 listens, OpenAI readyz 200. Public Wi-Fi Windows Firewall inbound Block, no inbound allow rule. No direct MKATUSHONOK test available; likely firewall but not conclusively proven.
- Server key file updated 20:17:08, so client must use latest copied key; Router Lite returns HTTP 401 wrong key, 409 duplicate machine name.
- v0.2.6 adds server LAN-only firewall permission button with Windows UAC, scope specific runtime, IP, TCP 55201, local subnet. Client shows appropriate WebSocket failures. This and updater file-lock fix tested locally.
- Owner must self-update; remote installed DOMKAT app and firewall remain unchanged.

## Stable v0.2.6 released; client acceptance pending
- Successful GitHub Windows CI 40736310056, source 37702f749cd6b391a4eda4fa7766ed9c3a59a847.
- Full Setup SHA256 66a12e056afa3bf3822e878ded51f8d3bd32f96f13ba12faa94575bf8f17484d, Update and alias SHA256 2053ccadc645acfb0a79d5f7a3f4aef91453da8401db34f13bd97411805ede9b.
- v0.2.6 fixes Windows staging writer file lock, adds server UI LAN firewall UAC scoped to runtime, LAN address, TCP 55201 and local subnet, and visible 401/409/network errors on client.
- DOMKAT up at 192.168.0.3:55201 readyz 200 before release; firewall Public BlockInbound with no allow rule. Client shared secret modified 20:17:08. Physical MKATUSHONOK-side connectivity not checked. Do not claim resolved until actual test.
- User must manually install v0.2.6 Update; never remote-upgrade. Afterwards click server LAN rule button and enter current copied key on client.

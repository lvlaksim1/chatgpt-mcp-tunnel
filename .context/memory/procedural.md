# Procedural memory

## 30.10.2026 — эксплуатация менеджера и фиксация новой версии
1. При входе в обычный диалог восстановить `manager_id=project-manager` по `.context/ENTRYPOINT.md` и `state-integrity.json`, не отождествлять случайное имя в сообщении с новой ролью supervisor. Диалоги — временный runtime; identity — постоянная капсула.
2. Сверять версию установленного EXE напрямую на DOMKAT перед статусом; 30.10.2026 проверено `0.2.28+ad28e28`. Выпуск `v0.2.28` уже размещён и пользователь установил его сам; не повторять выпуск/обновление без отдельного поручения.
3. Для своих компьютерных вызовов (если схема MCP инструмента поддерживает) использовать `executor="project-manager"`; для обычного чата нет executor → ChatGPT. Подпись не предназначена для прав доступа. После релиза обновить/проверить схему Host и протестировать реальное overlay отображение.
4. Перед новым независимым планом перечитать текущее состояние поверх архивных ранних блоков; старые «v0.2.28 не готова» завершены. Локальная разработка `D:\Dev`, пакетные сценарии, GUI ~1 с, CLI без искусственных задержек, GitHub Actions запрещены; STOP — локально.
5. При обновлении капсулы сохранять тот же идентификатор и Core, проверять связанную восьмёрку хешей, увеличивать поколение seal строго после согласованной записи и сохранять Git history. Пользовательские/локальные неотслеживаемые файлы не чистить.

## 30.10.2026 — персонализированные чаты и локальный индикатор
- При использовании Windows MCP tools в этом project-manager-контексте передавать \`executor="project-manager"\` на каждом tool call (когда схема обновлена). Supervisor использует \`executor="supervisor"\`; обычный ChatGPT аргумент опускает.
- UI источник — ControlActivity.Executor; нормализация в ControlExecutor, не печатать произвольные аргументы или секреты. Для каждого вызова отдельная подпись, в desktop.batch вложенные шаги наследуют её и не могут переопределять; удалённый Router Lite также передаёт идентичность.

## 30.10.2026 — новый способ разработки и выпусков
1. На DOMKAT в `D:\Dev\ChatGPT-MCP-Tunnel` сначала собрать достаточный контекст, затем локальным PowerShell/Python сценарием в одном tool call провести серию правок, сборок, тестов и итог. Ошибка => fail-fast и возвращение модели. Не выполнять десятки микрокоманд.
2. GUI: ~1 сек между управляющими действиями (включая `desktop.batch`); чтение и CLI без искусственной паузы. STOP локальный, но долгий самостоятельный скрипт обязан иметь отдельное прерывание.
3. `scripts/Build-LocalRelease.ps1 -Version X.Y.Z` делает локальный .NET publish, selftests, Inno Setup full/update и SHA256 в `.tools/local-release/vX.Y.Z/dist`. GitHub Releases — прямой API upload, Actions не вызывать.
4. Перед выпуском сверять русские тексты на literal `????` и проводить интеграционную проверку реально запущенного окна/трея, а не только независимый selftest формы.
5. Для автоапдейта отдельно запускаем процесс, штатно освобождаем рабочие файлы, проверяем SHA256/версию/MCP после. В v0.2.27 установщик завершился, но внешний `Start-Process -Wait` висел из-за дочернего tray — ждать конкретный PID с таймаутом.
6. Для Context Capsule Windows CRLF: прямой Git hash-object --stdin даёт ложное расхождение; проверять `git hash-object --path=...` либо Git-объекты. Все восемь прежних хешей поколения 45 были валидны. Источник: проверенные операции 30.10.2026.

## Router Lite development test and serialization pitfall (28.10.2026)
- Use a separate `dotnet build -o .tools/router-lite-build` to avoid overwriting DLLs loaded by the live tunnel.
- Run `CHATGPT_MCP_TEST_DLL=...` with `tests/test_mcp_stdio.py` and `tests/test_router_lite.py`; the latter starts router + agent as independent local processes, checks discovery, call, unknown address and disconnect.
- A default/uninitialized `System.Text.Json.JsonElement` in a response packet fails on serialization, drops a WebSocket connection and makes the command appear to fail. Initialize optional `arguments` to a valid empty JSON object or otherwise omit undefined fields. This was reproduced and fixed on 28.10.2026.
- Source: verified local build logs and first failing then succeeding E2E test on DOMKAT.
- Authority: verified-runtime.

## Remote PNG and regression isolation (28.10.2026)
- MCP SDK `ImageContentBlock.Data` already stores base64-encoded UTF-8 bytes. Encoding its value into base64 again produces double-encoded PNG output; forward `Encoding.UTF8.GetString(image.Data.Span)` and reconstruct with `ImageContentBlock.FromBytes(Convert.FromBase64String(...), mime)`.
- Verify PNG by checking base64-decoded signature `89 50 4E 47 0D 0A 1A 0A`, not merely the `image` block presence.
- Existing tests require an isolated `CHATGPT_MCP_TEST_DLL` override to avoid overwriting the DLL of the running tunnel.
- Use disposable WPF/Tk/WinForms windows for remote mouse, keyboard and UI Automation regression. Never operate on working documents in these tests.
- source: verified runtime error and corrected E2E/GUI tests on DOMKAT, 28.10.2026.
- authority: verified-runtime.


## Agent installer build and acceptance procedure (28.10.2026)
- Build WinForms tray host with `dotnet publish ... -r win-x64 --self-contained true -p:PublishSingleFile=true`, Runtime under `publish/Runtime`; keep script `AutomationWorker.ps1` with runtime.
- Use `ChatGptMcpAgent.exe --selftest` for DPAPI/URL checks without modifying installed user settings.
- CI runners can lack interactive graphical sessions; use `CHATGPT_MCP_SKIP_GUI_TESTS=1` for router integration in Actions, while DOMKAT's interactive GUI regression remains authoritative for visual operations.
- Inno Setup does not exist on DOMKAT; GitHub Actions Windows must verify Setup/Update/uninstall actual binaries before release.
- source: observed .NET self-contained publish and test on DOMKAT 28.10.2026 and the owner-approved MailRu Desktop installer pattern.
- authority: verified-runtime, owner-directive.

## Windows CI WinExe and Inno updater process lifetime (28.10.2026)
- Graphical WinExe `ChatGptMcpAgent.exe --selftest` must be started with `Start-Process -Wait -PassThru`; calling `&` and checking `$LASTEXITCODE` is not reliable (could be empty).
- For Inno Setup processes which start persistent tray apps, use `Start-Process -PassThru` followed by the returned `Process.WaitForExit(timeout)` instead of PowerShell `Start-Process -Wait`, because PowerShell can wait for descendant long-running tray processes too.
- CI run 40689966362 verified that correction through first install, update and uninstall.
- source: GitHub Actions release acceptance 40689966362 and prior test failures 40687941223 / 40688327383 / 40689211248.
- authority: verified-runtime.

## Inno Setup role-aware path (28.10.2026)
- Full setup computes initial directory from role with code callback and shows native folder picker after role page.
- Interactively change suggested path only when it equals an unmodified default; preserve custom user input and prior installations.
- Store final InstallDir at ssPostInstall and have update use that registry path; keep same AppId.
- CI tests server D: default, client user-local C: default, custom client path and update preservation.
- source: Inno documentation and locally inspected installer source.
- authority: verified-repository.

## Release proof for folder selection
- GitHub Actions workflow run 40696034380 passed the compiled Inno Setup full/update installers.
- Server default D if present; client default user-local C; user-defined client directory persisted through update, deleted on uninstall.
- Release v0.2.0 includes official verified OpenAI client and one universal executable. Existing DOMKAT runner was not touched by CI.
- source: actions 40696034380, release v0.2.0.
- authority: verified-runtime.

## Reusing logical OpenAI Tunnel ID
- tunnel-client init creates only a local profile for an EXISTING tunnel; never call tunnel-client admin tunnels create/delete during this rollout.
- Windows PowerShell 5.1 does not support 1>&2; use 2>&1 pipeline and verify syntax via PowerShell Parser.
- Valid logical tunnel IDs use 32 lowercase alphanumeric characters, optionally preceded by a 4-character namespace. Retain existing ID but do not transfer old credentials.
- First stage is read-only preflight plus verified download and independent CMD; destructive transition starts only after local runner can finish without the old MCP channel.
- source: official CLI help and local PowerShell tests, 28.10.2026.
- authority: owner-directive, verified-runtime.

## Windows PowerShell 5.1 native stderr incident (28.10.2026)
- Native MCP stdout/stderr must NOT be merged with 2>&1 while ErrorActionPreference is Stop; informative StdioServerTransport log to stderr becomes NativeCommandError, terminating parent and child.
- Run the tunnel-client command directly with ErrorActionPreference temporarily Continue, restore previous preference in finally, inspect native LASTEXITCODE.
- Diagnose safely via original bridge_ping, system.info, GitHub auth, listening ports 55198 and 55201, healthz/readyz HTTP 200. Never log decrypted DPAPI keys.
- source: reproduced and repaired actual DOMKAT 28.10.2026.
- authority: verified-runtime.

## Safe actual in-place update proof (28.10.2026)
- Download official v0.2.2 update to local staging, confirm GitHub SHA256 against da46afd35df787f71ca0939722545796f95ad164b172c73d2a3a8418e87f038b.
- Backup working ServerStart.ps1 (no secrets), keep existing DPAPI and settings untouched.
- Update only this product by running --shutdown then Inno Update /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS, wait for installer process, verify installation and existing Tunnel ID/role.
- Confirm fixed native stderr launcher, healthz/readyz HTTP 200, 55201 listener, one tunnel-client, original plugin bridge_ping, system.info and 42 MCP registrations.
- source: actual successful DOMKAT v0.2.2 in-place update 28.10.2026, run 40712500651.
- authority: verified-runtime.

## WinForms tray GUI and icon (28.10.2026)
- Do not call blocking ShowDialog() during Application.Idle in Form-less ApplicationContext. It may leave a hidden modal Settings form and block later tray menu requests.
- Use Form.Show(), hold one reference, restore and Activate an existing form, and free on FormClosed. The --settings local EventWaitHandle triggers the running tray instance, not a second tunnel.
- Server health is readyz HTTP 200 plus Router Lite TCP 55201; Client stderr text must not override Server status.
- Embed Windows ICO through csproj ApplicationIcon and Inno Setup SetupIconFile, verify WinExe --test-icons and extracted native icon; use explicit green/amber/red tray-dot variants.
- source: DOMKAT invisible Settings HWND, original project source, Owner screenshot and win-x64 tests.
- authority: verified-runtime, owner-directive.

## WinForms native top-level window visibility (28.10.2026)
- A Form can exist and have a tray-menu callback complete while Windows HWND lacks WS_VISIBLE; managed Form.Show() alone was insufficient in the observed DOMKAT tray-only ApplicationContext.
- Inspect native HWND using EnumWindows / IsWindowVisible; a real ShowWindow(hwnd, SW_SHOW) made the hidden Server Settings window visible on DOMKAT. In production, check IsWindowVisible after Form.Show and apply ShowWindow(SW_SHOW) as needed.
- Validate by desktop.window.list after invoking the existing tray process --settings and verify no additional cloud OpenAI tunnel was created.
- source: actual DOMKAT Win32 experiment and v0.2.4 local tests 28.10.2026.
- authority: verified-runtime.

## Secret entry UX and owner-controlled update (28.10.2026)
- Keep stored API/client key values in DPAPI; primary Settings windows must never contain secret TextBoxes, only status and buttons opening a temporary SecretInputDialog.
- Client can submit a new secret but has no show, reveal or copy action; Server may copy stored client secret with explicit server-side button. Keep OpenAI Tunnel ID fixed once valid on server.
- Password entry dialog input supports typing and paste but does not expose copy/cut/reveal. If changes are staged, only persist keys after user saves primary settings.
- User requested only two installer binaries and retains right to run app's built-in updater themselves; do not remotely update their installed DOMKAT server. Keep IP 192.168.0.3.
- source: owner decision and v0.2.5 source/tests 28.10.2026.
- authority: owner-directive, verified-runtime.

## Owner-control of installed updates
- When user requests publication of full and update installers but explicitly says they will update with built-in program controls, only compile/publish/verify release assets; do not execute shutdown, Inno installer or any mutation of the installed DOMKAT software.
- v0.2.5 Windows Actions 40732281198 verified both installer assets, update preservation, full installation and uninstall. Legacy alias ChatGPT_MCP_Agent_Update_v0.2.5.exe is retained for existing auto-updater.
- source: owner directive and successful GitHub release on 28.10.2026.
- authority: owner-directive, verified-repository.

## Owner restriction on installed upgrades (2026-10-28)
- source: user explicitly controls update installation; remote installed DOMKAT must not be changed by project manager.
- authority: owner-directive
- action: publish verified full/update binaries only; never run remote installer or shutdown running server without specific new authorization.

## Windows update stream lifecycle
- Never rename downloaded .part while FileStream remains open; use bounded await using scope around all writes, then File.Move only after DisposeAsync. Validate actual bytes against ContentLength before replacement, preserve previous download on failure.
- --test-updater-download exercises overwrite, interrupted download cleanup, and exclusive file access. v0.2.5 existing built-in updater needs one-time manual v0.2.6 Update EXE, user explicitly disallows remote update.
- source: owner incident and runtime log 28.10.2026.
- authority: owner-directive, verified-runtime.

## LAN-only client access security and diagnosis (28.10.2026)
- When server Router Lite listens on LAN but client cannot connect, check Windows Firewall inbound profile, permitted rule for TCP 55201, latest server DPAPI key timestamp, and client WebSocket error, separating HTTP 401 auth, 409 duplicate name and network failures. A readyz 200 on localhost does NOT prove external LAN reachability.
- Permit firewall only by explicit server Settings button and Windows UAC approval; bound to Router Lite runtime exe, local address, TCP 55201 and current LAN subnet. Never open port to entire Internet nor change global firewall profile.
- source: DOMKAT read-only network/Windows Firewall diagnostics and source tests, 28.10.2026.
- authority: verified-runtime, owner-incident.

## v0.2.6 LAN client support after stable release (28.10.2026)
- Full/update installers published, CI 40736310056 success. Owner updates DOMKAT themselves; never remotely install.
- When user tests MKATUSHONOK: server should allow LAN-only firewall rule via explicit UAC, copy most recent server-side DPAPI key, client enters it and saves. For failures classify network, 401 key, 409 duplicate name. No independent physical-client test has been done yet; maintain distinction between plausible firewall diagnosis and proven end-to-end connection.
- source: Windows CI/release and DOMKAT read-only logs 28.10.2026.
- authority: verified-repository, verified-runtime, owner-directive.

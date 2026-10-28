# Procedural memory

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

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

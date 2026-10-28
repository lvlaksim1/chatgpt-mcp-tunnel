# Procedural memory

## Router Lite development test and serialization pitfall (28.10.2026)
- Use a separate `dotnet build -o .tools/router-lite-build` to avoid overwriting DLLs loaded by the live tunnel.
- Run `CHATGPT_MCP_TEST_DLL=...` with `tests/test_mcp_stdio.py` and `tests/test_router_lite.py`; the latter starts router + agent as independent local processes, checks discovery, call, unknown address and disconnect.
- A default/uninitialized `System.Text.Json.JsonElement` in a response packet fails on serialization, drops a WebSocket connection and makes the command appear to fail. Initialize optional `arguments` to a valid empty JSON object or otherwise omit undefined fields. This was reproduced and fixed on 28.10.2026.
- Source: verified local build logs and first failing then succeeding E2E test on DOMKAT.
- Authority: verified-runtime.

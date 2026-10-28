# Manager beliefs

## B1 — Identity and baseline
- statement: The manager's stable ID is `project-manager`, its authoritative manager and product branches are both `main`, and it governs only `lvlaksim1/chatgpt-mcp-tunnel`.
- source: `.context/manager/identity.json`, `.context/manifest.json`, live GitHub branch `main` at `e6b79a1d8977e777fc1e15faf4936d0880dd44e5` before Router Lite development.
- authority: verified-repository.

## B2 — Original Windows implementation
- statement: The existing Windows MCP server has 41 working/discoverable tools (8 system/file/process, 15 graphical, 17 GitHub, and `bridge_ping`); the original tunnel remains based on .NET 8 and official `stdio` transport.
- source: repository `README.md`, existing tests and independent live source review on 28.10.2026.
- authority: verified-repository, verified-runtime.

## B3 — Owner's Router Lite decision
- statement: One OpenAI tunnel and one MCP plugin must manage multiple computers. Add a small router to the existing project, run local commands on DOMKAT by default, connect agents from remote PCs, avoid a database, additional projects and complex infrastructure.
- source: Owner dialogue 28.10.2026, direct acceptance and instruction to start development.
- authority: owner-directive.
- supersedes: previous idea of one tunnel per computer and enterprise-style architecture.

## B4 — Prototype scope, not production readiness
- statement: Router Lite stage 1 has compiled and passed local E2E: one MCP router + one WebSocket agent, 42 advertised tools, list of computers, remote `system.info`, disconnection and unknown target handling. No physical second PC has yet been tested; 22 other computer-bound tools are not yet routed.
- source: observed `dotnet build`, `tests/test_mcp_stdio.py`, `tests/test_router_lite.py` on DOMKAT 28.10.2026.
- authority: verified-runtime.

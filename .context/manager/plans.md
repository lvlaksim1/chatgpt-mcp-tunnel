# Manager plans

## Router Lite — active implementation plan
1. Preserve 41 existing tools and live tunnel; `computers.list` is tool 42. All 23 Windows tools have `computer` parameter. [complete, locally verified]
2. One executable with router/agent mode, outgoing WebSocket agents and request IDs without database. [complete, locally verified]
3. Extend routing to the remaining 22 Windows tools while preserving MCP text/image blocks and Windows interactive-session behavior. [complete, local E2E verified]
4. Connect two distinct physical PCs through a protected network; on DOMKAT reconnect, wrong key, duplicate ID, unknown target and GUI actions already passed. [next]
5. Add safe configuration/startup of agents and protected local secret storage; avoid new plugins or repositories.
6. After verified release of a version, update context and record the exact tests and commit.

Implementation references reviewed: MeshCentral agent networking, Supergateway process/JSON-RPC forwarding, Docker MCP Gateway collision and security rules. They are design references, not dependencies.

Source: accepted Owner scope, inspected repository code and verified eight test suites on DOMKAT 28.10.2026.

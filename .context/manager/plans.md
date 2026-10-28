# Manager plans

## Router Lite — active implementation plan
1. Preserve 41 existing tools and keep the current production tunnel unchanged during testing; stage 1 adds only `computers.list` plus an optional `computer` on `system.info`.
2. Build one binary with `--agent` mode, a tiny WebSocket hub and common request IDs. Use outgoing connections to the main host; no new database. [stage 1 complete, local E2E verified]
3. Extend the routing to the remaining 22 computer-bound tools while preserving MCP text/image content and Windows interactive-session behavior. [next]
4. Verify results on two physical Windows machines, including reconnect, bad key, conflict of names and unknown target; do not expose unsecured `ws://` on public networks.
5. Add safe configuration/startup of agents and protected local secret storage; avoid new plugins or repositories.
6. After verified release of a version, update context and record the exact tests and commit.

Implementation references reviewed: MeshCentral agent networking, Supergateway process/JSON-RPC forwarding, Docker MCP Gateway collision and security rules. They are design references, not dependencies.

Source: accepted Owner scope, inspected GitHub source, observed stage 1 tests (28.10.2026).

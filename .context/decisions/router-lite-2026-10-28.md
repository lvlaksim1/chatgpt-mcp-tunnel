# Decision — Minimal Router Lite for several Windows computers

- Date: 28.10.2026 (MSK)
- Status: approved by Owner, implementation active
- Scope: `lvlaksim1/chatgpt-mcp-tunnel`
- Authority: direct Owner conversation on 28.10.2026.

## Decision
Provide **one OpenAI MCP Tunnel and one ChatGPT plugin**, not one tunnel per computer. Embed a lightweight command router in the existing C#/.NET 8 project, with the main node initially on DOMKAT and small Windows agents on the other computers. Preserve the existing Windows commands, their local defaults, and the GitHub connector as a global module on the main host.

One binary runs in router/default mode or agent/`--agent` mode. Agents initiate WebSocket connections to the router; public exposure requires TLS or an encrypted private network (Tailscale is optional). Minimal JSON messages identify requests, tools and results; match replies by request ID. Use `computer` as an optional parameter in computer-bound tools, and publish one `computers.list` tool. No database, dedicated cloud component or additional product repository.

## Rejected alternatives
- One independent OpenAI tunnel/plugin on every computer: Owner explicitly rejected it.
- Enterprise server/agent framework, broker, SQL registry, complex queue and mandatory remote-management platform: Owner judged it disproportionate.
- Installing MeshCentral/ContextForge/Supergateway wholesale: unnecessary dependencies; only architectural lessons are relevant.
- Harpoon is not required for multi-computer command routing.

## Phasing and evidence
Stage 1 (28.10.2026) has passed local two-process build/E2E with 42 tools, remote `system.info`, connection and disconnection. This does **not** prove use on separate physical PCs; other remote Windows tools and images remain open work.

## Design precedents reviewed
- MeshCentral/MeshAgent: client-initiated connections, stable identities, verification on connection.
- Supergateway: MCP stdio bridges, JSON-RPC correlation, lifecycle of child processes and cancellations.
- Docker MCP Gateway/IBM ContextForge: tool-name collisions and routing checks; keep a shared tool namespace instead of duplicating tools per computer.

Source: Owner conversation, public GitHub source inspected and local code/tests. No dependency on these third-party projects is introduced.

# Project constraints

- Repository is public; API keys, DPAPI ciphertext, `.tools`, process output and binary distributions must remain untracked.
- Use factory repository storage policy: max 5 MiB per tracked file; no tracked executable, installer, archive, DLL or PDB.
- Windows paths passed through tunnel-client's command parser must use forward slashes in `mcp-command`.
- `bridge_ping` is safe and read-only; file/process tools require separately designed permission policy and audit.
- Keep tunnel process running for ChatGPT connector discovery and calls; avoid two clients connected to the same tunnel simultaneously.
- Maintain working environment on drive D:; no dependence on a browser message injection scheme, task scheduler or another app's ToolRouter.
- Automated CI must not require user API keys.


## Mandatory owner-only access boundary (owner directive, 28.10.2026)

**Threat model:** Treat the ChatGPT custom plugin, its URL, and tunnel ID as potentially discoverable or accidentally shared. An unrelated ChatGPT user must never be able to read files, inspect private computer data, or execute Windows commands on the owner's computer merely by installing or invoking the same plugin. The project serves only the owner; third-party deployment and sharing are out of scope.

**Release gate for all tools beyond non-sensitive `bridge_ping`:**
1. **Fail closed.** Each tool that reveals non-public machine state, reads files, launches programs, or writes data must perform server-side authorization; unverified calls never execute.
2. Do not treat plugin visibility, the tunnel's runtime API key, knowledge of `tunnel_id`, or model instructions as end-user identity or authorization.
3. Current stdio MCP transport does not provide a proven authenticated caller identity to our application. Until a verified owner-authentication mechanism is implemented and end-to-end tested, all sensitive tools must be unavailable. Local machine-specific, per-operation confirmation is an additional protective option; blanket process-wide approval is insufficient to distinguish concurrent callers.
4. Prefer switching to a local HTTP MCP transport with standards-based authenticated requests where the server can validate the requesting owner's identity. Verify authorization forwarding through OpenAI Secure MCP Tunnel and reject missing, expired, malformed, or other-user credentials. Do not claim identity is established without a real negative cross-identity test.
5. For execution and file modification, require a second independent action-scoped authorization boundary (e.g., explicit local confirmation of exact operation and arguments), user-scoped unprivileged process privileges, bounded paths and command policy, structured audit logs, emergency stop and automatic deny when the approval service is unavailable.
6. Acceptance tests must demonstrate: owner accepted; anonymous denied; unrelated valid user denied; missing and replayed credentials denied; a denied request produced zero local side effects; failed local confirmation denied; ability to revoke access without reinstalling.
7. Before these gates pass, keep only the harmless connectivity diagnostic exposed. Preserve the rule across all future feature development.

Evidence: user directive in ChatGPT conversation 28.10.2026. This is a security release criterion, not a claim that it has already been implemented.

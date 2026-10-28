# Project constraints

- Repository is public; API keys, DPAPI ciphertext, `.tools`, process output and binary distributions must remain untracked.
- Use factory repository storage policy: max 5 MiB per tracked file; no tracked executable, installer, archive, DLL or PDB.
- Windows paths passed through tunnel-client's command parser must use forward slashes in `mcp-command`.
- As of 28.10.2026 only `bridge_ping` is implemented; no additional authorization or approval framework is commissioned by the owner.
- Keep tunnel process running for ChatGPT connector discovery and calls; avoid two clients connected to the same tunnel simultaneously.
- Maintain working environment on drive D:; no dependence on a browser message injection scheme, task scheduler or another app's ToolRouter.
- Automated CI must not require user API keys.

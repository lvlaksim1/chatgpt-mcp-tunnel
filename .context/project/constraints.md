# Project constraints

- Repository is public; API keys, DPAPI ciphertext, .tools, process output and binary distributions must remain untracked.
- Use factory repository storage policy: max 5 MiB per tracked file; no tracked executable, installer, archive, DLL or PDB.
- Windows paths passed through tunnel-client command parser must use forward slashes in mcp-command.
- On 28 October 2026 the source exports 41 tools (24 Windows and 17 GitHub); maintain compatibility with the earlier Windows tool names and independently refresh the tool catalog in ChatGPT.
- No additional owner-authentication or approval framework is commissioned by the owner at this stage.
- Keep one active tunnel client attached to a tunnel; avoid simultaneous clients for the same tunnel.
- Maintain the working environment on drive D:. Do not depend on browser message injection or another application's tool router.
- Automated CI must not require user API keys or an interactive desktop.

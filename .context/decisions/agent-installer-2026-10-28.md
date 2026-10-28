# Decision — Windows agent installer following MailRu Desktop

- Date: 28.10.2026 MSK
- Status: Owner approved and implementation in progress
- Scope: `lvlaksim1/chatgpt-mcp-tunnel`
- Authority: Owner's explicit approval: agent installation/update must follow the MailRu Desktop architecture.

## Scope

One OpenAI MCP Tunnel is retained on DOMKAT. The additional Windows computers run a per-user tray agent that manages the existing Router Lite `ChatGptMcpTunnel.exe --agent` executable. This packaging does not add another tunnel, database or project repository.

Replicate the MailRu Desktop setup conventions using Inno Setup 6: separate full `ChatGPT_MCP_Agent_Setup_vX.Y.Z.exe` and update `ChatGPT_MCP_Agent_Update_vX.Y.Z.exe`; install without elevation under LocalAppData/Programs; mutable state only under LocalAppData; updates preserve settings and shortcuts, uninstall removes all application-owned files/settings/autostart. Self-contained .NET 8 Windows x64; run in interactive user session for GUI/UIA.

New tray host: compact Russian settings dialog, computer name/Router URL/agent key/autostart; DPAPI-protected credentials, tray status/menu/reconnect; GitHub Releases update prompt with a separate updater copy that downloads the official update asset, signals parent via local named pipe, waits for exit and starts Inno Setup silently. Publish only binary release assets (no workflow artifacts), keep code-only repository and manager capsule.

## Evidence

Reviewed MailRu Desktop `installer/full.iss`, `installer/update.iss`, `release-installer.yml` and `MailRuDesktop.UpdateAgent/Program.cs`. On DOMKAT, local self-contained WinForms and MCP runtime builds succeeded, `--selftest` returned 0, and MCP 42-tool/Router Lite/reconnect smoke tests passed on the published runtime. Inno Setup was not installed locally and must be verified in Windows GitHub Actions. No second physical PC has been tested yet.

## Next evidence gate

GitHub Actions must build both installer EXEs, run silent first install/update/uninstall verification, then publish stable numeric v0.1.0. Test the agent on a second physical computer before declaring multi-PC production acceptance.

- source: Owner directive 28.10.2026, inspected GitHub source, observed DOMKAT .NET builds and tests.
- authority: owner-directive, verified-repository, verified-runtime.

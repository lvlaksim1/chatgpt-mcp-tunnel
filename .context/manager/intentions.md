# Manager intentions and commitments

## Router Lite implementation
- commitment_id: `router-lite-2026-10-28`
- status: accepted/active
- owner: Project Manager `project-manager`
- source: Owner 28.10.2026: «восстанови менеджера проекта chatgpt-mcp-tunnel. передай ему всю эту информацию и пусть приступает к разработке».
- authority: owner-directive
- outcome: one MCP tunnel with explicit computer selection and remote execution of the existing Windows tools.
verified partial progress: stages 1-3 passed local E2E, all 23 Windows tools remote-capable, PNG preserved, parallel calls, bad keys, duplicate IDs, reconnect, old Windows/GUI/UIA regressions passed.
- remaining: cross-machine verification on second physical PC, protected agent provisioning/autostart, operational acceptance and release validation.
- completion gate: functional second physical PC and regression of all 41 original tools; explicit verification, no completion claim from local-only test.

## Maintain independent core plugin
- commitment_id: `independent-mcp-tunnel`
- status: accepted/active
- outcome: preserve the independent project, launcher, GitHub tools and existing Windows capabilities during incremental changes.
- source: prior Owner decisions recorded in `.context/project/*` and repository README.
- authority: owner-directive.


## Agent installer and updater
- commitment_id: `agent-installer-2026-10-28`
- status: accepted/active
- owner: Project Manager `project-manager`
- source: Owner explicitly accepted the MailRu Desktop installer architecture for Router Lite and ordered implementation on 28.10.2026.
- authority: owner-directive
- outcome: a self-contained x64 per-user Windows agent with two Inno Setup installers, DPAPI credentials, tray interface, user-session autostart and GitHub Releases updates.
- verified partial progress: application and runtime built self-contained on DOMKAT; `--selftest` and 42-tool/router/reconnect smoke tests passed; installer source/workflow are written.
- remaining: Inno Setup CI install/update/uninstall acceptance and binary GitHub Release; second physical PC connection.
- completion gate: valid downloadable Setup and Update installer assets, CI acceptance, then physical network operation validated separately.

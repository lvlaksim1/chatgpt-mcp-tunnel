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

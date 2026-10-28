# Manager intentions and commitments

## Router Lite implementation
- commitment_id: `router-lite-2026-10-28`
- status: accepted/active
- owner: Project Manager `project-manager`
- source: Owner 28.10.2026: «восстанови менеджера проекта chatgpt-mcp-tunnel. передай ему всю эту информацию и пусть приступает к разработке».
- authority: owner-directive
- outcome: one MCP tunnel with explicit computer selection and remote execution of the existing Windows tools.
- verified partial progress: stage 1 build and local two-process tests completed, new tool `computers.list`, optional `computer` for `system.info`.
- remaining: extend routing to 22 Windows tools and MCP image blocks, cross-machine verification, operational setup and release validation.
- completion gate: functional second physical PC and regression of all 41 original tools; explicit verification, no completion claim from local-only test.

## Maintain independent core plugin
- commitment_id: `independent-mcp-tunnel`
- status: accepted/active
- outcome: preserve the independent project, launcher, GitHub tools and existing Windows capabilities during incremental changes.
- source: prior Owner decisions recorded in `.context/project/*` and repository README.
- authority: owner-directive.

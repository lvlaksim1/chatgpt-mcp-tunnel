# Latest handoff

This is a compact working view; sealed manager BDI state takes precedence.

## Completed and verified (28.10.2026)

- Project Manager `project-manager` reinstated with stable v2.1.0 and previously published Router Lite decision.
- Router Lite stages 1–3 implemented; all 23 Windows MCP tools now have optional `computer`, while 17 GitHub tools stay global.
- One agent/one router mode in same .NET 8 binary; 42 MCP tools total. Preserved screenshot PNG blocks and original Windows behavior.
- Local DOMKAT tests passed: MCP stdio, two-process remote files/shell/processes/GUI/UIA/PNG, 23 target schemas, parallel requests, bad key, duplicate ID, unknown name, disconnect and reconnect after router restart.
- Passed original Windows, Tk GUI, WPF UIA, WinForms UIA and real-application read-only regressions using an isolated build. Live tunnel unchanged.

## Open commitment

Demonstrate and deploy a real second physical computer over protected network; add simple agent startup/credential handling, then release after full acceptance.

## Next operation

Physical PC onboarding and cross-host tests; do not claim production-ready multi-PC operation based only on DOMKAT tests. See `docs/router-lite.md`.

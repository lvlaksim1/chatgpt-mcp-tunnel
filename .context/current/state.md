# Current state

- Date: 28.10.2026 MSK; Project Manager ID `project-manager`; stable PM v2.1.0; `main` owns both manager state and product baseline.
- Live DOMKAT production tunnel: existing 41-tool installation; not restarted or replaced during Router Lite tests.
- Router Lite development stage 1: one new `RouterLite.cs`, two executable modes, one new `computers.list` tool (42 total), optional routing parameter `computer` for `system.info`.
- Verified: separate .NET 8 Release build with 0 errors and 0 warnings; stdio discovery of 42 tools; local two-process WebSocket connection, remote `system.info`, unknown target rejection, disconnect handling.
- The successful test is local to DOMKAT. Remote physical PC, 22 other Windows tools, authentication hardening and installation remain to be implemented.
- Documentation: `docs/router-lite.md` details configuration and limits. The two existing untracked local files with Russian names were untouched.
- Source authority: Owner directive plus live source/build/test evidence (28.10.2026).

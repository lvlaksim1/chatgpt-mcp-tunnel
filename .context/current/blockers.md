# Current blockers and open risks

1. No second physical PC tested: cross-network operation, real remote Windows sessions and deployment remain **not proven**.
2. Router Lite is not activated in the live DOMKAT tunnel; isolated test build only. Deployment and safe agent configuration/autostart remain open.
3. Agent authentication currently uses one shared secret supplied through environment variables. Next phase should support protected per-agent credentials and straightforward onboarding without added database/infrastructure.
4. 16 MiB wire message limit exists. Oversized responses fail explicitly; high-resolution screenshot size and network latency on a second PC need empirical validation.
5. Remote GUI actions require a user session on that PC. Server or locked-session behavior has not been tested.

The local routing, controls and reconnection tests passed; these risks do not invalidate that narrower evidence.

6. Agent installer Setup/Update v0.1.0 was compiled/tested/published on Windows GitHub Actions; next blocker is real deployment on second physical Windows PC.
7. Protected per-user DPAPI secret is functional for the client; Router Lite still requires the same manually configured shared secret server-side. A second physical PC has not been onboarded.

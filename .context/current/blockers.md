# Current blockers and open risks

1. No second physical computer has been connected or verified: cross-host operation remains unproven.
2. Stage 1 remote dispatcher supports only `system.info`; other 22 Windows-specific tools still run locally even when this prototype lacks the `computer` parameter.
3. MCP image-block preservation and interactive Windows session effects must be designed and tested before GUI routing.
4. Initial agent secret is a manually supplied shared environment variable. For later use on multiple PCs, require protected local storage and improved per-agent identification.
5. Current production tunnel has not been restarted onto this branch's new build, intentionally.
6. Router Lite requires reachable protected network path; unencrypted `ws://` must not be publicly exposed.

None of these facts is evidence that the local prototype failed. Evidence: tests and source inspection 28.10.2026.

# Next actions

1. Prepare and test installation of Router Lite agent on a **second physical Windows computer**, using a protected private network (or WSS/TLS). Do not expose plain WebSocket with secret to the internet.
2. Verify physical PC identification, 23 tools, remote screenshots, UIA, machine-specific paths, reconnect and errors; run full regression on DOMKAT again before live replacement.
3. Add simple agent provisioning, protected local secret storage and Windows sign-in autostart, while retaining one binary/repository/plugin and avoiding a database.
4. Update launcher for explicitly configured Router Lite; release a stable numerical version only after acceptance, update manager capsule and document real cross-host evidence.

Stages 1–3 were completed and locally verified on DOMKAT; do not repeat that work without a regression or new finding. Source: Owner task and verified tests (28.10.2026).

5. Publish agent installer source in `main`, trigger Windows GitHub Actions release workflow for numeric v0.1.0, inspect output/repair failures until Setup and Update EXE are published. Verify installation, update preservation and uninstall.
6. After binary release, configure DOMKAT Router Lite and connect a second Windows PC via a protected network; perform physical multi-PC acceptance.

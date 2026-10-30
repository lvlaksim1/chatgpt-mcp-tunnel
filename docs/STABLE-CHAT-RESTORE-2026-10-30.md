# Stable Chat Restoration — 30 October 2026

## Baseline and decision
The accepted ordinary ChatGPT relay is the v0.2.18 ChatRelayApp.html (Git revision eb1845f). The file remains unchanged in the restored candidate; its normalized Git blob hash is 4bcc1460b1ecf2aaf485d5978a045d5af048fdcc.

The additional automatic Hub/Bootstrap logic introduced later interfered with chat delivery and recovery. DOMKAT v0.2.29 delivered one ordinary ChatGPT response when bootstrap was manually enabled, but a subsequent local message remained pending and unforwarded. An older iframe also showed cloudflare_challenge. A live host heartbeat is not evidence of permission to dispatch.

## Stable operation in local v0.2.30 candidate
- The primary sending component is chat.ui.open, invoked in an already existing ordinary ChatGPT conversation. The established v0.2.18 relay requires pressing its Start button once; the component must remain active.
- ChatBootstrapApp.html is now an observer only and never polls or acknowledges queued messages, even after explicit click or host context update. Cached v0224/v0225 bootstrap resource pointers remain available, returning the same safe HTML as v0229.
- The Windows ChatHostActivator remains in source for future research, but is not driven from the tray polling loop, so it cannot autonomously open ChatGPT or compete with the manual relay.
- Existing user messages, sessions, attachments, and forwarded markers are left untouched. No Work/Codex/API fallback.
- The pending old message was not automatically replayed during implementation.

## Tests and packaging
Both .NET Release builds completed with 0 errors and 0 warnings. Ten isolated Python/Node suites passed, including the exact historical relay comparison, MCP resource compatibility, manual ui/message, multichat routing, and absent Bootstrap dispatch. The local packaged release builder ran Windows agent self-tests, MCP tests, and compiled both Inno full/update installers successfully. All SHA-256 manifest entries for local dist v0.2.30 match. No GitHub Actions.

## Acceptance limits
No live user ChatGPT E2E was run against v0.2.30, because the existing installation was intentionally kept at v0.2.29. This is a tested local release candidate, not a production-accepted build. After owner-directed installation, verify: chat.ui.open in a visibly ordinary ChatGPT thread, press Start, send one controlled local message, receive chat.reply, send a second message in the same local chat, then validate a second local chat and switch back. Diagnose any host challenge in place; do not bypass it.

The automatic no-click launch is deferred to a separate phase, after the stable manual relay passes live acceptance. No update, upload, push or release was performed as part of the restoration.

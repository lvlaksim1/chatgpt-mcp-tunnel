# ChatGPT MCP Tunnel — browser chat client v0.2.46 (30.10.2026)

## Design
- Runs standalone Node 24 BrowserChatBridge.js under ChatGptMcpAgent on all agent roles.
- Dedicated Yandex/Chromium profile under LocalAppData/ChatGPTMCPAgent/Chat/dedicated-browser-profile; browser CDP endpoint loopback only.
- Existing ChatWindow and ChatSessionStore persist messages, attachments and sessions.
- New session: only CreateCloudThread=true may create a fresh ChatGPT /c/UUID URL, bound durably to the local chat.
- Existing session: canonical cloud URL is imported into session and browser-threads binding, continued in the same cloud conversation.
- Catalog periodically reads authenticated account sidebar; import by pasted URL is always available.
- Imported cloud history is displayed from read-only browser-history cache. New local messages remain in the existing queue.
- Browser snapshot monitors assistant text and completion, storing partials and final reply in the original local record.
- Atomic browser-claims keep uncertain operations from automatic resending.
- activated.json is the durable switch disabling legacy MCP Apps queue acquisition even while browser login or recovery is pending.
- Initial activation cutoff excludes all pre-existing queued messages from new automated sends.
- ChatGPT MCP tools and Router Lite remain independent. No OpenAI API key, Work, Codex or scheduler for chat.
- Bundles a local Node executable and its license in Inno Setup installers.

## Verified
- Both .NET Release projects compile.
- Node tests: queue claims, routing, activation cutoff, attachment-only eligibility, URL import and catalog normalization, worker heartbeat.
- WinForms selftests: sessions, chat windows, attachments and imported URL idempotency.
- Full local release suite validated Windows MCP, UIA and chat bridge suites. Inno setup/update packaging.
- Real CDP probe connected to dedicated Yandex profile, created a ChatGPT tab. Page reported ready=false, needsLogin=true, challenge=false.

## Not accepted / outstanding
1. User must complete interactive login in the dedicated browser profile; never read or copy cookies/passwords.
2. Perform genuine ChatGPT 2-in-one-thread, 10-in-one-thread, two-thread and restart E2E.
3. Confirm attachments upload and finish on real ChatGPT. DOM.setFileInputFiles path implemented but not live accepted.
4. Verify imported cloud transcript completeness and sidebar catalog in authenticated profile.
5. Test on MKATUSHONOK after installation and its own one-time login.
6. Fail closed on login challenge and disconnected VPN. Never silently fall back to another conversation or resend uncertain messages.

## Deployment
Source branch feature/browser-chat-v0243, workspace D:\Dev\ChatGPT-MCP-Tunnel.
Build with scripts/Build-LocalRelease.ps1 -Version 0.2.46. Installers in .tools/local-release/v0.2.46/dist.
Keep live DOMKAT stable v0.2.37 until real cloud E2E is accepted.

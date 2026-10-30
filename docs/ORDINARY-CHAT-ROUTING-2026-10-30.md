# ChatGPT MCP Tunnel: ordinary-chat routing acceptance, 30.10.2026

## Goal and restrictions
The local Windows chat must create **ordinary ChatGPT threads**, not Work or Codex. No separate OpenAI API. No GitHub Actions. Compile/test exclusively on DOMKAT in `D:\Dev\ChatGPT-MCP-Tunnel`.

## Verified evidence
- The older v0.2.18 `ChatRelayApp.html` used `ui/message` from an iframe in a normal ChatGPT conversation and `target:"new"` for other local chats.
- The Global MCP entrypoint could run in a different host context and must **never** originate `ui/message`. A text prompt saying “do not use Work” cannot control the host-selected execution mode.
- A real diagnostic invocation of `chat.thread.open` in the current ChatGPT conversation (30.10.2026) reported `hasOpenAiMessage=true`, `hasDeepLink=false`, `displayMode=inline`. An earlier bootstrap diagnostic on 29.10 reported `hasOpenAiMessage=false`, `hasDeepLink=false`. These are live observations, not guarantees of future host capability.
- Four earlier messages were pending in the Windows local queue when this investigation began. The investigation did not send or delete them.

## v0.2.29 routing implementation
1. `chat.hub.open` (Global entrypoint) exposes a **diagnostic-only** HTML resource with *no* `ui/message` code, no pending-queue calls and no hidden dispatch branch.
2. `chat.bootstrap.open` remains a **non-Global** tool, invoked from an **existing ordinary ChatGPT conversation**. Its independent resource URI `chat-bootstrap-v0229` avoids stale cached v0.2.25 HTML.
3. Only dispatch if `hostCapabilities.message.text` and the `experimental["openai/message"]` extension are advertised. An explicit ordinary `https://chatgpt.com/c/<UUID>` host deep link authorizes transport. An explicit Global, Work, non-ChatGPT, malformed or other URL blocks transport.
4. If the host **does not expose a deep link**, but advertises the extension, the iframe offers a button **«Подтверждаю: это обычный чат ChatGPT. Включить пересылку»**. The user must verify the current ChatGPT mode and click this button once per widget lifetime. This enables automatic dispatch afterward, but the button alone cannot cryptographically prove host mode.
5. A host-context change immediately revokes earlier user confirmation and stops queue polling. An in-flight operation re-checks eligibility before any `ui/message` send. Pending messages are acknowledged only after host acceptance, so unconfirmed/unsupported contexts do **not** consume the local queue.
6. The bootstrap logs lifecycle as `mode=chat`; the Global observer logs as `mode=global`. Windows anchor activator recognizes bootstrap heartbeats while keeping `/mcp-app` URLs forbidden as anchors.

## Acceptance
- Local: passing Node VM tests for accepted ordinary route, no extension, absent route, explicit Global/Work route, hostile origin, revocation and no queue loss; Python MCP resource and Windows/Router Lite tests.
- **Still required after installing v0.2.29:** launch `chat.bootstrap.open` directly from a visually verified ordinary `/c` ChatGPT conversation; confirm the user button when deep link is absent; send a single controlled local-chat test message; visually verify that the newly created ChatGPT thread is ordinary Chat, with response returning through `chat.reply`. An accepted `ui/message` result alone does not prove that the created thread is not Work.
- If the real host rejects or changes `target:"new"`, keep the queue and obtain diagnostics; do not silently revert to Global or claim the problem solved.

**Source:** direct Owner instruction 30.10.2026, source history v0.2.18 through v0.2.29 and local/real-host diagnostics. This is a partial code-level repair pending live E2E acceptance.

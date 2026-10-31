# Free VLESS/REALITY automatic refresh — DOMKAT, 31 October 2026

## Status: installed locally and tested

Owner asked to automatically update and maintain free VLESS/REALITY servers for the existing ChatGPT MCP Tunnel ecosystem. Implementation lives under `scripts/Vpn` of the existing repository, not another repo.

- **Task Scheduler**: `ChatGPTMCP-FreeReality-Refresh`
- **Frequency**: every 3 hours, with a repeating daily trigger (permanent); starts when possible after a missed run.
- **Task account**: currently logged-on Windows user `DOMKAT\Maksim`, interactive token, no administrative privilege or saved Windows password.
- **Entry**: Python 3.13 `pythonw.exe` launches `scripts/Vpn/free_reality_updater.py` with `--max-probes 16 --timeout 9 --port 11836`.
- **Sources**: `scripts/Vpn/free_reality_sources.json`, containing AetrisVPN-lite and two 0xRadikal lists, fetched only via approved HTTPS `raw.githubusercontent.com`; maximum 3 MB/source.
- **Filters**: only VLESS/TCP or RAW/REALITY with correctly formed UUID, REALITY public key, SNI, optional XTLS-Vision, no localhost or nonpublic IPs. Deduplicate by hash, interleave multiple providers, prioritize last validated server.
- **Validation**: private local ephemeral Xray-core v26.10.29 on `127.0.0.1:11836`, actual HTTPS `example.com` HTTP 200 through `curl --proxy`. Stop and clean up each child, never touch Happ or the current MCP PID.
- **Selection**: keep previously validated server when still alive; otherwise choose a working candidate, with two passing nodes sufficient for a regular refresh cycle. If sources fail or no new working node is found, preserve the last selected encrypted profile (marked as last-known, **not** as currently alive).
- **Data**: user-local `%LOCALAPPDATA%\ChatGPTMCPAgent\Vpn\profiles\`.
  - `status.json`: last refresh, feed health, test counts, selected public endpoint, no UUID or REALITY password.
  - `catalog.json`: public safe metadata for deduplicated candidates, no raw vless URIs.
  - `Free-Reality-Auto.dpapi`: current-user Windows DPAPI-encrypted valid Xray config; compatible with the existing Start-ProtectedXrayProfile.ps1 launcher.
  - `Free-Reality-Auto.meta.json`: nonsecret profile metadata.
- **Concurrency**: exclusive `refresh.lock` with a stale timeout; Task Scheduler ignores overlapping invocations.
- **Scope**: updates the catalog and offline profile only. **No automatic rerouting of authenticated ChatGPT, WebView2, or MCP to an unknown public server**, no Windows system proxy/routing edits, no extra MCP tunnel. Enabling a particular free exit for logged-in ChatGPT needs a separate trust decision and live transport acceptance.

## Verified tests

- Python `tests/test_free_reality_updater.py`: 6/6 PASS (feed parsing, base64, private address protection, Xray JSON, deduplication, priority selection, DPAPI encryption, and provider outage preserving last-good state).
- Initial actual refresh 2026-10-31 ~16:26 MSK: **512 deduplicated VLESS/TCP/REALITY candidates**, **11 probed**, **2 reachable**. Selected `147.45.245.177:443` from AetrisVPN-lite, validated via Xray.
- Windows Task Scheduler was also invoked immediately after registration and returned **LastTaskResult 0**. Second cycle checked 2 nodes, both healthy, retained the working current selection. Next automatic run: **2026-10-31 19:28:28 MSK**.
- Windows PowerShell `Security.Cryptography.ProtectedData.Unprotect(...CurrentUser)` successfully decrypted updater-created DPAPI data. Confirmed `listen=127.0.0.1`, `port=11821`, `protocol=vless`, `security=reality`, and REALITY password presence, without printing credentials.
- The scheduler-selected **Free-Reality-Auto.dpapi** was independently launched using the existing production-format \`Start-ProtectedXrayProfile.ps1\` (loopback 11821); a new \`curl --proxy\` request to \`https://example.com/\` returned **HTTP 200**. Stopped through \`Stop-ProtectedXrayProfile.ps1\` with no orphan listener. This verifies re-use of the automatically refreshed encrypted profile, not authenticated ChatGPT routing.
- Existing Happ, installed GUI v0.2.61, MCP runtime, tunnel-client and WebView2 login preserved; task did not change production routing.
- Initial terminal-only run finished node selection but could not encode a non-ASCII label with local cp1251 stdout. Fixed CLI JSON output to use ASCII escapes; Scheduled Task's subsequent run completed with exit code 0.

## Managing the updater

Status (without secrets):

```powershell
& 'D:\Dev\ChatGPT-MCP-Tunnel\scripts\Vpn\Get-FreeRealityStatus.ps1'
```

Manual refresh without waiting for the scheduled trigger:

```powershell
python 'D:\Dev\ChatGPT-MCP-Tunnel\scripts\Vpn\free_reality_updater.py'
```

Disable automatic refresh without removing the selected encrypted profile:

```powershell
& 'D:\Dev\ChatGPT-MCP-Tunnel\scripts\Vpn\Disable-FreeRealityRefreshTask.ps1'
```

Re-enable the existing task:

```powershell
Enable-ScheduledTask -TaskName 'ChatGPTMCP-FreeReality-Refresh'
```

Installation script for fresh machine: `scripts/Vpn/Install-FreeRealityRefreshTask.ps1`, does not overwrite an existing scheduled task.

## Security and production boundaries

Public free servers are run by unknown operators and can track destination metadata or disappear. TLS encrypts ChatGPT request contents end-to-end, but endpoint metadata remains visible to the operator. Do **not** silently route the user's signed-in ChatGPT session or private repositories through an arbitrary free public proxy. The task merely maintains the available validated nodes.

This manager depends on a working connection to GitHub raw feeds for updates. Existing last-good configuration persists during upstream outages. The scheduled task runs as the interactive Windows user; it will resume after that user's login and the next eligible trigger. Windows 10, Happ and the existing MCP tunnel remain intact.

The follow-on integration is a GUI selection for `Happ / Free Xray (experimental) / Trusted Xray` plus health-based switching of a dedicated child Xray process. That feature has not yet been released or E2E tested with the actual authenticated ChatGPT session and MCP client.

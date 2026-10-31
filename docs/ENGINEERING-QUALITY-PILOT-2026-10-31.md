# Engineering Quality Pilot — ChatGPT MCP Tunnel

Date: 2026-10-31. Owner-approved pilot applying the verifiable engineering principles of
unclebob/ub-mode to **this project's offline quality assurance**.

## What is implemented

- Policy manifest: `quality/standard.json`. No external paid services.
- Runner: `scripts/quality_runner.py`, standard-library Python, Windows-compatible.
- Self-tests: `tests/test_quality_runner.py`.
- Profiles: `quick` and `full`; fail fast, with bounded per-step timeouts.
- Evidence: UTC timestamp, source SHA, branch/dirty flag, command exit codes, stdout/stderr
  tails and per-step outcomes in local JSON (`.tools/quality-reports/`).
- Strict distinction between mocked/offline tests and **unverified live ChatGPT/installed-app/Owner acceptance**.
- Runner never calls GitHub Actions, publishes/releases software, installs updates or controls the live tunnel.

## Commands on DOMKAT

```powershell
cd D:\Dev\ChatGPT-MCP-Tunnel-Quality-Pilot
python scripts\quality_runner.py --plan
python scripts\quality_runner.py --profile quick
python scripts\quality_runner.py --profile full
```

Run only in a verified isolated worktree; builds write their normal bin/obj folders.
Reports remain local (excluded by gitignore). This pilot's source branch was based
on the authoritative product `main` baseline; it must not replace unfinished
WebView2/voice development. Merge/cherry-pick only after reconciling it with the
concurrent local changes and validating the current manager capsule.

## Engineering standard

1. State a user-visible, testable expected outcome before changing behavior.
2. Prefer a failing behavior test, reproduce, fix the root cause and confirm it passes.
3. Reconsider the shared design premise after two failed approaches based on it.
4. Separate protocol mocks from real host end-to-end evidence.
5. Keep operations repeatable and preserve a rollback route for changes to
   persistent data, update agents and transports.
6. Each commit says what changed, why and which tests passed.
7. Distinguish build, unit/mock, local integration, actual user host and Owner acceptance.
8. Never turn offline test success into claims about real ChatGPT message transport.

## Explicit future acceptance gates (not implemented in this pilot)

- Ordinary ChatGPT (not Work/Codex): local send -> actual assistant answer -> local display.
- New conversation, existing conversation, state after restart and retry/idempotency.
- Physical audio output, real local installation, preserving user settings and exact
  setup/update installer hashes; no automated mass email or unintended remote operations.
- Supervision/Repo Factory fleet integration after validated single-project pilot.

## Known limitations

The current Project Manager capsule may be structurally VALID but not READY under the
installed `pmctl.py` because historical belief/memory entries lack explicit
`source:` and `authority:` provenance. This pilot does **not** rewrite that
capsule or claim manager reinstantiation. The active worktree contains other
uncommitted WebView2/TTS changes and is left untouched.


## Execution evidence (2026-10-31, DOMKAT)

Product baseline: `main` at `b2e4f7795835f67d4693cad8ca73112d02462737`.
The separate worktree has branch `feature/engineering-quality-pilot-20261031`.

- Quick profile: **6/6 PASS**, local JSON:
  `.tools/quality-reports/quality-quick-20261031T092840720904Z.json`.
- First full attempt: **7/8 PASS, 1 FAIL**. The manifest referenced a test
  added only in concurrent, uncommitted WebView2 work. This was a quality
  manifest defect, **not** an application test failure. Removed the invalid
  reference and selected existing sandbox tests instead.
- Corrected full profile: **9/9 PASS**, local JSON:
  `.tools/quality-reports/quality-full-20261031T092948949295Z.json`.
- Both .NET 8 builds passed. MCP protocol, chat routing, retry and attachment
  verification ran offline/in isolated sandboxes or mocks.
- Live normal ChatGPT delivery: **NOT_TESTED**.
- Physical sound and installed app acceptance: **NOT_TESTED**.
- The active WebView2/TTS worktree was left unmodified and the active runtime
  was not restarted.

**Publication caveat:** the repository's `.github/workflows/repository-storage.yml`
currently declares `on: push`. Owner policy forbids running GitHub Actions.
Consequently, even a feature-branch push could trigger Actions. Until this is
made safe, retain the pilot commit locally rather than automatically push.

**Manager-state caveat:** `pmctl validate` succeeds, but `pmctl ready`
(non-authoritative inspection) reports incomplete provenance in belief/memory
entries even on the `main` baseline. No changes were made to the active
capsule or manager authority. Repair should be a separate, guarded task.

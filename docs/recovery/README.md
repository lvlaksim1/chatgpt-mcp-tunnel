# ChatGPT MCP Tunnel — Recovery Kit and client source audit

## What is recoverable

The repository owns the C# source of the MCP plugin/server and MCP Apps,
Router Lite server/client, Windows GUI, VPN proxy bridge, voice worker,
admin service, and installer scripts. Its four .NET projects compiled
in a separate Git checkout on DOMKAT on 31 October 2026.

External OpenAI tunnel-client, cloudflared, Xray-core, WebView2 runtime,
Sherpa/Piper dependencies and voice models are not code owned by this
repository. Recovery Kit packs verified Setup/Update containing
redistributable binaries and models, not their third-party source code.

Cloud custom-MCP plugin registration and ChatGPT account permissions
are provider-hosted records, not local plugin source files. The plugin
source is under src/ChatGptMcpTunnel, with MCP Apps HTML alongside it.

## Recovered MKATUSHONOK source

Six scripts were transferred privately via Router Lite, with verified SHA-256.
The running Free-Xray supervisor is now parameterized under
scripts/Recovery/Client-FreeXraySupervisor.ps1, with absolute
user-specific paths removed.

The Python updater accepts --xray-binary and --xray-sha256-file so
installed clients do not depend on developer-only .tools/vpn-pilot.

scripts/Recovery/Install-ClientFreeVpnTasks.ps1 is INSPECT_ONLY by
default. To register tasks on a fresh client, the operator must
supply -Apply. Replacing any existing task additionally requires
-ReplaceExisting. The script validates client role and installed Xray
SHA-256. It neither starts a second official tunnel-client nor touches Happ.

Both Inno installers package reusable recovery scripts under the
installed app Recovery folder. Packaging does NOT run or register tasks.

The original one-off WebView2 authorization and v0.2.65 update scripts
were preserved as NONEXECUTABLE, path-sanitized documents under
docs/recovery/legacy, with SHA-256 source provenance. They are not
general recovery commands and must not be executed on a new machine.
Their original bytes reside only in ignored Supervisor .tools staging.

A restored browser should use fresh authorized ChatGPT sign-in.
Restoration by copying cookies, tokens, WebView2 Local State and DPAPI
envelopes is NOT automated or implied by this kit.

## Build the credential-free kit

After a local version has been built, checked and committed, run
scripts/Recovery/build_recovery_kit.py in build mode with
--repo, --dist, --kit and --version arguments. Dist must contain
ChatGPT_MCP_Tunnel_Setup_vVERSION.exe, the matching Update EXE,
and SHA256-vVERSION.json (from the locally tested stable build).
The script refuses uncommitted changes, an existing output directory,
invalid manifest digests, or missing plugin source.

Then run the same script in verify mode with --kit pointing to the
resulting folder. It checks every artifact and the mandatory
MCP/plugin source code.

The kit includes two installers and source-snapshot.zip from the exact
Git commit, with a machine-readable recovery-manifest.json. It does
not include secrets, protected credentials, cookies, local WebView2
data, private certificates, cloud registrations, or Git history.
Additional controlled off-host backup is required for disaster recovery.

Build success on DOMKAT with its existing NuGet caches does not
prove a clean air-gapped rebuild of every dependency.

## Restore an authorized machine

1. Verify all recovery artifact SHA-256 hashes.
2. Install Setup and choose server or client. CLIENT is Router Lite
   agent only; it does not start an official OpenAI tunnel-client.
3. On CLIENT inspect the bundled Recovery/Install-ClientFreeVpnTasks.ps1
   without arguments. For a clean authorized client only, register the
   tasks using -Apply. Never use -ReplaceExisting without a deliberate
   replacement decision. Validate FreeReality refresh and VPN health.
4. On both machines perform a fresh authorized WebView2 ChatGPT sign-in
   as necessary, without importing old cookies or secrets by default.
5. On SERVER obtain owner-authorized OpenAI Platform tunnel credentials.
   Existing CurrentUser DPAPI files are NOT portable to other users.
6. Register the custom MCP plugin in the destination ChatGPT account
   with appropriate provider permissions; reuse the existing tunnel
   only when permitted. Creating a replacement tunnel is a separate,
   owner-approved disaster procedure.
7. Run MCP discovery, bridge_ping, computers.list, Windows tool checks,
   GUI, Router Lite, VPN, and voice verification; test rollback.
8. Keep RECOVERY_NOT_PROVEN until an isolated restore and owner acceptance.

## Security and operational invariants

- Exactly one official OpenAI tunnel-client on the SERVER.
- Router Lite CLIENT never launches another official tunnel-client.
- No production VPN/network/Windows service/browser changes during
  source staging or kit building.
- Do not commit .dpapi, PFX, keys, Xray URIs or session cookies.
- Cloud account records are not part of a local source archive.
- Installed E2E and another-account registration remain separate gates.
- Project Manager capsule may require --max-chars 262144 for RECOVER.

# RecoveryKit: permanent sidecar component of ChatGPT MCP Tunnel

## Lifecycle contract

Since the next stable locally built version after the implementation of
this contract, a release is **not successful** until the full, separate
RecoveryKit is assembled, verified and published. The kit is never
embedded into the executable it contains (that would be circular).

The standard release script, scripts/Build-LocalRelease.ps1, now:

1. Requires explicit -Version and a **clean, committed** source tree.
2. Builds full Setup and Update in Inno, then their SHA-256 manifest.
3. Uses scripts/Recovery/build_recovery_kit.py to package the source
   snapshot **from that commit**, both installers and a standalone Windows
   PowerShell integrity verifier.
4. Saves a distributable sidecar under
   .tools/local-release/vVERSION/dist/RecoveryKit.
5. Verifies all files in Python **and** Windows PowerShell.
6. Creates a separate RecoveryKit_vVERSION.zip asset for server-only
   distribution; the build itself NEVER installs the sidecar on any computer.
7. Refuses to report LOCAL_RELEASE_BUILD_OK if any mandatory step fails.
8. Full/Update installer verifies and installs the matching companion only
   when role=server. Client installation does not install RecoveryKit.

Canonical example on DOMKAT:

- Application: D:/ChatGPTMCPAgent/ChatGptMcpAgent.exe
- Recovery sidecar: D:/ChatGPTMCPAgent/RecoveryKit
- Versioned kit: D:/ChatGPTMCPAgent/RecoveryKit/Versions/vVERSION
- Latest verified *build* pointer: D:/ChatGPTMCPAgent/RecoveryKit/latest.json
- Portable distributed copy: D:/Dev/ChatGPT-MCP-Tunnel/.tools/
  local-release/vVERSION/dist/RecoveryKit

The latest.json version represents the last installed **server** RecoveryKit
release. Building the next version by itself does not modify this index;
only the server Setup/Update changes it after installing the main program.

Each build prepares dist/RecoveryKit and dist/RecoveryKit_vVERSION.zip, but
does not install either. Server Setup/Update expects a sibling RecoveryKit
folder; the application update downloader retrieves and verifies the ZIP asset
before stopping the current server. Clients never download or deploy the kit.

## Atomic publication, rollback and security

- Copy kit files into a uniquely named staging directory on the same
  disk as the installed application.
- Verify every SHA-256 and required plugin source in staging.
- Promote to Versions/vVERSION using an atomic directory rename.
- Update latest.json last via atomic file rename.
- Keep older verified versions for rollback; NEVER delete them during
  a normal build. Refuse downgrade and refuse replacing a version
  whose manifest checksum differs.
- Do not change the running GUI, MCP Runtime, UAC service, WebView2,
  OpenAI tunnel-client, Router Lite or VPN during this operation.
- Publish no cookies, keys, DPAPI, account settings, browser profiles
  or other live credentials into the kit.
- Concurrent publications fail closed using an exclusive lock directory.
  A stale lock must be inspected by an operator before manual removal.

On Windows no Python is needed to check the preserved recovery package:

    powershell -NoProfile -ExecutionPolicy Bypass -File
        D:\ChatGPTMCPAgent\RecoveryKit\Versions\vVERSION\Verify-RecoveryKit.ps1
        -InstallDir D:\ChatGPTMCPAgent

The published Python sidecar can be inspected with:

    python scripts/Recovery/publish_recovery_sidecar.py verify
        --install-dir D:\ChatGPTMCPAgent

Rollback to an older version is a separate, owner-authorized
operation using that version's Setup/Update after verifying its hash.
The publisher never switches running binaries or performs rollback.

## Limitations / acceptance gates

- The kit is installed only as part of installing/updating the main SERVER
  program. A CLIENT gets the ordinary application package only. RecoveryKit
  is never installed on client machines. For server-side manual installations,
  place the verified dist/RecoveryKit folder beside Setup/Update EXE.
- For server auto-updates, publish RecoveryKit_vVERSION.zip as an official
  stable release asset alongside the corresponding Update EXE; without it
  the server updater correctly fails before stopping the running version.
- **First migration from v0.2.65:** its existing updater downloads only the
  Update EXE. Install the first server-sidecar-aware stable version using the
  local Setup/Update EXE with its sibling RecoveryKit folder. Subsequent
  updates download the ZIP companion automatically on the server. Client
  machines require no migration and never receive the kit.
- Registration of the hosted MCP plugin in another ChatGPT account
  remains an independent test postponed until the owner has another
  account; it is not a release blocker.
- A build using already installed .NET/NuGet/Inno caches is not proof
  of a cold toolchain rebuild with no Internet.
- An off-host verified copy remains part of the backup policy and
  must be operated separately from merely storing the kit beside
  the same application disk.

The sidecar is **required by the local release pipeline**; it is not
just an optional per-release manual afterthought.

#!/usr/bin/env python3
"""Create/verify a credential-free offline recovery folder for ChatGPT MCP Tunnel.

An immutable, clean git HEAD source snapshot is bundled; no DPAPI data, cookies,
profiles, OpenAI keys or browser directories are read by this program.
"""
from __future__ import annotations
import argparse
import datetime
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import zipfile

SCHEMA = "chatgpt-mcp-tunnel-recovery-kit-v1"
REQUIRED_SOURCE = [
    "src/ChatGptMcpTunnel/Program.cs",
    "src/ChatGptMcpTunnel/ChatUiTools.cs",
    "src/ChatGptMcpTunnel/RouterLite.cs",
    "src/ChatGptMcpTunnel/ChatRelayApp.html",
    "src/ChatGptMcpTunnel/ChatLifecycleApp.html",
    "src/ChatGptMcpTunnel/ChatGlobalHubApp.html",
    "src/ChatGptMcpTunnel/ChatBootstrapApp.html",
    "src/ChatGptMcpAgent/ChatGptMcpAgent.csproj",
    "src/ChatGptMcpProxyBridge/ChatGptMcpProxyBridge.csproj",
    "src/ChatGptMcpVoice/ChatGptMcpVoice.csproj",
    "scripts/Recovery/Client-FreeXraySupervisor.ps1",
    "scripts/Recovery/Install-ClientFreeVpnTasks.ps1",
    "scripts/Recovery/publish_recovery_sidecar.py",
    "scripts/Recovery/Verify-RecoveryKit.ps1",
    "scripts/Recovery/build_recovery_kit.py",
    "scripts/Vpn/free_reality_updater.py",
    "installer/agent-full.iss",
    "installer/agent-update.iss",
]
FORBIDDEN_SUFFIXES = (".dpapi", ".pfx", ".p12", ".key", ".pem", ".sqlite")
FORBIDDEN_NAMES = {"cookies", "local state", "login data", "web data", "history"}
DEFAULT_DIST_NAMES = ("ChatGPT_MCP_Tunnel_Setup_v{version}.exe",
                      "ChatGPT_MCP_Tunnel_Update_v{version}.exe")

def git(repo: Path, *args: str) -> str:
    p = subprocess.run(["git", "-C", str(repo), *args],
                       capture_output=True, text=True, encoding="utf-8", check=True)
    return p.stdout.strip()

def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for block in iter(lambda: f.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()

def check_relative(name: str) -> str:
    p = Path(name.replace("\\", "/"))
    if (p.is_absolute() or ".." in p.parts or name.startswith(("/", "\\")) or ":" in name):
        raise ValueError("Unsafe artifact path")
    if p.suffix.lower() in FORBIDDEN_SUFFIXES or (p.name.lower() in FORBIDDEN_NAMES and not name.endswith("/")):
        raise ValueError("Sensitive artifact filename rejected")
    return p.as_posix()

def verify(kit: Path) -> dict:
    manifest_file = kit / "recovery-manifest.json"
    manifest = json.loads(manifest_file.read_text(encoding="utf-8"))
    if manifest.get("schema") != SCHEMA or manifest.get("containsSecrets") is not False:
        raise ValueError("Invalid schema or secrets policy")
    entries = manifest.get("artifacts", [])
    if len(entries) < 3:
        raise ValueError("Incomplete kit")
    found = set()
    for item in entries:
        name = check_relative(item["path"])
        if name in found:
            raise ValueError("Duplicate artifact")
        found.add(name)
        f = kit / name
        if not f.is_file() or f.stat().st_size != item["bytes"] or sha256(f) != item["sha256"]:
            raise ValueError("Missing or corrupt artifact: " + name)
    if "source-snapshot.zip" not in found:
        raise ValueError("Source snapshot missing")
    with zipfile.ZipFile(kit / "source-snapshot.zip") as source:
        names = source.namelist()
        for n in names:
            check_relative(n)
        missing = set(REQUIRED_SOURCE) - set(names)
        if missing:
            raise ValueError("Source snapshot incomplete: " + repr(sorted(missing)))
    if len([n for n in found if n.endswith(".exe")]) < 2:
        raise ValueError("Setup and Update required")
    return manifest

def build(repo: Path, dist: Path, out: Path, version: str) -> dict:
    head = git(repo, "rev-parse", "HEAD")
    if git(repo, "status", "--porcelain"):
        raise ValueError("Source checkout must be clean and committed before packaging")
    if out.exists():
        raise FileExistsError("Output folder already exists; no overwrite allowed")
    dist = dist.resolve()
    setups = [dist / template.format(version=version) for template in DEFAULT_DIST_NAMES]
    for src in setups:
        if not src.is_file() or src.stat().st_size < 1024 * 1024:
            raise ValueError("Missing verified local installer: " + src.name)
    sha_manifest = dist / ("SHA256-v" + version + ".json")
    if not sha_manifest.is_file():
        raise ValueError("Local installer SHA256 manifest missing")
    saved = {v["Name"]: v for v in json.loads(sha_manifest.read_text(encoding="utf-8-sig"))}
    for src in setups:
        info = saved.get(src.name)
        if not info or src.stat().st_size != info["Length"] or sha256(src).lower() != info["SHA256"].lower():
            raise ValueError("Local release artifact SHA256 mismatch: " + src.name)
    out.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="mcp-kit-", dir=out.parent) as temp:
        stage = Path(temp)
        archive = stage / "source-snapshot.zip"
        subprocess.run(["git", "-C", str(repo), "archive", "--format=zip",
                        "-o", str(archive), head], check=True,
                       stdout=subprocess.DEVNULL, stderr=subprocess.PIPE)
        with zipfile.ZipFile(archive) as z:
            names = set(z.namelist())
            if not set(REQUIRED_SOURCE).issubset(names):
                raise ValueError("Source commit missing required plugin/client source files")
            for name in names:
                check_relative(name)
        verifier_source = repo / "scripts" / "Recovery" / "Verify-RecoveryKit.ps1"
        if not verifier_source.is_file():
            raise ValueError("Standalone PowerShell integrity verifier missing")
        standalone_verifier = stage / "Verify-RecoveryKit.ps1"
        shutil.copy2(verifier_source, standalone_verifier)
        files = [archive, standalone_verifier]
        for src in setups:
            dest = stage / src.name
            shutil.copy2(src, dest)
            files.append(dest)
        manifest = {
            "schema": SCHEMA,
            "version": version,
            "sourceCommit": head,
            "sourceRemote": "https://github.com/lvlaksim1/chatgpt-mcp-tunnel",
            "createdUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
            "containsSecrets": False,
            "externalBinarySourceCodeIncluded": False,
            "cloudRegistrationIncluded": False,
            "credentialRecovery": "reauthorize or separately restore an owner-approved encrypted backup",
            "roles": ["server", "client"],
            "artifacts": [{"path": f.name, "bytes": f.stat().st_size,
                           "sha256": sha256(f)} for f in files],
        }
        (stage / "recovery-manifest.json").write_text(
            json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        readme = (
            "ChatGPT MCP Tunnel Recovery Kit v" + version + "\n"
            "This folder contains NO credentials, cookies, DPAPI profiles, private keys or cloud registrations.\n"
            "1. Verify without Python: powershell -NoProfile -File Verify-RecoveryKit.ps1 -Kit <folder>\n"
            "2. Install the Setup on a clean authorized Windows 10/11 host, choose SERVER or CLIENT.\n"
            "3. Sign in to ChatGPT WebView2 normally; configure required local VPN separately.\n"
            "4. On server obtain the owner-authorized tunnel_id and runtime key from OpenAI Platform;\n"
            "   register custom MCP in the destination ChatGPT account after permissions are confirmed.\n"
            "5. On a CLIENT, bundled Recovery scripts can install free VPN tasks after explicit -Apply.\n"
            "6. Source snapshot is complete at the listed commit; it is NOT a git history bundle.\n"
            "7. No claim of cross-account cloud restore or clean-room E2E is implied by this archive.\n"
            "8. Never copy *.dpapi or WebView2 profile to another Windows account as a shortcut.\n"
        )
        (stage / "RECOVERY-README.txt").write_text(readme, encoding="utf-8")
        verify(stage)
        # Atomic rename into final location after all digests and contents checked.
        os.replace(stage, out)
        # TemporaryDirectory cleanup of moved original path is a no-op.
    return verify(out)

def main() -> int:
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest="command", required=True)
    b = sub.add_parser("build")
    b.add_argument("--repo", type=Path, required=True)
    b.add_argument("--dist", type=Path, required=True)
    b.add_argument("--kit", type=Path, required=True)
    b.add_argument("--version", required=True)
    v = sub.add_parser("verify")
    v.add_argument("--kit", type=Path, required=True)
    args = parser.parse_args()
    try:
        data = (build(args.repo.resolve(), args.dist, args.kit.resolve(), args.version)
                if args.command == "build" else verify(args.kit.resolve()))
        print(json.dumps({"status": "PASS", "kit": str(args.kit), "version": data["version"],
                          "sourceCommit": data["sourceCommit"], "artifacts": len(data["artifacts"]),
                          "containsSecrets": data["containsSecrets"]}, ensure_ascii=False))
        return 0
    except (OSError, ValueError, KeyError, subprocess.CalledProcessError, zipfile.BadZipFile) as e:
        print(json.dumps({"status": "FAIL", "reason": type(e).__name__,
                          "message": str(e)[:240]}, ensure_ascii=False), file=sys.stderr)
        return 1

if __name__ == "__main__":
    raise SystemExit(main())

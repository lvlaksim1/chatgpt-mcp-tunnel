#!/usr/bin/env python3
"""Publish and verify the mandatory, versioned RecoveryKit sidecar beside the app.

This is deliberately NOT part of an installer self-extracting EXE. Full Setup and
Update remain ordinary executable files; source ZIP, manifest and PowerShell
verification are kept separately and retained across in-place updates.
"""
from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import sys
import tempfile
import uuid

from build_recovery_kit import verify as verify_kit, sha256

SCHEMA = "chatgpt-mcp-tunnel-recovery-sidecar-v1"
VERSION = re.compile(r"^0|[1-9][0-9]*$")  # use semver() for complete validation


def semver(value: str) -> tuple[int, int, int]:
    if not re.fullmatch(r"(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)", value):
        raise ValueError("Invalid three-part release version")
    return tuple(int(part) for part in value.split("."))


def sidecar_dir(install_dir: Path) -> Path:
    if not (install_dir / "ChatGptMcpAgent.exe").is_file():
        raise ValueError("No installed ChatGptMcpAgent.exe; cannot publish near this program")
    if install_dir.is_symlink():
        raise ValueError("Symlinked install directory is prohibited")
    root = install_dir / "RecoveryKit"
    if root.is_symlink():
        raise ValueError("Symlinked recovery root is prohibited")
    return root


def read_latest(root: Path) -> dict | None:
    f = root / "latest.json"
    if not f.is_file():
        return None
    data = json.loads(f.read_text(encoding="utf-8"))
    if data.get("schema") != SCHEMA:
        raise ValueError("Unknown RecoveryKit index")
    version = data.get("latestVersion", "")
    semver(version)
    if data.get("relativePath") != "Versions/v" + version:
        raise ValueError("RecoveryKit index contains unsafe version path")
    return data


def describe(install_dir: Path) -> dict:
    root = sidecar_dir(install_dir)
    index = read_latest(root)
    if index is None:
        raise ValueError("No published RecoveryKit beside program")
    kit = root / index["relativePath"]
    if not kit.is_dir() or kit.is_symlink():
        raise ValueError("Current RecoveryKit version missing or linked")
    manifest = verify_kit(kit)
    if manifest["version"] != index["latestVersion"]:
        raise ValueError("Index points to wrong RecoveryKit version")
    if manifest["sourceCommit"] != index["sourceCommit"]:
        raise ValueError("RecoveryKit source commit changed")
    if sha256(kit / "recovery-manifest.json") != index["manifestSha256"]:
        raise ValueError("RecoveryKit manifest checksum changed")
    versions = []
    folder = root / "Versions"
    for item in folder.iterdir():
        if not item.is_dir() or item.is_symlink() or not item.name.startswith("v"):
            continue
        try:
            ver = item.name[1:]
            semver(ver)
            data = verify_kit(item)
            if data["version"] == ver:
                versions.append(ver)
        except (OSError, ValueError, KeyError, json.JSONDecodeError):
            pass
    return {
        "status": "PASS",
        "installDir": str(install_dir),
        "sidecar": str(root),
        "latestBuildVersion": index["latestVersion"],
        "sourceCommit": index["sourceCommit"],
        "versionsVerified": sorted(versions, key=semver),
        "containsSecrets": False,
    }


def publish(kit: Path, install_dir: Path) -> dict:
    kit = kit.resolve(strict=True)
    manifest = verify_kit(kit)
    version = manifest["version"]
    semver(version)
    root = sidecar_dir(install_dir)
    versions = root / "Versions"
    versions.mkdir(parents=True, exist_ok=True)
    lock = root / ".publish-lock"
    # Creation is exclusive; no other build can be in the promotion window.
    lock.mkdir()
    try:
        latest = read_latest(root)
        if latest and semver(version) < semver(latest["latestVersion"]):
            raise ValueError("Downgrade is prohibited; previous kits are retained for manual rollback")
        destination = versions / ("v" + version)
        if destination.is_symlink():
            raise ValueError("Versioned RecoveryKit path must not be a link")
        if destination.exists():
            verified = verify_kit(destination)
            if (verified["version"] != version or
                sha256(destination / "recovery-manifest.json") != sha256(kit / "recovery-manifest.json")):
                raise ValueError("RecoveryKit version conflict: refusing overwrite")
        else:
            stage = versions / (".staging-" + uuid.uuid4().hex)
            try:
                shutil.copytree(kit, stage, symlinks=False)
                staged = verify_kit(stage)
                if staged["version"] != version or (
                    sha256(stage / "recovery-manifest.json") != sha256(kit / "recovery-manifest.json")
                ):
                    raise ValueError("Staging verification failed")
                os.replace(stage, destination)
            finally:
                if stage.exists():
                    shutil.rmtree(stage)
        # Prior version stays available until after the verified new kit is durable.
        index = {
            "schema": SCHEMA,
            "latestVersion": version,
            "relativePath": "Versions/v" + version,
            "sourceCommit": manifest["sourceCommit"],
            "manifestSha256": sha256(destination / "recovery-manifest.json"),
            "createdUtc": dt.datetime.now(dt.timezone.utc).isoformat(),
            "containsSecrets": False,
            "semantics": "latest verified build; not necessarily the running installed version",
        }
        tmp = root / (".latest-" + uuid.uuid4().hex + ".tmp")
        try:
            tmp.write_text(json.dumps(index, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
            os.replace(tmp, root / "latest.json")
        finally:
            tmp.unlink(missing_ok=True)
        return describe(install_dir)
    finally:
        lock.rmdir()


def main() -> int:
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest="command", required=True)
    p = sub.add_parser("publish", help="atomically publish a verified kit next to program")
    p.add_argument("--kit", type=Path, required=True)
    p.add_argument("--install-dir", type=Path, required=True)
    v = sub.add_parser("verify", help="verify latest and list intact retained versions")
    v.add_argument("--install-dir", type=Path, required=True)
    args = parser.parse_args()
    try:
        result = (publish(args.kit, args.install_dir) if args.command == "publish"
                  else describe(args.install_dir))
        print(json.dumps(result, ensure_ascii=False))
        return 0
    except (OSError, ValueError, KeyError, json.JSONDecodeError) as e:
        print(json.dumps({"status":"FAIL","type":type(e).__name__,
                          "error":str(e)[:250]}, ensure_ascii=False),file=sys.stderr)
        return 1

if __name__ == "__main__":
    raise SystemExit(main())

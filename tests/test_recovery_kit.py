"""Offline recovery-kit and installed-client static regressions.

No cloud registration, VPN, browser profile, Windows service or live app is changed.
"""
from __future__ import annotations
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "scripts" / "Recovery"))
import build_recovery_kit as kit

def run(*args, cwd=None):
    return subprocess.run(args, cwd=cwd, check=True,
                          capture_output=True, text=True, encoding="utf-8").stdout

class RecoveryTests(unittest.TestCase):
    def test_source_dependencies_present_and_safe(self):
        for name in kit.REQUIRED_SOURCE:
            self.assertTrue((ROOT / name).is_file(), name)
        for filename in ("installer/agent-full.iss", "installer/agent-update.iss"):
            s = (ROOT / filename).read_text(encoding="utf-8")
            for name in ("Client-FreeXraySupervisor.ps1", "Install-ClientFreeVpnTasks.ps1",
                         "free_reality_updater.py", "free_reality_sources.json"):
                self.assertIn(name, s)
        script = (ROOT / "scripts/Recovery/Client-FreeXraySupervisor.ps1").read_text(encoding="utf-8")
        self.assertIn("neverUseHapp=$true", script)
        self.assertNotIn("C:\\Users\\m.katushonok", script)
        installer = (ROOT / "scripts/Recovery/Install-ClientFreeVpnTasks.ps1").read_text(encoding="utf-8")
        self.assertIn("[switch]$Apply", installer)
        self.assertIn("Refusing to change existing tasks", installer)
        self.assertNotIn("tunnel-client.exe", installer)

    def test_no_credential_names_in_recovery_archive(self):
        for file in (ROOT / "docs/recovery/legacy").glob("*.txt"):
            data = file.read_text(encoding="utf-8")
            self.assertNotIn("C:\\Users\\m.katushonok", data)
            self.assertIn("ARCHIVE ONLY", data)

    def test_explicit_xray_binary_hash(self):
        spec = importlib.util.spec_from_file_location(
            "free_reality_updater", ROOT / "scripts/Vpn/free_reality_updater.py")
        mod = importlib.util.module_from_spec(spec)
        assert spec.loader
        spec.loader.exec_module(mod)
        with tempfile.TemporaryDirectory() as tmp:
            d=Path(tmp)
            exe=d/"xray.exe"
            exe.write_bytes(b"offline test xray fixture, NOT executable")
            sha=d/"xray.sha256"
            sha.write_text(hashlib.sha256(exe.read_bytes()).hexdigest()+"\n", encoding="ascii")
            self.assertEqual(mod.verified_engine(exe, sha), str(exe))
            with self.assertRaises(RuntimeError):
                mod.verified_engine(exe, None)
            sha.write_text("0"*64, encoding="ascii")
            with self.assertRaises(RuntimeError):
                mod.verified_engine(exe, sha)
            sha.write_text("not a hash", encoding="ascii")
            with self.assertRaises(RuntimeError):
                mod.verified_engine(exe, sha)

    def test_kit_build_verify_and_tampering(self):
        with tempfile.TemporaryDirectory(prefix="tunnel-kit-test-") as tmp:
            d=Path(tmp)
            repo=d/"repo"
            dist=d/"dist"
            repo.mkdir()
            dist.mkdir()
            run("git", "init", "-q", str(repo))
            run("git", "-C", str(repo), "config", "user.email", "fixture@example.invalid")
            run("git", "-C", str(repo), "config", "user.name", "Fixture")
            for rel in kit.REQUIRED_SOURCE:
                file=repo / rel
                file.parent.mkdir(parents=True, exist_ok=True)
                file.write_text("synthetic fixture\n", encoding="utf-8")
            # Untracked secrets must never be packaged in a git archive.
            secret=repo/"webview"/"Cookies"
            secret.parent.mkdir(parents=True, exist_ok=True)
            secret.write_text("never-copy-this-secret", encoding="utf-8")
            (repo / ".gitignore").write_text("webview/\n", encoding="utf-8")
            run("git", "-C", str(repo), "add", ".gitignore", *kit.REQUIRED_SOURCE)
            run("git", "-C", str(repo), "commit", "-qm", "synthetic recovery fixture")
            ver="0.0.1"
            entries=[]
            for name in kit.DEFAULT_DIST_NAMES:
                filename=name.format(version=ver)
                f=dist/filename
                f.write_bytes(b"Z"*1_050_000)
                entries.append({"Name":filename,"Length":f.stat().st_size,
                                "SHA256":kit.sha256(f)})
            (dist/"SHA256-v0.0.1.json").write_text(json.dumps(entries), encoding="utf-8")
            output=d/"kit"
            data=kit.build(repo,dist,output,ver)
            self.assertEqual(data["schema"],kit.SCHEMA)
            self.assertFalse(data["containsSecrets"])
            with self.assertRaises(FileExistsError):
                kit.build(repo,dist,output,ver)
            with self.assertRaises(ValueError):
                kit.check_relative("..\\Cookies")
            with self.assertRaises(ValueError):
                kit.check_relative("a.dpapi")
            with self.assertRaises(ValueError):
                kit.check_relative("Default/Network/Cookies")
            with __import__("zipfile").ZipFile(output/"source-snapshot.zip") as z:
                self.assertNotIn("webview/Cookies",z.namelist())
                self.assertEqual(set(kit.REQUIRED_SOURCE)-set(z.namelist()),set())
            p=output/entries[0]["Name"]
            with p.open("ab") as f:
                f.write(b"tampered")
            with self.assertRaises(ValueError):
                kit.verify(output)

if __name__ == "__main__":
    unittest.main(verbosity=2)

"""RecoveryKit sidecar lifecycle: atomic promotion, retention, integrity, fail-close."""
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest
import zipfile

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/"scripts/Recovery"))
import build_recovery_kit as builder
import publish_recovery_sidecar as sidecar

def fixture(root: Path, version: str, source: str="f"*40) -> Path:
    kit=root/("kit-"+version)
    kit.mkdir()
    with zipfile.ZipFile(kit/"source-snapshot.zip","w",compression=zipfile.ZIP_DEFLATED) as archive:
        for rel in builder.REQUIRED_SOURCE:
            archive.writestr(rel, "source fixture\n")
    shutil.copy2(ROOT/"scripts/Recovery/Verify-RecoveryKit.ps1",kit/"Verify-RecoveryKit.ps1")
    for name in builder.DEFAULT_DIST_NAMES:
        (kit/name.format(version=version)).write_bytes(b"MZ"+bytes([int(version.rsplit(".",1)[1])])*101)
    artifacts=[]
    for file in kit.iterdir():
        artifacts.append({"path":file.name,"bytes":file.stat().st_size,"sha256":builder.sha256(file)})
    (kit/"recovery-manifest.json").write_text(json.dumps({
        "schema":builder.SCHEMA,"version":version,"sourceCommit":source,
        "containsSecrets":False,"artifacts":artifacts
    }),encoding="utf-8")
    return kit

class SidecarTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory(prefix="recovery-sidecar-")
        self.addCleanup(self.tmp.cleanup)
        self.root=Path(self.tmp.name)
        self.app=self.root/"installed program"
        self.app.mkdir()
        (self.app/"ChatGptMcpAgent.exe").write_bytes(b"running fixture 0.2.65")
        self.k1=fixture(self.root,"0.2.66")

    def test_first_publish_idempotency(self):
        x=sidecar.publish(self.k1,self.app)
        self.assertEqual(x["latestBuildVersion"],"0.2.66")
        self.assertTrue((self.app/"RecoveryKit/Versions/v0.2.66/source-snapshot.zip").exists())
        self.assertEqual(sidecar.publish(self.k1,self.app)["latestBuildVersion"],"0.2.66")
        self.assertEqual(x["versionsVerified"],["0.2.66"])
        self.assertFalse((self.app/"RecoveryKit/.publish-lock").exists())
        self.assertEqual(json.loads((self.app/"RecoveryKit/latest.json").read_text())["semantics"],
                         "latest verified build; not necessarily the running installed version")

    def test_update_retains_prior_version(self):
        sidecar.publish(self.k1,self.app)
        k2=fixture(self.root,"0.2.67",source="a"*40)
        x=sidecar.publish(k2,self.app)
        self.assertEqual(x["latestBuildVersion"],"0.2.67")
        self.assertEqual(x["versionsVerified"],["0.2.66","0.2.67"])
        self.assertTrue((self.app/"RecoveryKit/Versions/v0.2.66").is_dir())
        with self.assertRaisesRegex(ValueError,"Downgrade"):
            sidecar.publish(self.k1,self.app)
        self.assertEqual(sidecar.describe(self.app)["latestBuildVersion"],"0.2.67")

    def test_corrupted_staging_does_not_replace_previous(self):
        sidecar.publish(self.k1,self.app)
        k2=fixture(self.root,"0.2.67")
        with (k2/"source-snapshot.zip").open("ab") as f:
            f.write(b"tamper")
        with self.assertRaisesRegex(ValueError,"corrupt"):
            sidecar.publish(k2,self.app)
        self.assertEqual(sidecar.describe(self.app)["latestBuildVersion"],"0.2.66")
        self.assertFalse((self.app/"RecoveryKit/Versions/v0.2.67").exists())

    def test_same_version_conflict_preserves_old(self):
        sidecar.publish(self.k1,self.app)
        other=self.root/"another-source"; other.mkdir()
        conflict=fixture(other,"0.2.66",source="a"*40)
        with self.assertRaisesRegex(ValueError,"conflict"):
            sidecar.publish(conflict,self.app)
        self.assertEqual(sidecar.describe(self.app)["sourceCommit"],"f"*40)

    def test_tamper_detected_after_publish(self):
        sidecar.publish(self.k1,self.app)
        installed=self.app/"RecoveryKit/Versions/v0.2.66"
        (installed/"Verify-RecoveryKit.ps1").write_text("bad",encoding="utf-8")
        with self.assertRaisesRegex(ValueError,"corrupt"):
            sidecar.describe(self.app)

    def test_missing_app_rejected(self):
        with self.assertRaisesRegex(ValueError,"No installed"):
            sidecar.publish(self.k1,self.root)

    def test_concurrent_lock_protects_pointer(self):
        sidecar.publish(self.k1,self.app)
        (self.app/"RecoveryKit/.publish-lock").mkdir()
        with self.assertRaises(FileExistsError):
            sidecar.publish(self.k1,self.app)
        self.assertEqual(sidecar.describe(self.app)["latestBuildVersion"],"0.2.66")

    def test_standalone_windows_powershell_verifier(self):
        result=subprocess.run(["powershell.exe","-NoProfile","-ExecutionPolicy","Bypass","-File",
                               str(self.k1/"Verify-RecoveryKit.ps1"),"-Kit",str(self.k1)],
                              capture_output=True,text=True,encoding="utf-8")
        self.assertEqual(result.returncode,0,(result.stdout+"\\n"+result.stderr)[-2000:])
        self.assertIn("PASS",result.stdout)
        sidecar.publish(self.k1,self.app)
        result=subprocess.run(["powershell.exe","-NoProfile","-ExecutionPolicy","Bypass","-File",
                               str(self.k1/"Verify-RecoveryKit.ps1"),"-InstallDir",str(self.app)],
                              capture_output=True,text=True,encoding="utf-8")
        self.assertEqual(result.returncode,0,(result.stdout+"\\n"+result.stderr)[-2000:])
        self.assertIn("PASS",result.stdout)

    def test_previous_kit_schema_without_sidecar_version(self):
        legacy=self.root/"legacy-kit"; legacy.mkdir()
        with zipfile.ZipFile(legacy/"source-snapshot.zip","w") as archive:
            for name in builder.BASE_REQUIRED_SOURCE:
                archive.writestr(name,"old source")
        for name in builder.DEFAULT_DIST_NAMES:
            (legacy/name.format(version="0.2.65")).write_bytes(b"MZ-legacy")
        files=[{"path":f.name,"bytes":f.stat().st_size,"sha256":builder.sha256(f)} for f in legacy.iterdir()]
        (legacy/"recovery-manifest.json").write_text(json.dumps({"schema":builder.SCHEMA,"version":"0.2.65","sourceCommit":"f"*40,"containsSecrets":False,"artifacts":files}),encoding="utf-8")
        self.assertEqual(builder.verify(legacy)["version"],"0.2.65")

    def test_source_scripts_do_not_reference_an_installed_version(self):
        s=(ROOT/"scripts/Build-LocalRelease.ps1").read_text(encoding="utf-8")
        self.assertIn("RECOVERY_KIT_BUILD",s)
        self.assertIn("mandatory Recovery Kit generation failed",s)
        self.assertIn("publish --kit",s)
        self.assertIn("RECOVERY_SIDECAR_DIST_ONLY",s)
        self.assertIn("[Parameter(Mandatory=$true)]",s)
        self.assertNotIn("[string]$Version = '0.2.27'",s)
        v=(ROOT/"scripts/Recovery/Verify-RecoveryKit.ps1").read_text(encoding="utf-8")
        self.assertIn("Get-FileHash",v)
        self.assertIn("source-snapshot.zip",v)

if __name__=="__main__":
    unittest.main(verbosity=2)

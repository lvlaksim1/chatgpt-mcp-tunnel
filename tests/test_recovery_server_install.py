"""Server-only RecoveryKit deployment acceptance, run in a synthetic Windows app dir.

Never touches the production app, HKCU role, browser profiles or VPN. Uses
only the existing server's recorded role or a function mock for client test.
"""
from __future__ import annotations
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

INSTALL=ROOT/"scripts/Recovery/Install-RecoveryKit.ps1"
VERIFY=ROOT/"scripts/Recovery/Verify-RecoveryKit.ps1"

def run_ps(script: Path,*args: str):
    return subprocess.run(["powershell.exe","-NoProfile","-NonInteractive",
                           "-ExecutionPolicy","Bypass","-File",str(script),*args],
                          capture_output=True,text=True,encoding="utf-8")

def fixture(root: Path,version: str) -> Path:
    kit=root/("companion-"+version)
    kit.mkdir()
    with zipfile.ZipFile(kit/"source-snapshot.zip","w") as archive:
        for name in builder.REQUIRED_SOURCE:
            archive.writestr(name,"synthetic source")
    shutil.copy2(VERIFY,kit/"Verify-RecoveryKit.ps1")
    for name in builder.DEFAULT_DIST_NAMES:
        (kit/name.format(version=version)).write_bytes(b"MZ synthetic, not executable")
    artifacts=[{"path":p.name,"bytes":p.stat().st_size,"sha256":builder.sha256(p)}
               for p in kit.iterdir()]
    (kit/"recovery-manifest.json").write_text(json.dumps({
        "schema":builder.SCHEMA,"version":version,"sidecarVersion":2,
        "sourceCommit":"a"*40,"containsSecrets":False,"artifacts":artifacts
    }),encoding="utf-8")
    return kit

class ServerInstallTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory(prefix="recovery-server-installer-")
        self.addCleanup(self.tmp.cleanup)
        self.root=Path(self.tmp.name)
        self.app=self.root/"server program"
        self.app.mkdir()
        (self.app/"ChatGptMcpAgent.exe").write_bytes(b"MZ fake installed application")
        self.kit=fixture(self.root,"0.2.68")

    def ps(self,*arguments):
        return run_ps(INSTALL,"-Kit",str(self.kit),"-InstallDir",str(self.app),
                      "-Version",*arguments)

    def test_preflight_does_not_install(self):
        result=self.ps("0.2.68","-Preflight")
        self.assertEqual(result.returncode,0,result.stderr[-1600:])
        self.assertIn("PREFLIGHT_OK",result.stdout)
        self.assertFalse((self.app/"RecoveryKit").exists())

    def test_server_install_and_idempotency(self):
        first=self.ps("0.2.68")
        self.assertEqual(first.returncode,0,first.stderr[-1600:])
        self.assertIn("INSTALLED",first.stdout)
        folder=self.app/"RecoveryKit"
        self.assertTrue((folder/"Versions/v0.2.68/source-snapshot.zip").exists())
        self.assertEqual(json.loads((folder/"latest.json").read_text())["latestVersion"],"0.2.68")
        verified=run_ps(VERIFY,"-InstallDir",str(self.app),"-ExpectedVersion","0.2.68")
        self.assertEqual(verified.returncode,0,verified.stderr[-1600:])
        second=self.ps("0.2.68")
        self.assertEqual(second.returncode,0,second.stderr[-1600:])
        self.assertFalse((folder/".install-lock").exists())

    def test_version_mismatch_leaves_no_kit(self):
        failure=self.ps("0.2.69")
        self.assertNotEqual(failure.returncode,0)
        self.assertFalse((self.app/"RecoveryKit").exists())

    def test_client_role_spoof_rejected_without_changing_registry(self):
        # PowerShell function shadowing provides a test-only synthetic client;
        # no writes to the real HKCU\Software\ChatGPTMCPAgent role.
        powershell_code=(
          "function Get-ItemProperty { [pscustomobject]@{Role='client'} }; "
          "try { & '" + str(INSTALL).replace("'","''") + "' -Kit '" +
          str(self.kit).replace("'","''") + "' -InstallDir '" +
          str(self.app).replace("'","''") +
          "' -Version '0.2.68'; exit 10 } "
          "catch { if($_.Exception.Message -match 'client'){exit 0}else{exit 11} }"
        )
        result=subprocess.run(["powershell.exe","-NoProfile","-Command",powershell_code],
                              capture_output=True,text=True)
        self.assertEqual(result.returncode,0,(result.stdout+result.stderr)[-1200:])
        self.assertFalse((self.app/"RecoveryKit").exists())

    def test_installer_and_updater_role_contract_static(self):
        for name in ("installer/agent-full.iss","installer/agent-update.iss"):
            source=(ROOT/name).read_text(encoding="utf-8")
            self.assertIn("Flags: dontcopy",source)
            self.assertIn("Install-RecoveryKit.ps1",source)
            self.assertIn("Verify-RecoveryKit.ps1",source)
            self.assertIn("KitExitCode",source)
        exe=(ROOT/"src/ChatGptMcpAgent/AgentUpdates.cs").read_text(encoding="utf-8")
        self.assertIn('AgentStorage.SelectedRole != "server"',exe)
        self.assertIn('AgentStorage.SelectedRole == "server"',exe)
        self.assertIn("RecoveryKit_v",exe)
        build=(ROOT/"scripts/Build-LocalRelease.ps1").read_text(encoding="utf-8")
        self.assertIn("RECOVERY_KIT_COMPANION_ONLY",build)
        self.assertNotIn("publish --kit",build)

if __name__=="__main__":
    unittest.main(verbosity=2)

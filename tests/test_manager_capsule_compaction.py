"""Verify exact historical bytes of capsule generation 76 and bounded recovery."""
from pathlib import Path
import hashlib,json,subprocess,sys,zipfile
R=Path(__file__).resolve().parents[1]
A=R/".context/archive/manager-gen76-2026-10-31"
manifest=json.loads((A/"manifest.json").read_text(encoding="utf-8"))
seal=json.loads((R/".context/manager/state-integrity.json").read_text(encoding="utf-8"))
assert manifest["original_generation"]==76
assert seal["generation"]>=77
assert len(manifest["files"])==len(seal["coupled_paths"])==8
assert {entry["original"] for entry in manifest["files"]}==set(seal["coupled_paths"])
zip_path=R/manifest["lossless_zip"]
assert hashlib.sha256(zip_path.read_bytes()).hexdigest()==manifest["lossless_zip_sha256"]
old=0
new=0
with zipfile.ZipFile(zip_path,"r") as archive:
    assert set(archive.namelist())==set(seal["coupled_paths"])
    for entry in manifest["files"]:
        rel=entry["original"]
        data=archive.read(rel)
        assert hashlib.sha256(data).hexdigest()==entry["sha256"],rel
        assert len(data)==entry["bytes"],rel
        readable=(R/entry["archive"]).read_bytes()
        # Human-readable .md can be newline-normalized on different Git checkouts.
        assert readable.decode("utf-8").replace("\r\n","\n")==data.decode("utf-8").replace("\r\n","\n"),rel
        digest=subprocess.run(["git","hash-object","--",rel],cwd=R,
                              capture_output=True,text=True,check=True).stdout.strip()
        assert seal["digests"][rel]=="git-blob-sha1:"+digest,rel
        old+=len(data)
        new+=(R/rel).stat().st_size
assert old>400000 and new<60000,(old,new)
goals=(R/".context/manager/goals.md").read_text(encoding="utf-8").lower()
for val in ("семь долгосрочных целей","полноценное исполнение задач ии",
            "равноправие и автономность","двусторонний локальный чат",
            "голосовая связь","ollama/goose","безопасность"):
    assert val in goals,val
for path in seal["coupled_paths"]:
    assert "archive/manager-gen76-2026-10-31/README.md" in (R/path).read_text(encoding="utf-8"),path
pm=Path(r"D:\Dev\context-capsule-project-manager\installer\pmctl.py")
if pm.is_file():
    branch=subprocess.run(["git","branch","--show-current"],cwd=R,
                          capture_output=True,text=True,check=True).stdout.strip()
    for action in ("validate","ready","recover"):
        cmd=[sys.executable,str(pm),action,"--target",str(R)]
        if action!="validate" and branch!="main":
            cmd.append("--non-authoritative")
        process=subprocess.run(cmd,capture_output=True)
        assert process.returncode==0,(action,process.stdout[:320],process.stderr[:320])
        if action=="recover":
            assert len(process.stdout.decode("utf-8"))<=131072
print("MANAGER_COMPACTION_LOSSLESS_ARCHIVE_8_OF_8_PASS")
print("original_bytes="+str(old)+" working_bytes="+str(new))

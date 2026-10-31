"""Lossless manager-state compacting regression. No installed app/network access."""
from pathlib import Path
import subprocess,hashlib,json,re,sys
ROOT=Path(__file__).resolve().parents[1]
archive=ROOT/".context/archive/manager-gen76-2026-10-31"
info=json.loads((archive/"manifest.json").read_text(encoding="utf-8"))
seal=json.loads((ROOT/".context/manager/state-integrity.json").read_text(encoding="utf-8"))
assert info["original_generation"]==76
assert seal["generation"]>=77
assert len(info["files"])==len(seal["coupled_paths"])==8
assert {x["original"] for x in info["files"]}==set(seal["coupled_paths"])
old=0
new=0
for entry in info["files"]:
    src=ROOT/entry["archive"]
    rel=entry["original"]
    assert src.is_file(),rel
    raw=src.read_bytes()
    assert hashlib.sha256(raw).hexdigest()==entry["sha256"],rel
    assert len(raw)==entry["bytes"],rel
    current=(ROOT/rel).read_bytes()
    digest=subprocess.run(["git","hash-object","--",rel],cwd=ROOT,
                          capture_output=True,text=True,check=True).stdout.strip()
    assert seal["digests"][rel]=="git-blob-sha1:"+digest,rel
    old+=len(raw)
    new+=len(current)
assert old>400000 and new<60000, (old,new)
goals=(ROOT/".context/manager/goals.md").read_text(encoding="utf-8")
for needle in ("Семь долгосрочных целей","Полноценное исполнение задач ИИ",
               "Равноправие и автономность","Двусторонний локальный чат",
               "голосовая связь","Ollama/Goose","безопасность"):
    assert needle.lower() in goals.lower(),needle
for rel in seal["coupled_paths"]:
    s=(ROOT/rel).read_text(encoding="utf-8")
    assert "archive/manager-gen76-2026-10-31/README.md" in s,rel
pm=Path(r"D:\Dev\context-capsule-project-manager\installer\pmctl.py")
if pm.is_file():
    for action in ("validate","ready","recover"):
        argv=[sys.executable,str(pm),action,"--target",str(ROOT)]
        if action!="validate" and subprocess.run(["git","branch","--show-current"],cwd=ROOT,
                    capture_output=True,text=True).stdout.strip()!="main":
            argv.append("--non-authoritative")
        result=subprocess.run(argv,capture_output=True,check=False)
        assert result.returncode==0,(action,result.stdout[:240],result.stderr[:240])
        if action=="recover":
            assert len(result.stdout.decode("utf-8"))<=131072
print(f"MANAGER_COMPACTION_ARCHIVE_8_8_PASS original_bytes={old} working_bytes={new}")

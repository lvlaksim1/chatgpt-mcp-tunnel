"""Standalone self-checks for the offline quality gate; never touches live Tunnel."""
import json
from pathlib import Path
import sys
import tempfile

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "scripts"))
import quality_runner as q


def step(name, script, timeout=10):
    return {"id": name, "kind": "offline",
            "command": [sys.executable, "-c", script], "timeout_sec": timeout}


with tempfile.TemporaryDirectory(prefix="quality-runner-test-") as d:
    root = Path(d)
    success = step("one", "print('OK')")
    failure = step("two", "import sys; sys.exit(9)")
    skipped = step("three", "raise RuntimeError('must not run')")
    results = q.execute_steps([success, failure, skipped], root)
    assert [r["status"] for r in results] == ["PASS", "FAIL", "NOT_RUN"], results
    assert results[0]["stdout_tail"].strip() == "OK"
    assert results[1]["exit_code"] == 9
    assert results[2]["reason"].startswith("Skipped")
    report = q.create_report("quick", root, results)
    assert report["offline_gate"] == "FAIL"
    assert report["live_chat_e2e"] == "NOT_TESTED"
    assert report["installed_application"] == "NOT_TESTED"
    assert report["release_authorized"] is False
    assert q.create_report("quick", root, q.execute_steps([success], root))["offline_gate"] == "PASS"

    timeout = step("stall", "import time; time.sleep(5)", 1)
    result = q.execute_steps([timeout], root)[0]
    assert result["status"] == "TIMEOUT", result

    bad = root / "bad.json"
    bad.write_text(json.dumps({
        "schema": "engineering-quality-pilot-v1",
        "quick": [success, success], "full_additional": []
    }), encoding="utf-8")
    try:
        q.load_steps(bad, "quick")
        raise AssertionError("Duplicate steps should have failed")
    except ValueError:
        pass

    bad.write_text(json.dumps({
        "schema": "engineering-quality-pilot-v1",
        "quick": [{"id": "danger", "kind": "offline",
                   "command": ["powershell", "Remove-Item", "C:\\"] , "timeout_sec": 10}],
        "full_additional": []
    }), encoding="utf-8")
    try:
        q.load_steps(bad, "quick")
        raise AssertionError("Unauthorized executables should have failed")
    except ValueError:
        pass

print("QUALITY_RUNNER_TESTS_PASS")

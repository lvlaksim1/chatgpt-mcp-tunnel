#!/usr/bin/env python3
"""Local-only, evidence-preserving quality gate for ChatGPT MCP Tunnel.

Only executes commands explicitly listed in quality/standard.json, never
publishes/releases/installs software and never reports live acceptance.
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import os
from pathlib import Path
import subprocess
import sys
import time
from typing import Any

PROJECT_ROOT = Path(__file__).resolve().parents[1]
CONFIG = PROJECT_ROOT / "quality" / "standard.json"
ACCEPTED_KINDS = {"offline", "build", "sandbox", "mock"}
ALLOWED_PROGRAMS = {"dotnet", "python", "node"}
MAX_CAPTURE = 12000


def git_read(root: Path, *args: str) -> str | None:
    try:
        result = subprocess.run(
            ["git", "-C", str(root), *args], capture_output=True,
            text=True, encoding="utf-8", errors="replace", timeout=12, check=False
        )
        return result.stdout.strip() if result.returncode == 0 else None
    except (OSError, subprocess.TimeoutExpired):
        return None


def load_steps(config: Path, profile: str) -> tuple[dict[str, Any], list[dict[str, Any]]]:
    data = json.loads(config.read_text(encoding="utf-8"))
    if data.get("schema") != "engineering-quality-pilot-v1":
        raise ValueError("Unknown quality manifest schema")
    steps = list(data["quick"])
    if profile == "full":
        steps += data["full_additional"]
    ids: set[str] = set()
    for item in steps:
        name = item.get("id")
        cmd = item.get("command")
        if not isinstance(name, str) or not name or name in ids:
            raise ValueError("Invalid or duplicate step id")
        ids.add(name)
        if (item.get("kind") not in ACCEPTED_KINDS
                or not isinstance(cmd, list) or not cmd
                or not all(isinstance(s, str) and s for s in cmd)
                or cmd[0].lower() not in ALLOWED_PROGRAMS
                or not isinstance(item.get("timeout_sec"), int)
                or not 1 <= item["timeout_sec"] <= 600):
            raise ValueError("Invalid quality step: " + name)
    return data, steps


def execute_steps(steps: list[dict[str, Any]], root: Path) -> list[dict[str, Any]]:
    """Fail closed on first failure; never pretend skipped steps passed."""
    results: list[dict[str, Any]] = []
    aborted = False
    for item in steps:
        result: dict[str, Any] = {
            "id": item["id"], "kind": item["kind"],
            "command": item["command"], "status": "NOT_RUN",
            "exit_code": None, "elapsed_seconds": 0, "stdout_tail": "", "stderr_tail": ""
        }
        if aborted:
            result["reason"] = "Skipped after failed prerequisite"
            results.append(result)
            continue
        start = time.monotonic()
        try:
            child = subprocess.run(
                item["command"], cwd=root, capture_output=True,
                text=True, encoding="utf-8", errors="replace",
                timeout=item["timeout_sec"], check=False,
                env={**os.environ, "PYTHONIOENCODING": "utf-8"}
            )
            result["exit_code"] = child.returncode
            result["status"] = "PASS" if child.returncode == 0 else "FAIL"
            result["stdout_tail"] = child.stdout[-MAX_CAPTURE:]
            result["stderr_tail"] = child.stderr[-MAX_CAPTURE:]
        except subprocess.TimeoutExpired as exc:
            result["status"] = "TIMEOUT"
            result["stdout_tail"] = _as_text(exc.stdout)[-MAX_CAPTURE:]
            result["stderr_tail"] = _as_text(exc.stderr)[-MAX_CAPTURE:]
        except OSError as exc:
            result["status"] = "FAIL"
            result["stderr_tail"] = str(exc)
        result["elapsed_seconds"] = round(time.monotonic() - start, 2)
        results.append(result)
        if result["status"] != "PASS":
            aborted = True
    return results


def _as_text(value: str | bytes | None) -> str:
    if isinstance(value, bytes):
        return value.decode("utf-8", errors="replace")
    return value or ""


def create_report(profile: str, root: Path, results: list[dict[str, Any]]) -> dict[str, Any]:
    all_passed = bool(results) and all(item["status"] == "PASS" for item in results)
    return {
        "schema": "engineering-quality-report-v1",
        "created_utc": dt.datetime.now(dt.timezone.utc).isoformat(),
        "project": "lvlaksim1/chatgpt-mcp-tunnel",
        "profile": profile,
        "workspace": str(root),
        "commit": git_read(root, "rev-parse", "HEAD"),
        "branch": git_read(root, "branch", "--show-current"),
        "source_dirty": bool(git_read(root, "status", "--porcelain")),
        "offline_gate": "PASS" if all_passed else "FAIL",
        "live_chat_e2e": "NOT_TESTED",
        "installed_application": "NOT_TESTED",
        "owner_acceptance": "NOT_TESTED",
        "release_authorized": False,
        "steps": results,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Offline quality gate; NOT a live acceptance test")
    parser.add_argument("--profile", choices=("quick", "full"), default="quick")
    parser.add_argument("--plan", action="store_true", help="List checks without executing")
    args = parser.parse_args()
    try:
        config, steps = load_steps(CONFIG, args.profile)
        if config.get("project") != "lvlaksim1/chatgpt-mcp-tunnel":
            raise ValueError("Wrong project configured")
        if config.get("policy", {}).get("runtime_acceptance") != "NOT_TESTED":
            raise ValueError("Runtime acceptance must remain separate")
    except (ValueError, OSError, KeyError, json.JSONDecodeError) as exc:
        print("QUALITY_CONFIGURATION_INVALID: " + str(exc), file=sys.stderr)
        return 2
    if args.plan:
        print(json.dumps({"profile": args.profile, "steps": [
            {"id": x["id"], "kind": x["kind"], "command": x["command"]} for x in steps
        ], "runtime": "NOT_TESTED"}, ensure_ascii=False, indent=2))
        return 0
    results = execute_steps(steps, PROJECT_ROOT)
    report = create_report(args.profile, PROJECT_ROOT, results)
    reports_dir = PROJECT_ROOT / ".tools" / "quality-reports"
    reports_dir.mkdir(parents=True, exist_ok=True)
    stamp = dt.datetime.now(dt.timezone.utc).strftime("%Y%m%dT%H%M%S%fZ")
    report_file = reports_dir / ("quality-" + args.profile + "-" + stamp + ".json")
    report_file.write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
    for item in results:
        print(item["status"].ljust(8) + " " + item["id"] + " (" + str(item["elapsed_seconds"]) + "s)")
    print("OFFLINE_GATE=" + report["offline_gate"])
    print("LIVE_CHAT_E2E=NOT_TESTED INSTALLED=NOT_TESTED OWNER_ACCEPTANCE=NOT_TESTED")
    print("REPORT=" + str(report_file))
    return 0 if report["offline_gate"] == "PASS" else 1


if __name__ == "__main__":
    sys.exit(main())

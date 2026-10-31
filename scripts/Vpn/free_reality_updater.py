"""Periodic isolated free VLESS/TCP/REALITY discovery for ChatGPT MCP Tunnel.

Never changes the Windows proxy, Happ, the running MCP transport, or WebView2.
Only writes DPAPI-protected user-local profiles after a real outbound HTTPS check.
Python standard library only, tested on Windows 10 with Xray-core.
"""
from __future__ import annotations
import argparse
import base64
import ctypes
import datetime as dt
import hashlib
import ipaddress
import json
import os
from pathlib import Path
import re
import socket
import subprocess
import sys
import tempfile
import time
import urllib.parse
import urllib.request
import uuid

ROOT = Path(__file__).resolve().parents[2]
SOURCES_PATH = Path(__file__).with_name("free_reality_sources.json")
ALLOWED_SOURCE_HOSTS = {"raw.githubusercontent.com"}
USER_AGENT = "ChatGPT-MCP-Tunnel-VPN-Refresh/1.0"
MAX_BYTES = 3_000_000
LOOPBACK = "127.0.0.1"
DEFAULT_PORT = 11836


def now() -> str:
    return dt.datetime.now(dt.timezone.utc).isoformat(timespec="seconds")


def atomic_write(path: Path, data: bytes) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_name(path.name + "." + uuid.uuid4().hex + ".tmp")
    try:
        with open(tmp, "wb") as fp:
            fp.write(data)
            fp.flush()
            os.fsync(fp.fileno())
        os.replace(tmp, path)
    finally:
        tmp.unlink(missing_ok=True)


def read_json(path: Path, default):
    try:
        return json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, ValueError, UnicodeError):
        return default


def dpapi_protect(data: bytes) -> bytes:
    """Windows current-user DPAPI, no keys in repo, command line, or console."""
    if os.name != "nt":
        raise RuntimeError("Windows DPAPI required")
    from ctypes import wintypes

    class Blob(ctypes.Structure):
        _fields_ = [("cbData", wintypes.DWORD),
                    ("pbData", ctypes.POINTER(ctypes.c_ubyte))]

    buffer = ctypes.create_string_buffer(data)
    inbound = Blob(len(data), ctypes.cast(buffer, ctypes.POINTER(ctypes.c_ubyte)))
    outbound = Blob()
    crypt = ctypes.WinDLL("crypt32", use_last_error=True)
    kernel = ctypes.WinDLL("kernel32", use_last_error=True)
    crypt.CryptProtectData.argtypes = [
        ctypes.POINTER(Blob), wintypes.LPCWSTR, ctypes.c_void_p,
        ctypes.c_void_p, ctypes.c_void_p, wintypes.DWORD, ctypes.POINTER(Blob)]
    crypt.CryptProtectData.restype = wintypes.BOOL
    kernel.LocalFree.argtypes = [ctypes.c_void_p]
    kernel.LocalFree.restype = ctypes.c_void_p
    if not crypt.CryptProtectData(ctypes.byref(inbound), None, None, None,
                                  None, 0x01, ctypes.byref(outbound)):
        raise ctypes.WinError(ctypes.get_last_error())
    try:
        return ctypes.string_at(outbound.pbData, outbound.cbData)
    finally:
        kernel.LocalFree(ctypes.cast(outbound.pbData, ctypes.c_void_p))


def parse_node(line: str, source: str):
    raw = line.strip()
    if not raw.startswith("vless://") or len(raw) > 8192:
        return None
    try:
        uri = urllib.parse.urlsplit(raw)
        q = urllib.parse.parse_qs(uri.query, keep_blank_values=True)
        one = lambda k, default="": q.get(k, [default])[0]
        if one("security").lower() != "reality":
            return None
        if one("type", "tcp").lower() not in ("tcp", "raw"):
            return None
        if one("encryption", "none").lower() != "none":
            return None
        if one("flow") not in ("", "xtls-rprx-vision"):
            return None
        if uri.password or not uri.hostname or uri.port is None:
            return None
        host = uri.hostname
        if host.lower() == "localhost":
            return None
        try:
            address = ipaddress.ip_address(host)
            if not address.is_global:
                return None
        except ValueError:
            if host.lower().endswith((".local", ".internal", ".localhost")):
                return None
        if not 1 <= uri.port <= 65535:
            return None
        user_id = str(uuid.UUID(urllib.parse.unquote(uri.username or "")))
        pbk = one("pbk")
        if not re.fullmatch(r"[A-Za-z0-9_-]{43,44}", pbk):
            return None
        sni = one("sni")
        if not sni or len(sni) > 253:
            return None
        sid = one("sid")
        if sid and (len(sid) > 16 or len(sid) % 2 or not re.fullmatch("[a-fA-F0-9]+", sid)):
            return None
        fp = one("fp", "chrome")
        if fp not in ("chrome", "firefox", "safari", "edge", "random", "randomized", "ios", "android"):
            return None
        fingerprint = hashlib.sha256(raw.split("#", 1)[0].encode("utf-8")).hexdigest()
        return {
            "id": fingerprint[:20], "source": source, "host": host,
            "port": uri.port, "raw": raw, "uuid": user_id,
            "pbk": pbk, "sni": sni, "sid": sid, "fp": fp,
            "flow": one("flow"), "spx": one("spx"),
            "label": urllib.parse.unquote(uri.fragment)[:70],
        }
    except (ValueError, UnicodeError):
        return None


def parse_feed(raw: bytes, name: str):
    s = raw.decode("utf-8-sig", errors="replace")
    if not s.lstrip().startswith("vless://") and "vless://" not in s[:1024]:
        try:
            decoded = base64.b64decode(b"".join(raw.split()), validate=True)
            s = decoded.decode("utf-8-sig")
        except (ValueError, UnicodeError):
            pass
    found = {}
    for line in s.splitlines():
        n = parse_node(line.strip(), name)
        if n is not None:
            found.setdefault(n["id"], n)
    return list(found.values())


def fetch_source(source: dict):
    url = source["url"]
    parts = urllib.parse.urlsplit(url)
    if parts.scheme != "https" or parts.hostname not in ALLOWED_SOURCE_HOSTS:
        raise ValueError("Source must use approved HTTPS host")
    request = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
    with urllib.request.urlopen(request, timeout=16) as response:
        final = urllib.parse.urlsplit(response.geturl())
        if final.scheme != "https" or final.hostname not in ALLOWED_SOURCE_HOSTS:
            raise ValueError("Source redirected away from approved HTTPS hosts")
        raw = response.read(MAX_BYTES + 1)
    if len(raw) > MAX_BYTES:
        raise ValueError("Source exceeds size limit")
    return parse_feed(raw, source["name"])


def xray_config(node: dict, port: int):
    user = {"id": node["uuid"], "encryption": "none"}
    if node["flow"]:
        user["flow"] = node["flow"]
    reality = {
        "serverName": node["sni"], "fingerprint": node["fp"],
        "password": node["pbk"], "shortId": node["sid"],
        "spiderX": node["spx"],
    }
    return {
        "log": {"loglevel": "warning"},
        "inbounds": [{
            "tag": "private-http", "listen": LOOPBACK, "port": port,
            "protocol": "http", "settings": {},
        }],
        "outbounds": [{
            "tag": "free-reality", "protocol": "vless",
            "settings": {"vnext": [{
                "address": node["host"], "port": node["port"],
                "users": [user],
            }]},
            "streamSettings": {
                "network": "tcp", "security": "reality",
                "realitySettings": reality,
            },
        }],
    }


def port_open(port: int) -> bool:
    try:
        with socket.create_connection((LOOPBACK, port), timeout=0.25):
            return True
    except (OSError, TimeoutError):
        return False


def probe(node: dict, exe: str, port: int, timeout: int):
    """No system proxy configuration, no unrelated process termination."""
    if port_open(port):
        raise RuntimeError("Diagnostic proxy port in use; refusing to disrupt owner")
    started = time.monotonic()
    with tempfile.TemporaryDirectory(prefix="mcp-free-vpn-") as temp:
        path = Path(temp) / "node.json"
        path.write_text(json.dumps(xray_config(node, port), ensure_ascii=False), encoding="utf-8")
        try:
            check = subprocess.run(
                [exe, "run", "-test", "-config", str(path)],
                stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=5)
            if check.returncode:
                return None
            flags = getattr(subprocess, "CREATE_NO_WINDOW", 0)
            proc = subprocess.Popen(
                [exe, "run", "-config", str(path)],
                cwd=str(Path(exe).parent),
                stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
                creationflags=flags,
            )
            try:
                for _ in range(25):
                    if proc.poll() is not None:
                        return None
                    if port_open(port):
                        break
                    time.sleep(0.10)
                if not port_open(port):
                    return None
                # HTTP CONNECT is mandatory: do not allow environment-proxy
                # overrides or a silent direct fallback.
                cmd = [
                    "curl.exe", "--disable",
                    "--proxy", f"http://{LOOPBACK}:{port}",
                    "--noproxy", "", "--silent", "--show-error",
                    "--connect-timeout", str(max(3, timeout - 3)),
                    "--max-time", str(timeout),
                    "-o", "NUL", "-w", "%{http_code}",
                    "https://example.com/",
                ]
                result = subprocess.run(
                    cmd, capture_output=True, timeout=timeout + 3)
                ok = result.returncode == 0 and result.stdout.strip() == b"200"
                return round((time.monotonic() - started) * 1000) if ok else None
            finally:
                if proc.poll() is None:
                    proc.terminate()
                    try:
                        proc.wait(timeout=2)
                    except subprocess.TimeoutExpired:
                        proc.kill()
                        proc.wait(timeout=2)
        except (OSError, subprocess.TimeoutExpired):
            return None


def public_metadata(n: dict):
    return {
        "id": n["id"], "source": n["source"], "host": n["host"],
        "port": n["port"],
        # The user-facing label is not authoritative geolocation.
        "label": n["label"], "protocol": "VLESS/TCP/REALITY",
    }


def select_candidates(nodes: list, previous_id: str, limit: int):
    # Keep the formerly working node first, then alternate sources.
    previous = next((n for n in nodes if n["id"] == previous_id), None)
    groups = {}
    for n in nodes:
        groups.setdefault(n["source"], []).append(n)
    result, used = [], set()
    if previous is not None:
        result.append(previous)
        used.add(previous["id"])
    while len(result) < limit:
        progressed = False
        for source, members in groups.items():
            while members and members[0]["id"] in used:
                members.pop(0)
            if members and len(result) < limit:
                n = members.pop(0)
                result.append(n)
                used.add(n["id"])
                progressed = True
        if not progressed:
            break
    return result


def state_dir_default() -> Path:
    return Path(os.environ.get("LOCALAPPDATA", str(Path.home()))) / "ChatGPTMCPAgent" / "Vpn" / "profiles"


def verified_engine(binary: Path | None = None, hash_file: Path | None = None) -> str:
    # Installed CLIENT path is explicit; developer pilot path remains supported.
    # Never search for an unverified engine or fall back to a global xray.exe.
    if (binary is None) != (hash_file is None):
        raise RuntimeError("Both --xray-binary and --xray-sha256-file are required")
    if binary is None:
        manifest = read_json(ROOT / ".tools/vpn-pilot/binary-manifest.json", {})
        exe = Path(manifest.get("xray_exe", ""))
        expected = manifest.get("binary_sha256", "")
    else:
        exe = binary
        if not hash_file.is_file():
            raise RuntimeError("Installed Xray sha256 file is missing")
        expected = hash_file.read_text(encoding="ascii").strip()
    if not exe.is_file():
        raise RuntimeError("Xray-core not installed or missing binary-manifest")
    if not re.fullmatch(r"[0-9a-fA-F]{64}", expected):
        raise RuntimeError("Invalid Xray SHA256 reference")
    got = hashlib.sha256(exe.read_bytes()).hexdigest()
    if got.lower() != expected.lower():
        raise RuntimeError("Xray-core SHA256 differs from verified binary")
    return str(exe)


def run_refresh(sources: list, state_dir: Path, exe: str,
                max_probes: int, timeout: int, port: int, test_probe=probe,
                download=fetch_source):
    state_dir.mkdir(parents=True, exist_ok=True)
    snapshot = state_dir / "status.json"
    old = read_json(snapshot, {})
    source_status, pool = [], []
    seen = set()
    for src in sources:
        try:
            candidates = download(src)
            count = 0
            for candidate in candidates:
                if candidate["id"] not in seen:
                    seen.add(candidate["id"])
                    pool.append(candidate)
                    count += 1
            source_status.append({"name": src["name"], "ok": True,
                                  "total": len(candidates), "added": count})
        except (OSError, ValueError, TimeoutError) as e:
            source_status.append({"name": src["name"], "ok": False,
                                  "errorType": type(e).__name__})
    old_id = old.get("selected", {}).get("id", "")
    candidates = select_candidates(pool, old_id, max_probes)
    checked, successes = [], []
    for node in candidates:
        latency = test_probe(node, exe, port, timeout)
        record = {**public_metadata(node), "latencyMs": latency,
                  "ok": latency is not None}
        checked.append(record)
        if latency is not None:
            successes.append((latency, node))
            # One active + up to two tested standby nodes. Skip unverified
            # candidates; do not blindly trust feed metadata.
            if len(successes) >= 3:
                break
    # Preserve the already-working choice if it is still demonstrably alive.
    if successes:
        existing = next((entry for entry in successes if entry[1]["id"] == old_id), None)
        latency, winner = existing or min(successes, key=lambda entry: entry[0])
        protected = dpapi_protect(
            (json.dumps(xray_config(winner, 11821), ensure_ascii=False)
             + "\n").encode("utf-8"))
        atomic_write(state_dir / "Free-Reality-Auto.dpapi", protected)
        profile_meta = {"name": "Free-Reality-Auto", "slug": "Free-Reality-Auto",
                        "proxyPort": 11821, "protocol": "VLESS/TCP/REALITY",
                        "encryption": "DPAPI CurrentUser", "updatedAt": now()}
        atomic_write(state_dir / "Free-Reality-Auto.meta.json",
                     json.dumps(profile_meta, ensure_ascii=False, indent=2).encode("utf-8"))
        # Keep separately encrypted, already-tested backup nodes. Credentials
        # never appear in catalog/status/Git and are not printed by the task.
        standbys = []
        for slot, (backup_latency, backup_node) in enumerate(
                [entry for entry in successes if entry[1]["id"] != winner["id"]][:2], 1):
            backup_config = (json.dumps(xray_config(backup_node, 11821),
                                        ensure_ascii=False) + "\n").encode("utf-8")
            atomic_write(state_dir / f"Free-Reality-Standby-{slot}.dpapi",
                         dpapi_protect(backup_config))
            standbys.append({**public_metadata(backup_node),
                             "latencyMs": backup_latency, "verifiedAt": now()})
        selected = {**public_metadata(winner), "latencyMs": latency,
                    "verifiedAt": now()}
    else:
        selected = old.get("selected")
        standbys = old.get("standbys", [])
    # A refresh with only one healthy candidate preserves a previously tested
    # encrypted standby, but marks it stale until the bridge revalidates it.
    if successes and not standbys:
        standbys = old.get("standbys", [])
    # Do not replace existing catalog with zero nodes on provider failure.
    if pool:
        catalog = [public_metadata(n) for n in pool]
        atomic_write(state_dir / "catalog.json",
                     json.dumps(catalog, ensure_ascii=False, indent=2).encode("utf-8"))
    report = {
        "updatedAt": now(), "sources": source_status,
        "availableCandidates": len(pool), "probed": len(checked),
        "healthy": len(successes), "checked": checked,
        "selected": selected, "standbys": standbys, "updatedSelection": bool(successes),
        "status": "updated" if successes else
                  ("kept-last-good" if selected else "no-working-node"),
        "productionRoutingChanged": False,
    }
    atomic_write(snapshot, json.dumps(report, ensure_ascii=False, indent=2).encode("utf-8"))
    return report


def main():
    p = argparse.ArgumentParser(description="Refresh public free VLESS/REALITY nodes")
    p.add_argument("--state-dir", type=Path, default=state_dir_default())
    p.add_argument("--sources", type=Path, default=SOURCES_PATH)
    p.add_argument("--max-probes", type=int, default=16)
    p.add_argument("--timeout", type=int, default=9)
    p.add_argument("--port", type=int, default=DEFAULT_PORT)
    p.add_argument("--xray-binary", type=Path, default=None,
                   help="installed client verified Xray binary")
    p.add_argument("--xray-sha256-file", type=Path, default=None,
                   help="SHA256 reference for the installed client Xray")
    args = p.parse_args()
    if not 1025 <= args.port <= 65535 or args.port in (10808, 10809):
        p.error("Invalid diagnostic port")
    if not 1 <= args.max_probes <= 100 or not 4 <= args.timeout <= 30:
        p.error("Invalid test limits")
    sources = json.loads(args.sources.read_text(encoding="utf-8-sig"))["sources"]
    if not isinstance(sources, list) or len(sources) > 8:
        p.error("Invalid source count")
    state = args.state_dir
    state.mkdir(parents=True, exist_ok=True)
    lock = state / "refresh.lock"
    try:
        fd = os.open(lock, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
    except FileExistsError:
        if time.time() - lock.stat().st_mtime < 1800:
            print("REFRESH_ALREADY_RUNNING")
            return 0
        lock.unlink()
        fd = os.open(lock, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
    try:
        with os.fdopen(fd, "w") as f:
            f.write(f"{os.getpid()}\n")
        exe = verified_engine(args.xray_binary, args.xray_sha256_file)
        report = run_refresh(sources, state, exe, args.max_probes, args.timeout, args.port)
        print(json.dumps({k: report[k] for k in
              ("updatedAt", "availableCandidates", "probed", "healthy",
               "selected", "status", "productionRoutingChanged")},
              ensure_ascii=True))
        return 0 if report["selected"] else 2
    finally:
        lock.unlink(missing_ok=True)


if __name__ == "__main__":
    sys.exit(main())

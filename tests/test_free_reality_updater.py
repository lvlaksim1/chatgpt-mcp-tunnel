"""Offline regression for automatic free REALITY source refresh."""
import base64
import importlib.util
import json
import os
from pathlib import Path
import tempfile
import unittest
import uuid

MODULE = Path(__file__).resolve().parents[1] / "scripts" / "Vpn" / "free_reality_updater.py"
spec = importlib.util.spec_from_file_location("free_reality_updater", MODULE)
vpn = importlib.util.module_from_spec(spec)
spec.loader.exec_module(vpn)
PUBKEY = base64.urlsafe_b64encode(b"\x08" * 32).decode().rstrip("=")


def uri(host="8.8.8.8", pk=PUBKEY, fragment="test"):
    return (f"vless://{uuid.UUID(int=42)}@{host}:443?"
            f"security=reality&type=tcp&pbk={pk}&sni=example.com"
            "&fp=chrome&sid=abcdef&flow=xtls-rprx-vision#"+fragment)


class RefreshTests(unittest.TestCase):
    def test_feed_filtering(self):
        feed = (uri() + "\nvmess://junk\n" +
                uri(host="127.0.0.1") + "\n" +
                uri(host="8.8.8.8") + "\n" +
                uri(host="1.1.1.1").replace("security=reality", "security=tls") + "\n")
        items = vpn.parse_feed(feed.encode(), "fixture")
        self.assertEqual(len(items), 1)
        self.assertEqual(items[0]["host"], "8.8.8.8")
        self.assertEqual(items[0]["flow"], "xtls-rprx-vision")

    def test_base64_feed(self):
        data = base64.b64encode((uri() + "\n").encode())
        self.assertEqual(len(vpn.parse_feed(data, "fixture")), 1)

    def test_private_addresses_rejected(self):
        for host in ["127.0.0.1", "10.0.0.1", "192.168.1.2",
                     "169.254.1.1", "localhost", "vault.local"]:
            self.assertIsNone(vpn.parse_node(uri(host=host), "fake"))

    def test_build_real_xray_config(self):
        node = vpn.parse_node(uri(host="8.8.8.8"), "fake")
        built = vpn.xray_config(node, 11836)
        self.assertEqual(built["inbounds"][0]["listen"], "127.0.0.1")
        self.assertEqual(built["outbounds"][0]["streamSettings"]["security"], "reality")
        self.assertEqual(built["outbounds"][0]["streamSettings"]["realitySettings"]["password"], PUBKEY)

    def test_dedup_and_last_good_survives_provider_outage(self):
        node_a = vpn.parse_node(uri(host="8.8.8.8"), "first")
        node_b = vpn.parse_node(uri(host="1.1.1.1"), "second")
        src = [{"name": "first", "url": "https://raw.githubusercontent.com/demo/main/first"},
               {"name": "second", "url": "https://raw.githubusercontent.com/demo/main/second"}]
        def ok_fetch(s):
            return [node_a] if s["name"]=="first" else [node_b]
        # Use actual Windows DPAPI, not a stub.
        with tempfile.TemporaryDirectory() as d:
            state = Path(d)
            report = vpn.run_refresh(src, state, "test-xray.exe", 2, 4, 11836,
                test_probe=lambda n,exe,port,timeout: 100 if n["id"] == node_a["id"] else 180,
                download=ok_fetch)
            self.assertEqual(report["status"], "updated")
            self.assertEqual(report["healthy"], 2)
            self.assertEqual(report["selected"]["id"], node_a["id"])
            self.assertEqual(len(report["standbys"]), 1)
            self.assertEqual(report["standbys"][0]["id"], node_b["id"])
            secondary = (state/"Free-Reality-Standby-1.dpapi").read_bytes()
            self.assertNotIn(node_b["uuid"].encode(), secondary)
            self.assertNotIn(PUBKEY.encode(), secondary)
            self.assertEqual(secondary[:1] != b"{", True)
            encrypted = (state/"Free-Reality-Auto.dpapi").read_bytes()
            self.assertNotIn(node_a["uuid"].encode(), encrypted)
            self.assertNotIn(PUBKEY.encode(), encrypted)
            self.assertNotIn("vless://", (state/"status.json").read_text(encoding="utf-8"))
            self.assertNotIn("vless://", (state/"catalog.json").read_text(encoding="utf-8"))
            def bad_fetch(s):
                raise OSError("offline")
            result = vpn.run_refresh(src, state, "test-xray.exe", 2, 4, 11836,
                        test_probe=lambda *args:None, download=bad_fetch)
            self.assertEqual(result["status"], "kept-last-good")
            self.assertEqual((state/"Free-Reality-Auto.dpapi").read_bytes(), encrypted)
            self.assertEqual((state/"Free-Reality-Standby-1.dpapi").read_bytes(), secondary)
            self.assertEqual(len(result["standbys"]), 1)
            self.assertEqual(json.loads((state/"catalog.json").read_text(encoding="utf-8"))[0]["host"], "8.8.8.8")

    def test_known_good_prioritized(self):
        one = vpn.parse_node(uri(host="8.8.8.8"), "a")
        two = vpn.parse_node(uri(host="1.1.1.1"), "b")
        result = vpn.select_candidates([one,two],two["id"],2)
        self.assertEqual(result[0]["id"], two["id"])


if __name__ == "__main__":
    unittest.main(verbosity=2)

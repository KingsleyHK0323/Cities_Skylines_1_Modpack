"""Offline negative contracts. Signature: OpenAI-Codex."""
import io
import json
import unittest
import zipfile
from unittest.mock import patch

import bundle


class BundleContracts(unittest.TestCase):
    def zip_bytes(self, names):
        data = io.BytesIO()
        with zipfile.ZipFile(data, "w") as archive:
            for name in names:
                archive.writestr(name, b"payload")
        return data.getvalue()

    def test_zip_accepts_exact_allowlist(self):
        self.assertEqual(bundle.strict_zip(self.zip_bytes(["a.dll"]), ["a.dll"]),
                         {"a.dll": b"payload"})

    def test_zip_rejects_traversal_extra_and_missing_files(self):
        for names in (["../a.dll"], ["a.dll", "UnityEngine.dll"], []):
            with self.subTest(names=names), self.assertRaises(ValueError):
                bundle.strict_zip(self.zip_bytes(names), ["a.dll"])

    def vehicle_payload(self):
        spec = bundle.MODULES[2]
        dll = b"MZmodule"
        info = {"commit": "a" * 40, "runId": "12", "attempt": "2",
                "assembly": "AutomatedPublicTransportVehicleManagement",
                "gameplayVerified": False, "sha256": bundle.digest(dll)}
        return spec, {spec[2]: dll, "build-provenance.json": json.dumps(info).encode()}

    def test_vehicle_hash_and_attempt_are_required(self):
        spec, files = self.vehicle_payload()
        run = {"id": 12, "run_attempt": 2}
        bundle.verify_files(files, spec, "a" * 40, run)
        with self.assertRaises(ValueError):
            bundle.verify_files(files, spec, "a" * 40, {"id": 12, "run_attempt": 3})
        files[spec[2]] += b"tampered"
        with self.assertRaises(ValueError):
            bundle.verify_files(files, spec, "a" * 40, run)

    def test_expired_artifact_never_falls_back(self):
        with patch.object(bundle, "api", return_value={"artifacts": [
                {"name": "vehicle-management-12-2", "expired": True}]}), \
                self.assertRaises(ValueError):
            bundle.vehicle_files({"id": 12, "run_attempt": 2}, bundle.MODULES[2][3])

    def test_missing_dependency_rejected(self):
        spec = bundle.MODULES[0]
        with self.assertRaises(ValueError):
            bundle.verify_files({"AIImprove.dll": b"MZonly"}, spec, "a" * 40,
                                {"id": 12, "run_attempt": 2})

    def test_unexpected_game_reference_rejected(self):
        spec, files = self.vehicle_payload()
        files["ICities.dll"] = b"MZreference"
        with self.assertRaises(ValueError):
            bundle.verify_files(files, spec, "a" * 40, {"id": 12, "run_attempt": 2})


if __name__ == "__main__":
    unittest.main()

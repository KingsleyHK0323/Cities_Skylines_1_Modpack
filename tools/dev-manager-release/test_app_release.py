"""Offline contracts for the Dev Manager-only Release publisher.

Signature: OpenAI-Codex
"""

import hashlib
import io
import json
import copy
import unittest
import zipfile
from unittest import mock

import app_release


SOURCE = "a" * 40
SETUP = b"MZ synthetic Windows installer"


def package(extra=None):
    files = {
        "DevManager.exe": b"MZ synthetic executable",
        "runtime/node.exe": b"MZ synthetic runtime",
        "runtime/LICENSE_NODE.txt": b"synthetic runtime license",
        "THIRD-PARTY.md": b"third party notices",
        "README.md": b"readme", "USER-GUIDE.md": b"guide", "LICENSE": b"license",
        "ARTIFACT-CONTRACT.md": b"contract", "artifact-source.mjs": b"source",
        "desktop-backend.mjs": b"synthetic backend", "desktop-service.mjs": b"service",
        "registry.mjs": b"registry",
    }
    additional_sources = (
        "core.mjs", "filesystem.mjs", "github-source.mjs", "download.mjs", "https-source.mjs",
        "module-policy.mjs", "release-index.mjs", "bundle-source.mjs", "update-order.mjs",
        "asset-policy.mjs", "PUBLISHER-CONTRACT.md", "SOURCE-CONTRACT.md", "Change Log.md",
        "preferences.mjs", "localization.mjs", "translations.json", "settings-store.mjs", "TEST-READINESS.md",
    )
    files.update({name: ("fixture " + name).encode() for name in additional_sources})
    manifest = {
        "schema": 1, "runtime": "v24.22.2",
        "signature": "OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager",
        "runtimeSHA256": hashlib.sha256(files["runtime/node.exe"]).hexdigest(),
        "nativeSHA256": hashlib.sha256(files["DevManager.exe"]).hexdigest(),
        "files": {name: hashlib.sha256(files[name]).hexdigest()
                  for name in ("README.md", "USER-GUIDE.md", "LICENSE", "ARTIFACT-CONTRACT.md",
                               "artifact-source.mjs", "desktop-backend.mjs", "desktop-service.mjs", "registry.mjs")},
    }
    manifest["files"].update({name: hashlib.sha256(files[name]).hexdigest() for name in additional_sources})
    files["package-manifest.json"] = json.dumps(manifest).encode()
    if extra:
        files.update(extra)
    stream = io.BytesIO()
    with zipfile.ZipFile(stream, "w", zipfile.ZIP_DEFLATED) as archive:
        for name, data in files.items():
            archive.writestr(name, data)
    return stream.getvalue(), hashlib.sha256(files["runtime/node.exe"]).hexdigest(), hashlib.sha256(files["runtime/LICENSE_NODE.txt"]).hexdigest()


def rewrite_declared_files(data, *, add=None, remove=()):
    """Construct a self-consistent package with an altered declared file set."""
    with zipfile.ZipFile(io.BytesIO(data)) as archive:
        files = {item.filename: archive.read(item) for item in archive.infolist()}
    manifest = json.loads(files["package-manifest.json"])
    for name in remove:
        files.pop(name, None)
        manifest["files"].pop(name, None)
    for name, contents in (add or {}).items():
        files[name] = contents
        manifest["files"][name] = hashlib.sha256(contents).hexdigest()
    files["package-manifest.json"] = json.dumps(manifest).encode()
    output = io.BytesIO()
    with zipfile.ZipFile(output, "w", zipfile.ZIP_DEFLATED) as archive:
        for name, contents in files.items():
            archive.writestr(name, contents)
    return output.getvalue()


def context():
    return {"repository": app_release.REPOSITORY, "sourceSha": SOURCE,
            "version": "v0.1.0-beta.1", "runId": 101, "attempt": 1}


def installer_asset(data=SETUP):
    return {"name": app_release.ASSET_NAME, "state": "uploaded",
            "size": len(data), "digest": "sha256:" + hashlib.sha256(data).hexdigest()}


class FakeGateway:
    def __init__(self, releases=None, head=SOURCE):
        self.releases = releases or []
        self.head = head
        self.calls = []

    def source_head(self):
        return self.head

    def list_releases(self):
        return list(self.releases)

    def tag_commit(self, tag):
        found = next((r for r in self.releases if r["tag_name"] == tag), None)
        return found.get("tag_commit") if found else None

    def create_release(self, body):
        self.calls.append(("create", body))
        release = {**body, "id": 101, "assets": [], "tag_commit": None}
        self.releases.append(release)
        return release

    def upload(self, release_id, data):
        self.calls.append(("upload", release_id))
        release = next(r for r in self.releases if r["id"] == release_id)
        release["assets"].append(installer_asset(data))

    def release(self, release_id):
        return next(r for r in self.releases if r["id"] == release_id)

    def publish_release(self, release_id, title):
        self.calls.append(("publish", release_id))
        release = self.release(release_id)
        release["draft"] = False
        release["name"] = title
        release["published_at"] = "2026-11-03T00:00:00Z"
        release["tag_commit"] = release["target_commitish"]

    def rename(self, release_id, title):
        self.calls.append(("rename", release_id, title))
        self.release(release_id)["name"] = title


class NoticeFixtureMixin:
    def setUp(self):
        super().setUp()
        patcher = mock.patch.object(app_release, "THIRD_PARTY_NOTICE_SHA", hashlib.sha256(b"third party notices").hexdigest())
        patcher.start()
        self.addCleanup(patcher.stop)


class PackageTests(NoticeFixtureMixin, unittest.TestCase):
    def test_unapproved_empty_archive_directory_is_rejected(self):
        data, runtime, license_hash = package()
        output = io.BytesIO(data)
        with zipfile.ZipFile(output, "a") as archive:
            archive.writestr("private-folder/", b"")
        with self.assertRaises(app_release.PublicationError):
            app_release.verify_package(output.getvalue(), runtime_sha=runtime, license_sha=license_hash)

    def test_unapproved_manifest_metadata_is_rejected(self):
        data, runtime, license_hash = package()
        with zipfile.ZipFile(io.BytesIO(data)) as archive:
            files = {item.filename: archive.read(item) for item in archive.infolist()}
        manifest = json.loads(files["package-manifest.json"])
        manifest["privatePath"] = "synthetic local path"
        files["package-manifest.json"] = json.dumps(manifest).encode()
        output = io.BytesIO()
        with zipfile.ZipFile(output, "w") as archive:
            for path, contents in files.items():
                archive.writestr(path, contents)
        with self.assertRaises(app_release.PublicationError):
            app_release.verify_package(output.getvalue(), runtime_sha=runtime, license_sha=license_hash)

    def test_changed_third_party_notice_is_rejected(self):
        data, runtime, license_hash = package({"THIRD-PARTY.md": b"unexpected private data"})
        with self.assertRaises(app_release.PublicationError):
            app_release.verify_package(data, runtime_sha=runtime, license_sha=license_hash)

    def test_third_party_notice_accepts_windows_line_endings(self):
        data, runtime, license_hash = package({"THIRD-PARTY.md": b"third party\r\nnotices"})
        digest = hashlib.sha256(b"third party\nnotices").hexdigest()
        with mock.patch.object(app_release, "THIRD_PARTY_NOTICE_SHA", digest):
            self.assertEqual(app_release.verify_package(data, runtime_sha=runtime, license_sha=license_hash),
                             hashlib.sha256(data).hexdigest())

    def test_declared_private_or_unapproved_files_are_rejected(self):
        data, runtime, license_hash = package()
        for name in (".desktop-state.json", "local-secrets.json", "unexpected.mjs"):
            altered = rewrite_declared_files(data, add={name: b"private or unapproved"})
            with self.subTest(name=name), self.assertRaises(app_release.PublicationError):
                app_release.verify_package(altered, runtime_sha=runtime, license_sha=license_hash)

    def test_missing_runtime_and_localization_files_are_rejected(self):
        data, runtime, license_hash = package()
        for name in ("core.mjs", "localization.mjs", "translations.json", "settings-store.mjs"):
            altered = rewrite_declared_files(data, remove=(name,))
            with self.subTest(name=name), self.assertRaises(app_release.PublicationError):
                app_release.verify_package(altered, runtime_sha=runtime, license_sha=license_hash)

    def test_build_identity_binds_the_installer_and_internal_payload(self):
        data, _, _ = package()
        identity = {**context(), "payloadSha256": hashlib.sha256(data).hexdigest(),
                    "setupSha256": hashlib.sha256(SETUP).hexdigest()}
        app_release.verify_build_identity(context(), identity, data, SETUP)
        for invalid in ({**identity, "payloadSha256": "0" * 64},
                        {**identity, "setupSha256": "0" * 64},
                        {**identity, "sourceSha": "b" * 40}):
            with self.subTest(invalid=invalid), self.assertRaises(app_release.PublicationError):
                app_release.verify_build_identity(context(), invalid, data, SETUP)

    def test_setup_must_be_a_bounded_windows_executable(self):
        self.assertEqual(app_release.verify_setup(SETUP), hashlib.sha256(SETUP).hexdigest())
        for setup in (b"", b"PK synthetic zip", b"not an executable"):
            with self.subTest(setup=setup), self.assertRaises(app_release.PublicationError):
                app_release.verify_setup(setup)

    def test_complete_portable_package_is_verified(self):
        data, runtime, license_hash = package()
        result = app_release.verify_package(data, runtime_sha=runtime, license_sha=license_hash)
        self.assertEqual(result, hashlib.sha256(data).hexdigest())

    def test_unsafe_extra_files_and_mismatched_runtime_are_rejected(self):
        for extra in ({"../escape.dll": b"x"}, {".desktop-state.json": b"private"},
                      {"runtime/node.exe": b"MZ changed"}):
            data, runtime, license_hash = package(extra)
            if "runtime/node.exe" in extra:
                runtime = hashlib.sha256(b"MZ synthetic runtime").hexdigest()
            with self.subTest(extra=extra), self.assertRaises(app_release.PublicationError):
                app_release.verify_package(data, runtime_sha=runtime, license_sha=license_hash)

    def test_windows_unsafe_manifest_files_are_rejected_even_if_hashes_match(self):
        data, runtime, license_hash = package()
        with zipfile.ZipFile(io.BytesIO(data)) as archive:
            files = {item.filename: archive.read(item) for item in archive.infolist()}
        manifest = json.loads(files["package-manifest.json"])
        for name in ("bad?.mjs", "CON.txt", "bad "):
            modified = dict(files)
            modified[name] = b"content"
            changed = dict(manifest, files={**manifest["files"], name: hashlib.sha256(b"content").hexdigest()})
            modified["package-manifest.json"] = json.dumps(changed).encode()
            output = io.BytesIO()
            with zipfile.ZipFile(output, "w") as archive:
                for path, content in modified.items():
                    archive.writestr(path, content)
            with self.subTest(name=name), self.assertRaises(app_release.PublicationError):
                app_release.verify_package(output.getvalue(), runtime_sha=runtime, license_sha=license_hash)


class ReleaseTests(NoticeFixtureMixin, unittest.TestCase):
    def publish(self, gateway, ctx=None):
        data, runtime, license_hash = package()
        return app_release.publish(ctx or context(), data, SETUP, gateway, runtime_sha=runtime, license_sha=license_hash)

    def test_preview_notes_disclose_missing_sources_and_game_launch_limits(self):
        gateway = FakeGateway()
        self.publish(gateway)
        body = gateway.releases[0]["body"]
        self.assertIn("module sources may be unavailable", body)
        self.assertIn("does not launch the game automatically", body)
        self.assertIn("Workshop coexistence detection is not complete", body)

    def test_changed_draft_metadata_never_becomes_public(self):
        for field, replacement in (("body", "owner changed the notes"),
                                   ("tag_name", "dev-manager-v9.9.9"),
                                   ("name", "owner changed the title")):
            class EditedDraft(FakeGateway):
                def upload(self, release_id, data):
                    super().upload(release_id, data)
                    self.release(release_id)[field] = replacement

            gateway = EditedDraft()
            with self.subTest(field=field), self.assertRaises(app_release.PublicationError):
                self.publish(gateway)
            self.assertFalse(any(call[0] == "publish" for call in gateway.calls))

    def test_retry_never_overwrites_owner_custom_title(self):
        gateway = FakeGateway()
        self.assertEqual(self.publish(gateway), "published")
        release = gateway.releases[0]
        release["name"] = "Owner custom title"
        before = len(gateway.calls)
        with self.assertRaises(app_release.PublicationError):
            self.publish(gateway)
        self.assertEqual(release["name"], "Owner custom title")
        self.assertFalse(any(call[0] == "rename" for call in gateway.calls[before:]))

    def test_stale_old_title_is_not_overwritten(self):
        class StaleList(FakeGateway):
            def __init__(self, releases):
                super().__init__(releases)
                self.reads = 0

            def list_releases(self):
                self.reads += 1
                snapshot = copy.deepcopy(super().list_releases())
                if self.reads == 2:
                    self.releases[0]["name"] = "Owner custom title"
                return snapshot

        older = {"id": 7, "tag_name": "dev-manager-v0.0.9", "name": "[LATEST APP] Old app",
                 "draft": False, "prerelease": True, "assets": [installer_asset()], "tag_commit": "b" * 40,
                 "published_at": "2026-11-02T00:00:00Z"}
        gateway = StaleList([older])
        with self.assertRaises(app_release.PublicationError):
            self.publish(gateway)
        self.assertEqual(older["name"], "Owner custom title")
        self.assertFalse(any(call[0] == "publish" for call in gateway.calls))

    def test_only_one_app_package_is_published_and_older_app_is_labelled(self):
        older = {"id": 7, "tag_name": "dev-manager-v0.0.9", "name": "[LATEST APP] Old app",
                 "draft": False, "prerelease": True, "assets": [installer_asset()], "tag_commit": "b" * 40,
                 "published_at": "2026-11-02T00:00:00Z"}
        gateway = FakeGateway([older])
        self.assertEqual(self.publish(gateway), "published")
        created = next(r for r in gateway.releases if r["id"] == 101)
        self.assertFalse(created["draft"])
        self.assertTrue(created["prerelease"])
        self.assertEqual(len(created["assets"]), 1)
        self.assertEqual(created["assets"][0]["name"], app_release.ASSET_NAME)
        self.assertTrue(created["assets"][0]["name"].endswith("_Setup.exe"))
        self.assertEqual(created["assets"][0]["size"], len(SETUP))
        self.assertIn("run the Windows installer", created["body"])
        self.assertIn("source-code archives", created["body"])
        self.assertNotIn("extract the portable package", created["body"])
        self.assertTrue(older["name"].startswith("[HISTORICAL APP]"))
        self.assertTrue(created["name"].startswith("[LATEST APP]"))
        self.assertLess(next(i for i, call in enumerate(gateway.calls) if call[0] == "rename"),
                        next(i for i, call in enumerate(gateway.calls) if call[0] == "publish"))
        self.assertFalse(any(call[0] == "delete" for call in gateway.calls))

    def test_unrelated_release_stale_source_or_bad_version_never_mutates(self):
        for gateway, ctx in (
            (FakeGateway([{"id": 2, "tag_name": "AI_Improve-dev-123-1", "name": "module", "draft": False}]), context()),
            (FakeGateway(head="b" * 40), context()),
            (FakeGateway(), {**context(), "version": "../bad"}),
        ):
            with self.subTest(ctx=ctx), self.assertRaises(app_release.PublicationError):
                self.publish(gateway, ctx)
            self.assertEqual(gateway.calls, [])

    def test_retry_reuses_matching_draft_and_never_overwrites_an_asset(self):
        data, runtime, license_hash = package()
        gateway = FakeGateway()
        created = app_release.prepare_draft(context(), data, SETUP, gateway, runtime_sha=runtime, license_sha=license_hash)
        gateway.upload(created["id"], SETUP)
        count = len(gateway.calls)
        self.assertEqual(app_release.publish(context(), data, SETUP, gateway, runtime_sha=runtime, license_sha=license_hash), "published")
        self.assertFalse(any(call[0] in ("create", "upload", "delete") for call in gateway.calls[count:]))

    def test_mismatched_existing_asset_remains_draft(self):
        data, runtime, license_hash = package()
        gateway = FakeGateway()
        created = app_release.prepare_draft(context(), data, SETUP, gateway, runtime_sha=runtime, license_sha=license_hash)
        created["assets"].append({"name": app_release.ASSET_NAME, "state": "uploaded", "size": 1, "digest": "sha256:" + "0" * 64})
        count = len(gateway.calls)
        with self.assertRaises(app_release.PublicationError):
            app_release.publish(context(), data, SETUP, gateway, runtime_sha=runtime, license_sha=license_hash)
        self.assertTrue(created["draft"])
        self.assertFalse(any(call[0] in ("publish", "delete") for call in gateway.calls[count:]))

    def test_source_moving_after_upload_keeps_draft_and_never_publishes(self):
        class MovingGateway(FakeGateway):
            def __init__(self):
                super().__init__()
                self.reads = 0

            def source_head(self):
                self.reads += 1
                return SOURCE if self.reads == 1 else "b" * 40

        gateway = MovingGateway()
        with self.assertRaises(app_release.PublicationError):
            self.publish(gateway)
        self.assertTrue(gateway.releases[0]["draft"])
        self.assertFalse(any(call[0] in ("publish", "delete") for call in gateway.calls))

    def test_invalid_installer_never_creates_a_release(self):
        data, runtime, license_hash = package()
        gateway = FakeGateway()
        with self.assertRaises(app_release.PublicationError):
            app_release.publish(context(), data, b"PK not an executable", gateway,
                                runtime_sha=runtime, license_sha=license_hash)
        self.assertEqual(gateway.calls, [])

    def test_existing_app_release_with_non_installer_asset_blocks_publication(self):
        older = {"id": 7, "tag_name": "dev-manager-v0.0.9", "name": "[LATEST APP] Old app",
                 "draft": False, "prerelease": True,
                 "assets": [{**installer_asset(), "name": "old-module.zip"}], "tag_commit": "b" * 40,
                 "published_at": "2026-11-02T00:00:00Z"}
        gateway = FakeGateway([older])
        with self.assertRaises(app_release.PublicationError):
            self.publish(gateway)
        self.assertEqual(gateway.calls, [])

    def test_different_installer_cannot_reuse_an_existing_draft(self):
        data, runtime, license_hash = package()
        gateway = FakeGateway()
        app_release.prepare_draft(context(), data, SETUP, gateway, runtime_sha=runtime, license_sha=license_hash)
        with self.assertRaises(app_release.PublicationError):
            app_release.publish(context(), data, b"MZ different installer", gateway,
                                runtime_sha=runtime, license_sha=license_hash)
        self.assertEqual([call[0] for call in gateway.calls], ["create"])

    def test_failed_publish_never_leaves_two_latest_labels(self):
        class FailedPublishGateway(FakeGateway):
            def publish_release(self, release_id, title):
                raise app_release.PublicationError("simulated publication failure")

        older = {"id": 7, "tag_name": "dev-manager-v0.0.9", "name": "[LATEST APP] Old app",
                 "draft": False, "prerelease": True, "assets": [installer_asset()], "tag_commit": "b" * 40,
                 "published_at": "2026-11-02T00:00:00Z"}
        gateway = FailedPublishGateway([older])
        with self.assertRaises(app_release.PublicationError):
            self.publish(gateway)
        self.assertTrue(gateway.releases[-1]["draft"])
        self.assertEqual([r["id"] for r in gateway.releases if r.get("name", "").startswith("[LATEST APP]")], [7])

    def test_source_move_after_relabel_restores_previous_latest(self):
        class MovingAfterRelabel(FakeGateway):
            def __init__(self, releases):
                super().__init__(releases)
                self.reads = 0

            def source_head(self):
                self.reads += 1
                return SOURCE if self.reads <= 2 else "b" * 40

        older = {"id": 7, "tag_name": "dev-manager-v0.0.9", "name": "[LATEST APP] Old app",
                 "draft": False, "prerelease": True, "assets": [installer_asset()], "tag_commit": "b" * 40,
                 "published_at": "2026-11-02T00:00:00Z"}
        gateway = MovingAfterRelabel([older])
        with self.assertRaises(app_release.PublicationError):
            self.publish(gateway)
        self.assertTrue(gateway.releases[-1]["draft"])
        self.assertEqual(older["name"], "[LATEST APP] Old app")

    def test_lost_publish_response_recovers_verified_published_release(self):
        class LostResponse(FakeGateway):
            def publish_release(self, release_id, title):
                super().publish_release(release_id, title)
                raise app_release.PublicationError("simulated lost response")

        older = {"id": 7, "tag_name": "dev-manager-v0.0.9", "name": "[LATEST APP] Old app",
                 "draft": False, "prerelease": True, "assets": [installer_asset()], "tag_commit": "b" * 40,
                 "published_at": "2026-11-02T00:00:00Z"}
        gateway = LostResponse([older])
        self.assertEqual(self.publish(gateway), "published")
        self.assertTrue(older["name"].startswith("[HISTORICAL APP]"))
        self.assertFalse(gateway.releases[-1]["draft"])
        self.assertTrue(gateway.releases[-1]["name"].startswith("[LATEST APP]"))

    def test_failed_publish_does_not_overwrite_owner_title_edit(self):
        class OwnerEdited(FakeGateway):
            def publish_release(self, release_id, title):
                self.releases[0]["name"] = "Owner edited this title"
                raise app_release.PublicationError("simulated publication failure")

        older = {"id": 7, "tag_name": "dev-manager-v0.0.9", "name": "[LATEST APP] Old app",
                 "draft": False, "prerelease": True, "assets": [installer_asset()], "tag_commit": "b" * 40,
                 "published_at": "2026-11-02T00:00:00Z"}
        gateway = OwnerEdited([older])
        with self.assertRaises(app_release.PublicationError):
            self.publish(gateway)
        self.assertEqual(older["name"], "Owner edited this title")

    def test_unknown_publish_state_never_guesses_or_restores_labels(self):
        class UnknownOutcome(FakeGateway):
            def __init__(self, releases):
                super().__init__(releases)
                self.failed = False

            def publish_release(self, release_id, title):
                self.failed = True
                raise app_release.PublicationError("simulated timeout")

            def release(self, release_id):
                if self.failed:
                    raise app_release.PublicationError("simulated read failure")
                return super().release(release_id)

        older = {"id": 7, "tag_name": "dev-manager-v0.0.9", "name": "[LATEST APP] Old app",
                 "draft": False, "prerelease": True, "assets": [installer_asset()], "tag_commit": "b" * 40,
                 "published_at": "2026-11-02T00:00:00Z"}
        gateway = UnknownOutcome([older])
        with self.assertRaisesRegex(app_release.PublicationError, "outcome is unknown"):
            self.publish(gateway)
        self.assertEqual(len([call for call in gateway.calls if call[0] == "rename"]), 1)

    def test_retry_of_older_app_release_never_demotes_newer_release(self):
        gateway = FakeGateway()
        self.assertEqual(self.publish(gateway), "published")
        older = gateway.releases[0]
        older["name"] = older["name"].replace("[LATEST APP]", "[HISTORICAL APP]")
        newer = {"id": 202, "tag_name": "dev-manager-v0.2.0", "name": "[LATEST APP] New app",
                 "draft": False, "prerelease": True, "assets": [installer_asset()], "tag_commit": SOURCE,
                 "published_at": "2026-11-04T00:00:00Z"}
        gateway.releases.append(newer)
        before = list(gateway.calls)
        self.assertEqual(self.publish(gateway), "already-published")
        self.assertEqual(gateway.calls, before)
        self.assertTrue(newer["name"].startswith("[LATEST APP]"))


if __name__ == "__main__":
    unittest.main()

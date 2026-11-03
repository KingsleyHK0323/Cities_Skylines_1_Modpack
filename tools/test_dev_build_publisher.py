"""Offline publication contracts for independent Development build branches.

Signature: OpenAI-Codex
"""

import hashlib
import json
import unittest
from types import SimpleNamespace
from unittest.mock import patch

import dev_build_publisher as publisher


SOURCE = "a" * 40
OLD_SOURCE = "b" * 40


def context(branch="AI_Improve", source_sha=SOURCE, run_id=101, attempt=1):
    return {
        "repository": "KingsleyHK0323/Cities_Skylines_1_Modpack",
        "branch": branch,
        "source_sha": source_sha,
        "run_id": run_id,
        "attempt": attempt,
    }


def payload(ctx):
    config = publisher.MODULES[ctx["branch"]]
    files = {name: ("fixture:" + name).encode("utf-8") for name in config["files"]}
    if ctx["branch"] == "AI_Improve":
        files["build-info.json"] = json.dumps({
            "sourceSHA": ctx["source_sha"], "runId": str(ctx["run_id"]),
            "runAttempt": str(ctx["attempt"]), "DevTools": True,
        }).encode("utf-8")
    else:
        files["build-info.txt"] = (
            f"sourceSHA={ctx['source_sha']}\nrunid={ctx['run_id']}\n"
            f"runattempt={ctx['attempt']}\nDevTools=true\n"
        ).encode("utf-8")
    files["SHA256SUMS"] = "".join(
        f"{hashlib.sha256(data).hexdigest()}  {name}\n"
        for name, data in sorted(files.items())
    ).encode("ascii")
    return files


class FakeGateway:
    def __init__(self, head=SOURCE, previous=None, relation="ahead"):
        self.heads = [head, head]
        self.previous = previous
        self.relation = relation
        self.writes = []

    def source_head(self, branch):
        return self.heads.pop(0) if len(self.heads) > 1 else self.heads[0]

    def artifact_state(self, branch):
        return self.previous

    def compare(self, old, new):
        return self.relation

    def run(self, run_id, attempt):
        return {"started": f"2026-11-{min(run_id - 98, 28):02d}T00:00:00Z"}

    def write_blob(self, data):
        sha = hashlib.sha1(b"blob " + str(len(data)).encode() + b"\0" + data).hexdigest()
        self.writes.append(("blob", sha))
        return sha

    def write_tree(self, base_tree, entries):
        self.writes.append(("tree", base_tree, entries))
        return "c" * 40

    def write_commit(self, tree, parent):
        self.writes.append(("commit", tree, parent))
        return "d" * 40

    def create_ref(self, branch, sha):
        self.writes.append(("create_ref", branch, sha))

    def update_ref(self, branch, sha, force):
        self.writes.append(("update_ref", branch, sha, force))


class ManifestTests(unittest.TestCase):
    def test_publication_requires_the_current_compile_attempt(self):
        publisher.validate_workflow_attempt(2, 2)
        for build_attempt, workflow_attempt in ((1, 2), (0, 1), (1, 0)):
            with self.subTest(build_attempt=build_attempt, workflow_attempt=workflow_attempt), self.assertRaises(publisher.PublicationError):
                publisher.validate_workflow_attempt(build_attempt, workflow_attempt)

    def test_two_modules_keep_independent_identity_and_required_files(self):
        for branch in ("AI_Improve", "Automated_Public_Transport_Planning"):
            ctx = context(branch)
            manifest, outputs = publisher.prepare_publication(ctx, payload(ctx))
            self.assertEqual(manifest["artifactBranch"], "Development_Build/" + branch)
            self.assertEqual(manifest["sourceSha"], SOURCE)
            self.assertEqual(manifest["buildId"], branch + "-dev-101-1")
            self.assertIs(manifest["devTools"], True)
            self.assertEqual(manifest["signature"], "OpenAI-Codex")
            self.assertTrue(outputs)
            self.assertEqual({entry["path"] for entry in manifest["files"]}, set(outputs))
            self.assertTrue(all(name.startswith("builds/101-1/") for name in outputs))
            self.assertTrue(all(entry["sha256"] == hashlib.sha256(outputs[entry["path"]]).hexdigest()
                                for entry in manifest["files"]))
            self.assertNotIn("SHA256SUMS", {entry["name"] for entry in manifest["files"]})
            self.assertNotIn("Assembly-CSharp.dll", {entry["name"] for entry in manifest["files"]})

    def test_missing_tampered_extra_or_unverified_build_is_rejected_before_writes(self):
        ctx = context()
        for variation in ("missing", "tampered", "extra", "wrong-build", "devtools-off"):
            files = payload(ctx)
            if variation == "missing":
                files.pop("CitiesHarmony.API.dll")
            elif variation == "tampered":
                files["AIImprove.dll"] = b"changed after the checksum"
            elif variation == "extra":
                files["Assembly-CSharp.dll"] = b"game reference"
            else:
                info = json.loads(files["build-info.json"])
                info["sourceSHA" if variation == "wrong-build" else "DevTools"] = OLD_SOURCE if variation == "wrong-build" else False
                files["build-info.json"] = json.dumps(info).encode()
                files["SHA256SUMS"] = "".join(
                    f"{hashlib.sha256(data).hexdigest()}  {name}\n"
                    for name, data in sorted(files.items()) if name != "SHA256SUMS"
                ).encode("ascii")
            gateway = FakeGateway()
            with self.subTest(variation=variation), self.assertRaises(publisher.PublicationError):
                publisher.publish(ctx, files, gateway)
            self.assertEqual(gateway.writes, [])

    def test_json_devtools_evidence_must_be_boolean_not_a_string(self):
        ctx = context()
        files = payload(ctx)
        info = json.loads(files["build-info.json"])
        info["DevTools"] = "true"
        files["build-info.json"] = json.dumps(info).encode()
        files["SHA256SUMS"] = "".join(
            f"{hashlib.sha256(data).hexdigest()}  {name}\n"
            for name, data in sorted(files.items()) if name != "SHA256SUMS"
        ).encode("ascii")
        with self.assertRaises(publisher.PublicationError):
            publisher.prepare_publication(ctx, files)


class RefUpdateTests(unittest.TestCase):
    def test_first_publication_creates_one_independent_branch(self):
        ctx = context()
        gateway = FakeGateway()
        self.assertEqual(publisher.publish(ctx, payload(ctx), gateway), "published")
        refs = [call for call in gateway.writes if call[0].endswith("_ref")]
        self.assertEqual(refs, [("create_ref", "Development_Build/AI_Improve", "d" * 40)])
        tree = next(call for call in gateway.writes if call[0] == "tree")
        self.assertIsNone(tree[1])
        self.assertIn("manifest.json", {item["path"] for item in tree[2]})

    def test_source_changes_before_or_during_publication_never_update_a_ref(self):
        for heads in ([OLD_SOURCE], [SOURCE, OLD_SOURCE]):
            gateway = FakeGateway()
            gateway.heads = list(heads)
            with self.subTest(heads=heads), self.assertRaises(publisher.PublicationError):
                publisher.publish(context(), payload(context()), gateway)
            self.assertFalse(any(call[0].endswith("_ref") for call in gateway.writes))

    def test_existing_branch_is_fast_forward_only_and_preserves_its_tree(self):
        ctx = context()
        old_manifest, _ = publisher.prepare_publication(context(source_sha=OLD_SOURCE, run_id=100),
                                                         payload(context(source_sha=OLD_SOURCE, run_id=100)))
        previous = {"head": "e" * 40, "tree": "f" * 40, "manifest": old_manifest, "paths": {}}
        gateway = FakeGateway(previous=previous)
        self.assertEqual(publisher.publish(ctx, payload(ctx), gateway), "published")
        self.assertIn(("update_ref", "Development_Build/AI_Improve", "d" * 40, False), gateway.writes)
        self.assertEqual(next(call for call in gateway.writes if call[0] == "tree")[1], "f" * 40)
        self.assertEqual(next(call for call in gateway.writes if call[0] == "commit")[2], "e" * 40)

    def test_older_divergent_or_wrong_module_never_updates_a_ref(self):
        old_ctx = context(source_sha=OLD_SOURCE, run_id=100)
        old_manifest, _ = publisher.prepare_publication(old_ctx, payload(old_ctx))
        for relation in ("behind", "diverged", "unknown"):
            previous = {"head": "e" * 40, "tree": "f" * 40, "manifest": old_manifest, "paths": {}}
            gateway = FakeGateway(previous=previous, relation=relation)
            with self.subTest(relation=relation), self.assertRaises(publisher.PublicationError):
                publisher.publish(context(), payload(context()), gateway)
            self.assertFalse(any(call[0].endswith("_ref") for call in gateway.writes))
        other = dict(old_manifest, moduleId="another-module")
        gateway = FakeGateway(previous={"head": "e" * 40, "tree": "f" * 40, "manifest": other, "paths": {}})
        with self.assertRaises(publisher.PublicationError):
            publisher.publish(context(), payload(context()), gateway)
        self.assertEqual(gateway.writes, [])


class GitHubGatewayTests(unittest.TestCase):
    def test_ref_updates_never_allow_force(self):
        gateway = publisher.GitHubGateway(context())
        with patch.object(gateway, "_call", return_value={}) as call:
            with self.assertRaises(publisher.PublicationError):
                gateway.update_ref("Development_Build/AI_Improve", "d" * 40, True)
            call.assert_not_called()
            gateway.update_ref("Development_Build/AI_Improve", "d" * 40, False)
            self.assertEqual(call.call_args.args[2]["force"], False)

    def test_remote_errors_do_not_echo_credentials_or_raw_diagnostics(self):
        gateway = publisher.GitHubGateway(context())
        response = SimpleNamespace(returncode=1, stdout="", stderr="sensitive-example-value")
        with patch.object(publisher.subprocess, "run", return_value=response):
            with self.assertRaises(publisher.PublicationError) as caught:
                gateway.source_head("AI_Improve")
        self.assertNotIn("sensitive-example-value", str(caught.exception))

    def test_existing_build_path_is_never_overwritten_with_different_bytes(self):
        old_ctx = context(source_sha=OLD_SOURCE, run_id=100)
        old_manifest, _ = publisher.prepare_publication(old_ctx, payload(old_ctx))
        previous = {"head": "e" * 40, "tree": "f" * 40, "manifest": old_manifest,
                    "paths": {"builds/101-1/AIImprove.dll": "0" * 40}}
        gateway = FakeGateway(previous=previous)
        with self.assertRaises(publisher.PublicationError):
            publisher.publish(context(), payload(context()), gateway)
        self.assertFalse(any(call[0].endswith("_ref") for call in gateway.writes))

    def test_same_build_is_unchanged_only_when_every_existing_blob_matches(self):
        ctx = context()
        manifest, files = publisher.prepare_publication(ctx, payload(ctx))
        paths = {path: hashlib.sha1(b"blob " + str(len(data)).encode() + b"\0" + data).hexdigest()
                 for path, data in files.items()}
        state = {"head": "e" * 40, "tree": "f" * 40, "manifest": manifest, "paths": paths}
        gateway = FakeGateway(previous=state)
        self.assertEqual(publisher.publish(ctx, payload(ctx), gateway), "unchanged")
        self.assertEqual(gateway.writes, [])
        state["paths"] = {}
        with self.assertRaises(publisher.PublicationError):
            publisher.publish(ctx, payload(ctx), FakeGateway(previous=state))

    def test_same_source_requires_a_later_build_or_attempt(self):
        old_ctx = context(run_id=100)
        old_manifest, _ = publisher.prepare_publication(old_ctx, payload(old_ctx))
        previous = {"head": "e" * 40, "tree": "f" * 40, "manifest": old_manifest, "paths": {}}
        self.assertEqual(publisher.publish(context(), payload(context()), FakeGateway(previous=previous)), "published")
        old_ctx = context(run_id=102)
        old_manifest, _ = publisher.prepare_publication(old_ctx, payload(old_ctx))
        previous["manifest"] = old_manifest
        gateway = FakeGateway(previous=previous)
        with self.assertRaises(publisher.PublicationError):
            publisher.publish(context(), payload(context()), gateway)
        self.assertEqual(gateway.writes, [])

    def test_corrupt_previous_build_identity_never_updates_the_branch(self):
        old_ctx = context(source_sha=OLD_SOURCE, run_id=100)
        old_manifest, _ = publisher.prepare_publication(old_ctx, payload(old_ctx))
        old_manifest["buildId"] = "another-build"
        previous = {"head": "e" * 40, "tree": "f" * 40, "manifest": old_manifest, "paths": {}}
        gateway = FakeGateway(previous=previous)
        with self.assertRaises(publisher.PublicationError):
            publisher.publish(context(), payload(context()), gateway)
        self.assertEqual(gateway.writes, [])


if __name__ == "__main__":
    unittest.main()

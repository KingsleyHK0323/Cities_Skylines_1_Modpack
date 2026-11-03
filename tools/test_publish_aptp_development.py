"""Synthetic checks for the APTP artifact identity gate.

Signature: OpenAI-Codex
"""

import copy
import hashlib
import importlib
import json
import os
from pathlib import Path
import stat
import sys
from types import SimpleNamespace
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parent))
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / '.github' / 'scripts'))
from Release_Provenance import BRANCH, REPOSITORY, SHARED_REFS
from test_dev_build_publisher import FakeGateway, context, payload


class FakeFile:
    def __init__(self, name, data):
        self.name, self.data = name, data

    def lstat(self):
        return SimpleNamespace(st_mode=stat.S_IFREG | 0o644, st_size=len(self.data))

    def is_symlink(self):
        return False

    def read_bytes(self):
        return self.data


class FakeDirectory:
    def __init__(self, files):
        self.files = files

    def is_dir(self):
        return True

    def is_symlink(self):
        return False

    def iterdir(self):
        return (FakeFile(name, data) for name, data in self.files.items())


class PublishAptpChecks(unittest.TestCase):
    def setUp(self):
        self.module = importlib.import_module('publish_aptp_development')
        self.env = dict(GITHUB_REPOSITORY=REPOSITORY, GITHUB_REF='refs/heads/' + BRANCH,
                        GITHUB_SHA='a' * 40, GITHUB_RUN_ID='123', GITHUB_RUN_ATTEMPT='2',
                        GITHUB_EVENT_NAME='push', RELEASE_BUILD_ATTEMPT='2', RELEASE_ARTIFACT_ID='456')
        self.metadata = ('sourceSHA=' + 'a' * 40 + '\nsharedrefsSHA=' + SHARED_REFS +
                         '\nrunid=123\nrunattempt=2\nDevTools=true\nrepository=' + REPOSITORY +
                         '\nref=refs/heads/' + BRANCH + '\n')
        self.artifact = dict(id=456, name='aptp-public-assets-123-2', expired=False,
                             workflow_run=dict(id=123, head_sha='a' * 40, head_branch=BRANCH))

    def invoke(self, env=None, metadata=None, artifact=None):
        selected_env = self.env if env is None else env
        selected_metadata = self.metadata if metadata is None else metadata
        selected_artifact = self.artifact if artifact is None else artifact
        with patch.dict(os.environ, selected_env, clear=True), \
             patch.object(sys, 'argv', ['publish_aptp_development.py', 'artifact']), \
             patch('pathlib.Path.read_text', return_value=selected_metadata), \
             patch.object(self.module.subprocess, 'check_output', return_value=json.dumps(selected_artifact)) as api, \
             patch.object(self.module.publisher, 'main') as publish:
            try:
                self.module.main()
            finally:
                self.api = api
                self.publish = publish

    def test_verified_artifact_enters_publisher_once(self):
        self.invoke()
        self.publish.assert_called_once_with()
        self.api.assert_called_once()

    def test_invalid_artifact_never_enters_publisher(self):
        variants = []
        for key, value in [('id', 999), ('name', 'wrong'), ('expired', True)]:
            artifact = copy.deepcopy(self.artifact)
            artifact[key] = value
            variants.append(artifact)
        for key, value in [('id', 999), ('head_sha', 'b' * 40), ('head_branch', 'other')]:
            artifact = copy.deepcopy(self.artifact)
            artifact['workflow_run'][key] = value
            variants.append(artifact)
        for artifact in variants:
            with self.subTest(artifact=artifact), self.assertRaises(ValueError):
                self.invoke(artifact=artifact)
            self.publish.assert_not_called()

    def test_wrong_or_duplicate_shared_reference_never_enters_publisher(self):
        for metadata in [self.metadata.replace(SHARED_REFS, 'b' * 40),
                         self.metadata + 'sharedrefsSHA=' + SHARED_REFS + '\n']:
            with self.subTest(metadata=metadata), self.assertRaises(ValueError):
                self.invoke(metadata=metadata)
            self.publish.assert_not_called()

    def test_mismatched_attempt_never_enters_publisher_or_calls_api(self):
        env = dict(self.env, GITHUB_RUN_ATTEMPT='3')
        with self.assertRaises(Exception):
            self.invoke(env=env)
        self.api.assert_not_called()
        self.publish.assert_not_called()

    def test_wrong_source_or_missing_artifact_id_never_enters_publisher(self):
        for env in [dict(self.env, GITHUB_SHA='b' * 40), dict(self.env, RELEASE_ARTIFACT_ID='')]:
            with self.subTest(env=env), self.assertRaises(ValueError):
                self.invoke(env=env)
            self.publish.assert_not_called()

    def test_real_publisher_creates_only_the_aptp_ref_after_verified_artifact(self):
        ctx = context(BRANCH, run_id=123, attempt=2)
        files = payload(ctx)
        files['build-info.txt'] = self.metadata.encode('utf-8')
        files['SHA256SUMS'] = ''.join(
            hashlib.sha256(data).hexdigest() + '  ' + name + '\n'
            for name, data in sorted(files.items()) if name != 'SHA256SUMS'
        ).encode('ascii')
        for failure in ('none', 'artifact', 'sharedrefs', 'checksum'):
            with self.subTest(failure=failure):
                gateway = FakeGateway()
                candidate = dict(files)
                artifact = copy.deepcopy(self.artifact)
                metadata = self.metadata
                if failure == 'artifact':
                    artifact['id'] = 999
                elif failure == 'sharedrefs':
                    metadata = metadata.replace(SHARED_REFS, 'b' * 40)
                elif failure == 'checksum':
                    candidate['AutomatedPublicTransportPlanning.dll'] = b'tampered'
                with patch.dict(os.environ, self.env, clear=True), \
                     patch.object(sys, 'argv', ['publish_aptp_development.py', 'artifact']), \
                     patch('pathlib.Path.read_text', return_value=metadata), \
                     patch.object(self.module.subprocess, 'check_output', return_value=json.dumps(artifact)), \
                     patch.object(self.module.publisher, 'Path', return_value=FakeDirectory(candidate)), \
                     patch.object(self.module.publisher, 'GitHubGateway', return_value=gateway):
                    if failure == 'none':
                        self.module.main()
                        self.assertIn(('create_ref', 'Development_Build/' + BRANCH, 'd' * 40), gateway.writes)
                    else:
                        with self.assertRaises((ValueError, self.module.publisher.PublicationError)):
                            self.module.main()
                        self.assertEqual([], gateway.writes)


if __name__ == '__main__':
    unittest.main()

# Signature: OpenAI-Codex_Automated_Public_Transport_Planning
"""Synthetic publisher contracts; no network, credentials, game files or release writes."""
import copy
import hashlib
import json
import os
import runpy
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / '.github' / 'scripts'))
from Release_Provenance import verify_provenance, REPOSITORY, BRANCH, SHARED_REFS


class ReleaseProvenanceChecks(unittest.TestCase):
    def setUp(self):
        self.expected = dict(repository=REPOSITORY, ref='refs/heads/' + BRANCH,
                             sha='a' * 40, run_id='123', build_attempt='2', artifact_id='456')
        self.metadata = ('\ufeffsourceSHA=' + 'a' * 40 + '\nsharedrefsSHA=' + SHARED_REFS +
                         '\nrunid=123\nrunattempt=2\nDevTools=true\nrepository=' + REPOSITORY +
                         '\nref=refs/heads/' + BRANCH + '\nSignature: test\n')
        self.artifact = dict(id=456, name='aptp-public-assets-123-2', expired=False,
                             workflow_run=dict(id=123, head_sha='a' * 40, head_branch=BRANCH))

    def test_valid_build_attempt_survives_publish_job_retry(self):
        # The publisher's current attempt is deliberately absent from this contract.
        self.assertEqual('2', verify_provenance(self.metadata, self.artifact, **self.expected))

    def test_reject_wrong_artifact_identity(self):
        for key, value in [('id', 999), ('name', 'aptp-public-assets-123-1'), ('expired', True)]:
            with self.subTest(key=key):
                artifact = copy.deepcopy(self.artifact)
                artifact[key] = value
                with self.assertRaises(ValueError):
                    verify_provenance(self.metadata, artifact, **self.expected)

    def test_reject_wrong_workflow_source(self):
        for key, value in [('id', 124), ('head_sha', 'b' * 40), ('head_branch', 'AI_Improve')]:
            with self.subTest(key=key):
                artifact = copy.deepcopy(self.artifact)
                artifact['workflow_run'][key] = value
                with self.assertRaises(ValueError):
                    verify_provenance(self.metadata, artifact, **self.expected)

    def test_reject_stale_or_missing_build_info(self):
        for old, new in [('runattempt=2', 'runattempt=1'), ('runid=123', 'runid=124'),
                         ('sourceSHA=' + 'a' * 40, 'sourceSHA=' + 'b' * 40),
                         ('sharedrefsSHA=' + SHARED_REFS, ''), ('DevTools=true', 'DevTools=false'),
                         ('repository=' + REPOSITORY, ''), ('ref=refs/heads/' + BRANCH, '')]:
            with self.subTest(field=old):
                with self.assertRaises(ValueError):
                    verify_provenance(self.metadata.replace(old, new), self.artifact, **self.expected)

    def test_reject_duplicate_fields_and_untrusted_context(self):
        with self.assertRaises(ValueError):
            verify_provenance(self.metadata + 'runid=123\n', self.artifact, **self.expected)
        for key, value in [('repository', 'someone/fork'), ('ref', 'refs/heads/AI_Improve'),
                           ('artifact_id', ''), ('build_attempt', ''), ('run_id', '0')]:
            with self.subTest(key=key):
                expected = dict(self.expected)
                expected[key] = value
                with self.assertRaises(ValueError):
                    verify_provenance(self.metadata, self.artifact, **expected)

    def test_actual_publisher_rejects_provenance_before_release_mutation(self):
        publisher = Path(__file__).resolve().parents[1] / '.github' / 'scripts' / 'Publish-Release.py'
        for mismatch in ('artifact', 'attempt'):
            with self.subTest(mismatch=mismatch), tempfile.TemporaryDirectory() as temp:
                root = Path(temp) / 'release-assets'
                root.mkdir()
                metadata = self.metadata.replace('runattempt=2', 'runattempt=1') if mismatch == 'attempt' else self.metadata
                files = {'AutomatedPublicTransportPlanning.dll': b'synthetic DLL, not executable',
                         'README.md': b'synthetic release body', 'LICENSE': b'synthetic license fixture',
                         'build-info.txt': metadata.encode('utf-8'), 'Steam_Workshop_Link.txt': b'fixture'}
                for name, data in files.items():
                    (root / name).write_bytes(data)
                (root / 'SHA256SUMS').write_text('\n'.join(
                    hashlib.sha256(data).hexdigest() + '  ' + name for name, data in files.items()), encoding='ascii')
                artifact = copy.deepcopy(self.artifact)
                if mismatch == 'artifact':
                    artifact['id'] = 999
                calls = []

                def fake_gh(args, **kwargs):
                    calls.append(args)
                    self.assertEqual(['gh', 'api', f'repos/{REPOSITORY}/actions/artifacts/456'], args)
                    return json.dumps(artifact)

                previous = os.getcwd()
                try:
                    os.chdir(temp)
                    with patch.dict(os.environ, dict(GITHUB_REPOSITORY=REPOSITORY,
                                    GITHUB_REF='refs/heads/' + BRANCH, GITHUB_SHA='a' * 40,
                                    GITHUB_RUN_ID='123', GITHUB_RUN_ATTEMPT='9',
                                    RELEASE_BUILD_ATTEMPT='2', RELEASE_ARTIFACT_ID='456')):
                        with patch('subprocess.check_output', side_effect=fake_gh):
                            with self.assertRaises(ValueError):
                                runpy.run_path(str(publisher), run_name='__main__')
                    self.assertEqual(1, len(calls), 'no draft/upload/edit call is allowed after mismatch')
                finally:
                    os.chdir(previous)


if __name__ == '__main__':
    unittest.main()

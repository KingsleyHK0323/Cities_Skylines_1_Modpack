"""Synthetic provenance checks for resuming the same App draft.

Signature: OpenAI-Codex
"""

import copy
import hashlib
import unittest
from unittest import mock

import app_release
import retry_release


class RecoveryChecks(unittest.TestCase):
    def setUp(self):
        self.sha = 'a' * 40
        self.run = dict(id=123, run_attempt=1, status='completed', conclusion='failure',
                        path='.github/workflows/dev-manager-app-release.yml', event='workflow_dispatch',
                        head_branch='Modpack_Website', head_sha=self.sha,
                        repository=dict(full_name=app_release.REPOSITORY, id=42),
                        head_repository=dict(full_name=app_release.REPOSITORY, id=42),
                        actor=dict(login='KingsleyHK0323'), triggering_actor=dict(login='KingsleyHK0323'))
        self.artifact = dict(id=456, name='dev-manager-app-123-1', expired=False,
                             workflow_run=dict(id=123, head_sha=self.sha, head_branch='Modpack_Website',
                                               repository_id=42, head_repository_id=42))
        self.jobs = dict(total_count=1, jobs=[dict(name='build', status='completed', conclusion='success',
                                                   run_id=123, run_attempt=1, head_sha=self.sha)])
        self.payload, self.setup = b'original payload', b'MZ original installer'
        self.ctx = dict(repository=app_release.REPOSITORY, sourceSha='b' * 40,
                        version='v0.1.0-beta.1', runId=123, attempt=1)
        self.identity = {**self.ctx, 'payloadSha256': hashlib.sha256(self.payload).hexdigest(),
                         'setupSha256': hashlib.sha256(self.setup).hexdigest()}

    def test_original_run_and_artifact_are_accepted(self):
        retry_release.validate_origin(self.run, self.artifact, self.jobs, 123, 1, 456)
        self.assertEqual(self.ctx, retry_release.recovery_context(self.identity, self.payload, self.setup, 123, 1))

    def test_wrong_run_or_artifact_identity_is_rejected(self):
        for field, value in [('status', 'in_progress'), ('conclusion', 'success'), ('event', 'push'),
                             ('path', '.github/workflows/other.yml'), ('head_branch', 'AI_Improve'),
                             ('run_attempt', 2)]:
            altered = dict(self.run, **{field: value})
            with self.subTest(field=field), self.assertRaises(app_release.PublicationError):
                retry_release.validate_origin(altered, self.artifact, self.jobs, 123, 1, 456)
        for field, value in [('id', 999), ('name', 'other'), ('expired', True)]:
            altered = dict(self.artifact, **{field: value})
            with self.subTest(field=field), self.assertRaises(app_release.PublicationError):
                retry_release.validate_origin(self.run, altered, self.jobs, 123, 1, 456)
        altered = copy.deepcopy(self.artifact)
        altered['workflow_run']['head_sha'] = 'c' * 40
        with self.assertRaises(app_release.PublicationError):
            retry_release.validate_origin(self.run, altered, self.jobs, 123, 1, 456)

    def test_build_job_must_be_successful_and_from_the_original_attempt(self):
        for field, value in [('conclusion', 'failure'), ('run_id', 124), ('run_attempt', 2),
                             ('head_sha', 'c' * 40)]:
            altered = copy.deepcopy(self.jobs)
            altered['jobs'][0][field] = value
            with self.subTest(field=field), self.assertRaises(app_release.PublicationError):
                retry_release.validate_origin(self.run, self.artifact, altered, 123, 1, 456)

    def test_wrong_author_and_repository_are_rejected(self):
        for field, value in [('actor', dict(login='other')),
                             ('triggering_actor', dict(login='other')),
                             ('head_repository', dict(full_name='other/repo', id=42))]:
            altered = dict(self.run, **{field: value})
            with self.subTest(field=field), self.assertRaises(app_release.PublicationError):
                retry_release.validate_origin(altered, self.artifact, self.jobs, 123, 1, 456)

    def test_identity_does_not_accept_another_run_or_modified_bytes(self):
        with self.assertRaises(app_release.PublicationError):
            retry_release.recovery_context(self.identity, self.payload, self.setup, 124, 1)
        with self.assertRaises(app_release.PublicationError):
            retry_release.recovery_context(self.identity, self.payload, self.setup + b'x', 123, 1)

    def test_recovery_gateway_never_creates_a_new_release(self):
        with self.assertRaises(app_release.PublicationError):
            retry_release.ExistingReleaseGateway(789).create_release({})

    def test_recovery_refuses_another_release_id_even_if_tags_are_changed(self):
        gateway = retry_release.ExistingReleaseGateway(789)
        gateway.expected_tag = 'dev-manager-v0.1.0-beta.1'
        releases = [dict(id=789, tag_name='changed'), dict(id=790, tag_name=gateway.expected_tag)]
        with mock.patch.object(app_release.GitHubGateway, 'list_releases', return_value=releases):
            with self.assertRaises(app_release.PublicationError):
                gateway.list_releases()
        with mock.patch.object(app_release.GitHubGateway, 'upload') as upload, \
             mock.patch.object(app_release.GitHubGateway, 'publish_release') as publish:
            with self.assertRaises(app_release.PublicationError):
                gateway.upload(790, b'MZ fixture')
            with self.assertRaises(app_release.PublicationError):
                gateway.publish_release(790, 'test')
        upload.assert_not_called()
        publish.assert_not_called()


if __name__ == '__main__':
    unittest.main()

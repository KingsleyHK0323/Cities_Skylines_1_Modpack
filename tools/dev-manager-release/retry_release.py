"""Resume one existing App Release using its original verified cloud build.

Signature: OpenAI-Codex
"""

import json
import os
from pathlib import Path
import re
import sys

import app_release as app

CONTROLLER_BRANCH = 'Modpack_Website'
WORKFLOW = '.github/workflows/dev-manager-app-release.yml'
OWNER = 'kingsleyhk0323'


def validate_origin(run, artifact, jobs, run_id, attempt, artifact_id):
    app.require(all(type(value) is int and value > 0 for value in (run_id, attempt, artifact_id)), 'Invalid original build identity')
    app.require(isinstance(run, dict) and run.get('id') == run_id and run.get('run_attempt') == attempt and
                run.get('status') == 'completed' and run.get('conclusion') == 'failure' and
                run.get('path') in (WORKFLOW, WORKFLOW + '@' + CONTROLLER_BRANCH,
                                    WORKFLOW + '@refs/heads/' + CONTROLLER_BRANCH) and
                run.get('event') == 'workflow_dispatch' and run.get('head_branch') == CONTROLLER_BRANCH and
                app.valid_sha(run.get('head_sha')), 'Original workflow identity is invalid')
    repository = run.get('repository') or {}
    head_repository = run.get('head_repository') or {}
    app.require(repository.get('full_name') == app.REPOSITORY and
                head_repository.get('full_name') == app.REPOSITORY and
                type(repository.get('id')) is int and repository['id'] > 0 and
                head_repository.get('id') == repository['id'] and
                str((run.get('actor') or {}).get('login', '')).casefold() == OWNER and
                str((run.get('triggering_actor') or {}).get('login', '')).casefold() == OWNER,
                'Original workflow owner or repository is invalid')
    source = artifact.get('workflow_run') if isinstance(artifact, dict) else None
    app.require(isinstance(source, dict) and artifact.get('id') == artifact_id and
                artifact.get('name') == f'dev-manager-app-{run_id}-{attempt}' and artifact.get('expired') is False and
                source.get('id') == run_id and source.get('head_sha') == run['head_sha'] and
                source.get('head_branch') == CONTROLLER_BRANCH and
                source.get('repository_id') == repository['id'] and source.get('head_repository_id') == repository['id'],
                'Original artifact identity is invalid')
    listing = jobs.get('jobs') if isinstance(jobs, dict) else None
    app.require(isinstance(listing, list) and type(jobs.get('total_count')) is int and
                jobs['total_count'] == len(listing) and len(listing) <= 100, 'Original job listing is incomplete')
    builds = [job for job in listing if isinstance(job, dict) and job.get('name') == 'build']
    app.require(len(builds) == 1 and builds[0].get('status') == 'completed' and
                builds[0].get('conclusion') == 'success' and builds[0].get('run_id') == run_id and
                builds[0].get('run_attempt') == attempt and builds[0].get('head_sha') == run['head_sha'],
                'Original build job was not successful')


def recovery_context(identity, payload, setup, run_id, attempt):
    app.require(isinstance(identity, dict), 'Original build identity is missing')
    ctx = dict(repository=app.REPOSITORY, sourceSha=identity.get('sourceSha'), version=identity.get('version'),
               runId=run_id, attempt=attempt)
    app._context(ctx)
    app.verify_build_identity(ctx, identity, payload, setup)
    return ctx


class ExistingReleaseGateway(app.GitHubGateway):
    def __init__(self, release_id):
        super().__init__()
        self.expected_release_id = release_id
        self.expected_tag = None

    def list_releases(self):
        releases = super().list_releases()
        matches = [item for item in releases if item.get('id') == self.expected_release_id]
        app.require(len(matches) == 1 and self.expected_tag is not None and
                    matches[0].get('tag_name') == self.expected_tag, 'The original App Release changed or is missing')
        return releases

    def create_release(self, body):
        raise app.PublicationError('Recovery cannot create a new Release')

    def upload(self, release_id, data):
        app.require(release_id == self.expected_release_id, 'Recovery refused another Release ID')
        return super().upload(release_id, data)

    def publish_release(self, release_id, title):
        app.require(release_id == self.expected_release_id, 'Recovery refused another Release ID')
        return super().publish_release(release_id, title)

    def rename(self, release_id, title):
        raise app.PublicationError('Recovery cannot modify another Release')


def positive_env(name):
    value = os.environ.get(name, '')
    app.require(re.fullmatch(r'[1-9][0-9]{0,15}', value) is not None, 'Invalid recovery input')
    return int(value)


def main():
    app.require(os.environ.get('GITHUB_REPOSITORY') == app.REPOSITORY and
                os.environ.get('GITHUB_EVENT_NAME') == 'workflow_dispatch' and
                os.environ.get('GITHUB_REF') == 'refs/heads/' + CONTROLLER_BRANCH and
                os.environ.get('GITHUB_ACTOR', '').casefold() == OWNER and
                os.environ.get('GITHUB_TRIGGERING_ACTOR', '').casefold() == OWNER,
                'Unexpected recovery authority')
    app.require(len(sys.argv) in (2, 5) and sys.argv[1] in ('preflight', 'publish'), 'Invalid recovery command')
    run_id, attempt = positive_env('RETRY_RUN_ID'), positive_env('RETRY_ATTEMPT')
    artifact_id, release_id = positive_env('RETRY_ARTIFACT_ID'), positive_env('RETRY_RELEASE_ID')
    api = ExistingReleaseGateway(release_id)
    run = api._call('GET', api.base + f'/actions/runs/{run_id}/attempts/{attempt}')
    artifact = api._call('GET', api.base + f'/actions/artifacts/{artifact_id}')
    jobs = api._call('GET', api.base + f'/actions/runs/{run_id}/attempts/{attempt}/jobs?per_page=100')
    validate_origin(run, artifact, jobs, run_id, attempt, artifact_id)
    existing = api.release(release_id)
    app.require(existing.get('id') == release_id and type(existing.get('draft')) is bool and
                existing.get('prerelease') is True, 'The original App Release is unavailable')
    if sys.argv[1] == 'preflight':
        app.require(len(sys.argv) == 2, 'Invalid recovery preflight')
        print('Original failed run, successful build and existing App Release verified.')
        return
    app.require(len(sys.argv) == 5, 'Recovery needs the original three build files')
    setup_path, package_path, identity_path = (Path(value) for value in sys.argv[2:])
    for path, maximum in ((setup_path, 200 * 1024 * 1024), (package_path, 160 * 1024 * 1024),
                          (identity_path, 1024 * 1024)):
        app.require(path.is_file() and not path.is_symlink() and 0 < path.stat().st_size <= maximum,
                    'Original build file is unavailable')
    identity = json.loads(identity_path.read_text(encoding='utf-8-sig'), object_pairs_hook=app._unique_object)
    payload, setup = package_path.read_bytes(), setup_path.read_bytes()
    ctx = recovery_context(identity, payload, setup, run_id, attempt)
    api.expected_tag = app._context(ctx)
    app.require(existing.get('tag_name') == api.expected_tag and existing.get('target_commitish') == ctx['sourceSha'],
                'The original App Release differs from the build')
    print('Existing Dev Manager Release:', app.publish(ctx, payload, setup, api))


if __name__ == '__main__':
    try:
        main()
    except (app.PublicationError, OSError, ValueError) as exc:
        print('Dev Manager recovery stopped:', str(exc) if isinstance(exc, app.PublicationError) else 'Invalid recovery data', file=sys.stderr)
        sys.exit(1)

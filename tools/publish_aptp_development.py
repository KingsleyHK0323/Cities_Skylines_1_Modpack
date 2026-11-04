"""Verify APTP compile provenance before invoking the branch publisher.

Signature: OpenAI-Codex
"""

import json
import os
from pathlib import Path
import re
import subprocess
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / '.github' / 'scripts'))
from Release_Provenance import REPOSITORY, positive_decimal, verify_provenance
import dev_build_publisher as publisher


def failure_message(error):
    """Expose only a fixed safe category and optional numeric HTTP status."""
    safe_reasons = {
        'Artifact checksum mismatch', 'Artifact file list differs from allowlist',
        'Artifact manifest blob mismatch', 'Artifact manifest is unavailable',
        'Artifact tree is incomplete', 'Build order cannot be verified',
        'Existing artifact branch identity differs', 'Existing build file would be overwritten',
        'Existing build identity has different contents', 'GitHub returned invalid data',
        'Invalid created commit', 'Invalid created tree', 'Source branch moved',
        'Source branch moved before publication', 'Source is older or divergent',
        'Uploaded blob checksum mismatch',
    }
    if isinstance(error, publisher.PublicationError):
        message = str(error)
        api_error = re.fullmatch(
            r'GitHub API (?:read|write-blob|write-tree|write-commit|create-ref|update-ref|other) '
            r'failed \(HTTP (?:unknown|[1-5][0-9]{2})\)', message)
        if message in safe_reasons or api_error:
            return 'APTP development publication failed: ' + message
    return 'APTP artifact provenance or publication verification failed.'


def main():
    if len(sys.argv) != 2:
        raise ValueError('Expected one artifact directory')
    repository = os.environ.get('GITHUB_REPOSITORY')
    if repository != REPOSITORY:
        raise ValueError('Unexpected repository')
    build_attempt = positive_decimal(os.environ.get('RELEASE_BUILD_ATTEMPT'))
    workflow_attempt = positive_decimal(os.environ.get('GITHUB_RUN_ATTEMPT'))
    publisher.validate_workflow_attempt(int(build_attempt), int(workflow_attempt))
    artifact_id = positive_decimal(os.environ.get('RELEASE_ARTIFACT_ID'))
    metadata = (Path(sys.argv[1]) / 'build-info.txt').read_text(encoding='utf-8-sig')
    artifact = json.loads(subprocess.check_output(
        ['gh', 'api', f'repos/{repository}/actions/artifacts/{artifact_id}'],
        text=True, stderr=subprocess.PIPE))
    if not isinstance(artifact, dict):
        raise ValueError('Invalid artifact response')
    verify_provenance(metadata, artifact, repository=repository,
                      ref=os.environ.get('GITHUB_REF'), sha=os.environ.get('GITHUB_SHA'),
                      run_id=os.environ.get('GITHUB_RUN_ID'), build_attempt=build_attempt,
                      artifact_id=artifact_id)
    publisher.main()


def entrypoint():
    try:
        main()
    except Exception as error:
        print(failure_message(error), file=sys.stderr)
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(entrypoint())

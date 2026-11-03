"""Verify APTP compile provenance before invoking the branch publisher.

Signature: OpenAI-Codex
"""

import json
import os
from pathlib import Path
import subprocess
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / '.github' / 'scripts'))
from Release_Provenance import REPOSITORY, positive_decimal, verify_provenance
import dev_build_publisher as publisher


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
        ['gh', 'api', f'repos/{repository}/actions/artifacts/{artifact_id}'], text=True))
    verify_provenance(metadata, artifact, repository=repository,
                      ref=os.environ.get('GITHUB_REF'), sha=os.environ.get('GITHUB_SHA'),
                      run_id=os.environ.get('GITHUB_RUN_ID'), build_attempt=build_attempt,
                      artifact_id=artifact_id)
    publisher.main()


if __name__ == '__main__':
    try:
        main()
    except (ValueError, OSError, subprocess.CalledProcessError, publisher.PublicationError):
        print('APTP artifact provenance or publication verification failed.', file=sys.stderr)
        sys.exit(1)

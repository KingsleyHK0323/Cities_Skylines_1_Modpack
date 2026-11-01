# Signature: OpenAI-Codex_Automated_Public_Transport_Planning
"""Pure checks binding APTP public files to the compile job's immutable artifact."""

REPOSITORY = 'KingsleyHK0323/Cities_Skylines_1_Modpack'
BRANCH = 'Automated_Public_Transport_Planning'
SHARED_REFS = '7b8a5e7c10f2db84388e8dd8af2084f239957b13'


def positive_decimal(value):
    value = str(value)
    if not value or any(c not in '0123456789' for c in value) or int(value) <= 0:
        raise ValueError('Missing or invalid compile identity')
    return str(int(value))


def verify_provenance(metadata, artifact, *, repository, ref, sha, run_id, build_attempt, artifact_id):
    """Reject before draft creation; publish-job retry numbers do not identify a build."""
    run_id = positive_decimal(run_id)
    build_attempt = positive_decimal(build_attempt)
    artifact_id = positive_decimal(artifact_id)
    if repository != REPOSITORY or ref != 'refs/heads/' + BRANCH:
        raise ValueError('Publisher is outside the APTP repository/branch')
    fields = {}
    for line in metadata.lstrip('\ufeff').splitlines():
        if '=' not in line:
            continue
        key, value = line.split('=', 1)
        if key in fields:
            raise ValueError('Duplicate build-info field')
        fields[key] = value
    expected = dict(sourceSHA=sha, sharedrefsSHA=SHARED_REFS, runid=run_id,
                    runattempt=build_attempt, DevTools='true', repository=repository, ref=ref)
    if any(fields.get(key) != value for key, value in expected.items()):
        raise ValueError('Build-info does not match the producing compile job')
    run = artifact.get('workflow_run') or {}
    if (positive_decimal(artifact.get('id')) != artifact_id or
            artifact.get('name') != f'aptp-public-assets-{run_id}-{build_attempt}' or
            artifact.get('expired') is not False or
            positive_decimal(run.get('id')) != run_id or
            run.get('head_sha') != sha or run.get('head_branch') != BRANCH):
        raise ValueError('Artifact API identity does not match the producing compile job')
    return build_attempt

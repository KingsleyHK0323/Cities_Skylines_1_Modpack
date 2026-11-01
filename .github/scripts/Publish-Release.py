# Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning
import hashlib
import json
import os
from pathlib import Path
import subprocess
# Signature: OpenAI-Codex_Automated_Public_Transport_Planning
from Release_Provenance import positive_decimal, verify_provenance

def gh(*args):
    return subprocess.check_output(['gh', *args], text=True).strip()

root = Path('release-assets')
names = {'AutomatedPublicTransportPlanning.dll', 'README.md', 'LICENSE', 'build-info.txt', 'Steam_Workshop_Link.txt', 'SHA256SUMS'}
assert {p.name for p in root.iterdir()} == names, 'Unexpected asset set'
hashes = {}
sizes = {}
for name in names:
    data = (root / name).read_bytes()
    assert data, name
    sizes[name] = len(data)
    hashes[name] = hashlib.sha256(data).hexdigest()
for line in (root / 'SHA256SUMS').read_text().splitlines():
    digest, name = line.split('  ', 1)
    assert name in names - {'SHA256SUMS'} and hashes[name] == digest
assert len((root / 'SHA256SUMS').read_text().splitlines()) == 5
assert f"sourceSHA={os.environ['GITHUB_SHA']}" in (root / 'build-info.txt').read_text(encoding='utf-8-sig')
repo = os.environ['GITHUB_REPOSITORY']
source_sha = os.environ['GITHUB_SHA']
artifact_id = positive_decimal(os.environ['RELEASE_ARTIFACT_ID'])
artifact = json.loads(gh('api', f'repos/{repo}/actions/artifacts/{artifact_id}'))
build_attempt = verify_provenance(
    (root / 'build-info.txt').read_text(encoding='utf-8-sig'), artifact,
    repository=repo, ref=os.environ['GITHUB_REF'], sha=source_sha,
    run_id=os.environ['GITHUB_RUN_ID'], build_attempt=os.environ['RELEASE_BUILD_ATTEMPT'],
    artifact_id=artifact_id)
tag = f"Automated_Public_Transport_Planning-dev-{os.environ['GITHUB_RUN_ID']}-{build_attempt}"
created = json.loads(gh('api', '--method', 'POST', f'repos/{repo}/releases', '-f', f'tag_name={tag}', '-f', f'target_commitish={source_sha}', '-F', 'draft=true', '-F', 'prerelease=true', '-f', 'make_latest=false', '-f', f'name=Automated Public Transport Planning — Development Build', '-f', 'body=' + (root / 'README.md').read_text(encoding='utf-8-sig')))
release_id = created['id']
gh('release', 'upload', tag, '--repo', repo, *[str(root / n) for n in sorted(names)])
release = json.loads(gh('api', f'repos/{repo}/releases/{release_id}'))
assert release['draft'] and release['prerelease']
assert len(release['assets']) == len(names)
for asset in release['assets']:
    name = asset['name']
    assert name in names and asset['size'] == sizes[name]
    assert asset['digest'] == 'sha256:' + hashes[name], f'Uploaded digest mismatch: {name}'
    print(f"{name} {sizes[name]} sha256:{hashes[name]}")
gh('release', 'edit', tag, '--repo', repo, '--draft=false', '--prerelease', '--latest=false')
release = json.loads(gh('api', f'repos/{repo}/releases/{release_id}'))
assert not release['draft'] and release['prerelease']
print(release['html_url'])

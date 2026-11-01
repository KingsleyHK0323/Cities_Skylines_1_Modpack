"""Read-only source inventory checks; not gameplay verification.

Signature: OpenAI-Codex_AI_Improve.
"""
import hashlib
import json
import re
import subprocess
import sys
from pathlib import Path, PurePosixPath

PILOT = {
    'fire-dispatch': ('Realistic City First Response',
                      {'FireResponseCapEnabled', 'FireIdleSeekEnabled',
                       'FireMaxRespondersPerBuilding', 'FireUncapAfterMinutes'},
                      {'FireTruckAI.SetTarget', 'FireCopterAI.SetTarget'},
                      'FireResponseCapPatch.cs'),
    'transit-dwell': ('Automated Public Transport Vehicle Management',
                      {'TransitDwellShortenEnabled', 'TransitUnbunchEnabled'},
                      {'BusAI.CanLeave', 'TrolleybusAI.CanLeave',
                       'PassengerTrainAI.CanLeave'}, 'TransitDwellShortenPatch.cs'),
}


class Invalid(ValueError):
    pass


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise Invalid('duplicate property: ' + key)
        result[key] = value
    return result


def load(path):
    return json.loads(path.read_text(encoding='utf-8-sig'),
                      object_pairs_hook=unique_object)


def require_inside(path, root):
    try:
        path.resolve(strict=True).relative_to(root.resolve(strict=True))
    except (ValueError, OSError, RuntimeError) as exc:
        raise Invalid('source resolves outside checkout or is unavailable') from exc


def source_path(root, name):
    if not isinstance(name, str) or not name or '\\' in name or ':' in name:
        raise Invalid('invalid relative source path')
    relative = PurePosixPath(name)
    if relative.is_absolute() or '..' in relative.parts or str(relative) != name:
        raise Invalid('invalid relative source path')
    path = root / name
    require_inside(path, root)
    if not path.is_file():
        raise Invalid('source is not a file: ' + name)
    return path


def fields(obj, expected):
    if not isinstance(obj, dict) or set(obj) != set(expected.split()):
        raise Invalid('missing or unknown fields')


def text(value):
    if not isinstance(value, str) or not value.strip():
        raise Invalid('expected nonempty text')


def text_list(value):
    if not isinstance(value, list) or not value:
        raise Invalid('expected nonempty text list')
    for item in value:
        text(item)
    if len(value) != len(set(value)):
        raise Invalid('duplicate list entry')


def git(root, *args):
    command = ['git', '-c', 'safe.directory=' + root.as_posix(),
               '-C', str(root), *args]
    try:
        return subprocess.run(command, check=True, stdout=subprocess.PIPE,
                              stderr=subprocess.PIPE).stdout
    except (OSError, subprocess.CalledProcessError) as exc:
        raise Invalid('baseline cannot be verified in local Git history') from exc


def validate(data, root):
    root = root.resolve(strict=True)
    fields(data, 'schemaVersion sourceBaseline signature responsibilityNotes sources features')
    if type(data['schemaVersion']) is not int or data['schemaVersion'] != 1:
        raise Invalid('unsupported schemaVersion')
    text(data['signature'])
    text_list(data['responsibilityNotes'])
    baseline = data['sourceBaseline']
    if not isinstance(baseline, str) or not re.fullmatch('[0-9a-f]{40}', baseline):
        raise Invalid('sourceBaseline must be a full commit SHA')
    if git(root, 'rev-parse', baseline + '^{commit}').decode().strip() != baseline:
        raise Invalid('sourceBaseline does not identify a commit')
    if not isinstance(data['sources'], list) or not data['sources']:
        raise Invalid('missing sources')
    sources = {}
    for entry in data['sources']:
        fields(entry, 'path sha256')
        name, digest = entry['path'], entry['sha256']
        path = source_path(root, name)
        if name in sources:
            raise Invalid('duplicate source: ' + name)
        if not isinstance(digest, str) or not re.fullmatch('[0-9a-f]{64}', digest):
            raise Invalid('invalid sha256')
        historical = git(root, 'show', baseline + ':' + name)
        # Normalize UTF-8 BOM and checkout line endings, not source text.
        canonical = lambda raw: raw.decode('utf-8-sig').replace('\r\n', '\n').encode('utf-8')
        if hashlib.sha256(canonical(historical)).hexdigest() != digest:
            raise Invalid('digest does not match baseline: ' + name)
        current = path.read_bytes()
        if hashlib.sha256(canonical(current)).hexdigest() != digest:
            raise Invalid('stale source: ' + name)
        sources[name] = current.decode('utf-8-sig')
    required = {'ModSettings.cs', 'Patcher.cs', 'FireResponseCapPatch.cs',
                'FireResponseTracker.cs', 'TransitDwellShortenPatch.cs',
                'CompanionModCompat.cs'}
    if set(sources) != required:
        raise Invalid('pilot requires the six reviewed source files')
    if 'ModSettings.cs' not in sources:
        raise Invalid('settings source must be pinned')
    keys = set(re.findall(r'new\s+Saved(?:Bool|Int|String)\s*\(\s*"([^"]+)"',
                          sources['ModSettings.cs']))
    if not isinstance(data['features'], list) or not data['features']:
        raise Invalid('missing features')
    seen = set()
    for feature in data['features']:
        fields(feature, 'featureId sourceHook signature patchKind declaredSettings '
               'branchConditions simulationWriteTargets privateCleanup candidateOwner '
               'ownerStatus fallbackSemantics evidencePath')
        for name in ('featureId', 'signature', 'candidateOwner', 'fallbackSemantics'):
            text(feature[name])
        identifier = feature['featureId']
        if identifier in seen:
            raise Invalid('duplicate featureId: ' + identifier)
        seen.add(identifier)
        if feature['ownerStatus'] != 'target-confirmed-not-migrated' or feature['patchKind'] != 'Prefix':
            raise Invalid('only confirmed targets without migration are supported')
        for name in ('sourceHook', 'declaredSettings', 'branchConditions',
                     'simulationWriteTargets', 'privateCleanup'):
            text_list(feature[name])
        if identifier not in PILOT:
            raise Invalid('feature outside reviewed pilot')
        owner, settings, hooks, source = PILOT[identifier]
        if (feature['candidateOwner'] != owner or
                set(feature['declaredSettings']) != settings or
                set(feature['sourceHook']) != hooks or feature['evidencePath'] != source):
            raise Invalid('pilot ownership, settings or hooks differ from reviewed contract')
        if not set(feature['declaredSettings']) <= keys:
            raise Invalid('unknown persisted setting')
        evidence = feature['evidencePath']
        if not isinstance(evidence, str) or evidence not in sources:
            raise Invalid('evidence source must be pinned')
        # Text presence locates evidence; it does not prove control flow.
        if 'Prefix(' not in sources[evidence]:
            raise Invalid('Prefix evidence missing')
        if 'Patcher.cs' not in sources:
            raise Invalid('registration source must be pinned')
        for hook in feature['sourceHook']:
            if not re.fullmatch(r'[A-Za-z]+AI\.(SetTarget|CanLeave)', hook):
                raise Invalid('unsupported hook locator')
            owner, method = hook.split('.')
            if owner not in sources['Patcher.cs'] or method not in sources['Patcher.cs']:
                raise Invalid('registration text missing: ' + hook)
    if seen != set(PILOT):
        raise Invalid('both reviewed pilot domains are required')


def main():
    root = Path(__file__).resolve().parents[2]
    try:
        validate(load(root / 'tools/migration/feature-boundaries.json'), root)
    except (Invalid, OSError, ValueError, TypeError, KeyError) as exc:
        print('INVALID: ' + str(exc), file=sys.stderr)
        return 1
    print('Source inventory valid; runtime attachment and migration remain unverified.')
    return 0


if __name__ == '__main__':
    sys.exit(main())

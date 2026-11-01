"""Boundary metadata contract tests. Signature: OpenAI-Codex_AI_Improve."""
import copy
import json
import unittest
from unittest.mock import patch
from pathlib import Path

import check_feature_boundaries as checker


class BoundaryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.root = Path(__file__).resolve().parents[2]
        cls.data = checker.load(cls.root / 'tools/migration/feature-boundaries.json')

    def test_current_sources(self):
        checker.validate(self.data, self.root)

    def test_invalid_contracts(self):
        cases = [
            lambda d: d.update(schemaVersion=99),
            lambda d: d.update(sourceBaseline='invalid'),
            lambda d: d.update(sourceBaseline='0' * 40),
            lambda d: d['features'].append(copy.deepcopy(d['features'][0])),
            lambda d: d['features'][0].update(ownerStatus='approved'),
            lambda d: d['features'][0].update(candidateOwner='Unconfirmed Owner'),
            lambda d: d['features'][0].update(sourceHook=['TramAI.CanLeave']),
            lambda d: d['features'][0].update(declaredSettings=['FireIdleSeekEnabled']),
            lambda d: d['features'][0].update(runtimeEnabled=True),
            lambda d: d['features'][0].update(fallbackSemantics=''),
            lambda d: d['features'][0].update(declaredSettings=['UnknownSetting']),
            lambda d: d['sources'][0].update(path='../outside.cs'),
            lambda d: d['sources'][0].update(path='C:/outside.cs'),
            lambda d: d['sources'][0].update(sha256='0' * 64),
            lambda d: d['features'][0].update(evidencePath='Untracked.cs'),
        ]
        for mutate in cases:
            with self.subTest(case=cases.index(mutate)):
                data = copy.deepcopy(self.data)
                mutate(data)
                with self.assertRaises(checker.Invalid):
                    checker.validate(data, self.root)

    def test_duplicate_properties(self):
        with self.assertRaises(checker.Invalid):
            json.loads('{"a":1,"a":2}', object_pairs_hook=checker.unique_object)

    def test_nested_duplicate_properties(self):
        with self.assertRaisesRegex(checker.Invalid, 'duplicate property'):
            json.loads('{"source":{"path":"a","path":"b"}}',
                       object_pairs_hook=checker.unique_object)

    def test_bom_crlf_source_equivalence(self):
        # Exercise the actual validator with alternate checkout bytes, while
        # baseline Git blobs and the reviewed metadata remain unchanged.
        original = Path.read_bytes

        def checkout_bytes(path):
            raw = original(path)
            normalized = raw.decode('utf-8-sig').replace('\r\n', '\n')
            return b'\xef\xbb\xbf' + normalized.replace('\n', '\r\n').encode('utf-8')

        with patch.object(Path, 'read_bytes', checkout_bytes):
            checker.validate(self.data, self.root)

    def test_source_text_change_is_not_normalized_away(self):
        original = Path.read_bytes

        def changed_bytes(path):
            return original(path) + b'\n// changed source\n'

        with patch.object(Path, 'read_bytes', changed_bytes):
            with self.assertRaisesRegex(checker.Invalid, 'stale source'):
                checker.validate(self.data, self.root)

    def test_resolved_escape(self):
        # No symlink creation or privilege requirement: supply an existing
        # resolved target outside the checkout to the containment predicate.
        with self.assertRaises(checker.Invalid):
            checker.require_inside(self.root.parent, self.root)

    def test_stale_working_source(self):
        with patch.object(Path, 'read_bytes', return_value=b'changed source'):
            with self.assertRaisesRegex(checker.Invalid, 'stale source'):
                checker.validate(self.data, self.root)

    def test_resolved_link_escape(self):
        # Model resolve() returning a link target outside the root without
        # creating links or depending on Windows symlink privileges.
        original = Path.resolve
        linked = self.root / 'FireResponseCapPatch.cs'

        def resolve(path, strict=False):
            if path == linked:
                return self.root.parent / 'outside.cs'
            return original(path, strict=strict)

        with patch.object(Path, 'resolve', resolve):
            with self.assertRaisesRegex(checker.Invalid, 'outside checkout'):
                checker.validate(self.data, self.root)


if __name__ == '__main__':
    unittest.main()

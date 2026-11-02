# Development bundle collector

Signature: OpenAI-Codex

The default `Modpack_Website` branch owns the central schedule. It runs at
00:00, 05:00 and 12:00 UTC (08:00, 13:00 and 20:00 Hong Kong time), and supports
manual dispatch. GitHub may delay scheduled runs.

The collector does not compile or execute module source. It resolves the three
selected branch tips and requires the latest push build for each exact source
to have completed successfully. AI Improve and Planning use durable published
release assets tied to the successful run and attempt. Vehicle Management uses
its exact run/attempt artifact; after its 14-day retention expires, a new cloud
build is required. Missing or expired outputs abort publication, without using
older sources or omitting a module.

The allowlist, source/run/attempt metadata and complete checksums are verified.
AI Improve and its runtime dependency remain together in `AI_Improve/`.
Standalone module DLLs are at the ZIP root; their instructions, licenses and
provenance are under `Documentation/<branch>/`. Game reference assemblies,
test executables, PDBs and arbitrary extra files are excluded.

Published source identities include module commits and the collector/workflow
hashes. Unchanged identities skip publication, including after artifact expiry.
Changed inputs require complete verification again. Branch movement during
collection aborts the run.

Publication first creates a draft, downloads and hashes its uploaded ZIP, then
publishes a prerelease. A failed draft is retained for inspection and blocks an
automatic duplicate retry. No existing assets, releases or tags are deleted.
Only older titles in the `modpack-dev-` channel are marked `[HISTORICAL]` after
the new publication succeeds. Individual module release titles are untouched.
`[LATEST DEV]` is a channel title marker, not GitHub's repository-wide Latest badge.

Cloud contract tests cover unsafe ZIP entries, missing dependencies, unexpected
reference DLLs, expired artifacts and mismatched hashes/build attempts. Run with
`python3 -B -m unittest discover -s tools/modpack-release -p 'test_bundle.py' -v`.
These tests and the collector do not establish game compatibility or stability.

This directory is repository automation code governed by the repository's
existing license. This change does not alter licensing of the site or modules.

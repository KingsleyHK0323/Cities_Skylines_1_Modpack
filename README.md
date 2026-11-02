# Cities Skylines 1 Modpack Dev Manager

Dependency-free local prototype: package preflight and an isolated filesystem
executor. Run `node --test core.test.mjs filesystem.test.mjs` using an installed
Node.js runtime. The filesystem tests create synthetic temporary directories.

This is not yet a desktop application. There is no network downloader, archive
extractor, authenticated latest catalog, assembly inspector or game-process detector.
Tests use synthetic bytes, not real module DLLs. A matching SHA-256 proves that
bytes match the supplied contract; it does not authenticate a publisher or module.

`IsolatedInstaller` supports actual install/update/uninstall operations only in
directories immediately under the current OS temporary directory named
`cs1-dev-manager-test-<random suffix>`. Other roots are rejected before metadata
creation. It cannot currently be pointed at a real game or Steam installation.

Each module has a dedicated directory. Canonical module IDs are lowercase ASCII
letters, digits, underscores and hyphens; Windows device names are rejected.
Packages contain nonempty buffers with expected SHA-256 values. Limits are 256
files, 32 MiB per file and 64 MiB per package. Parent/child file collisions,
case-insensitive duplicate paths, reserved names and alternate streams are refused.

The executor stages and verifies all files, rechecks the old installation, then
renames directories and atomically replaces its ownership record. An advisory
exclusive lock prevents cooperating operations from running concurrently. Failed
pre-commit operations restore the old active directory where possible. Modified,
extra, missing, hard-linked or linked files block update/uninstall. Uninstall moves
the verified module out of its active location into retained backups; it does not
permanently erase those bytes. Failed activation files are retained as well.

If rollback cannot safely restore a directory, a journal and backup remain and
future mutations refuse to proceed. There is no automatic crash recovery or backup
purge yet. A commit that succeeded but failed final housekeeping reports an error
with `committed=true`; callers must inspect state instead of retrying blindly.
Transaction failures use `AggregateError.errors` to retain original operation,
recovery and lock-release errors without modifying them. `lockReleaseFailed`
identifies failed lock validation or removal; `recoveryRequired` identifies a
retained or potentially partial journal. A release failure does not change the recorded commit outcome.
Failed lock acquisition reports `lockAcquisitionFailed` and `lockMayRemain`;
successful exclusive creation followed by write/sync failure requires manual
inspection (`recoveryRequired=true`). Partial metadata is retained without
additional reads or attempted removal. Existing foreign locks are never removed.
Diagnostic reporting makes no additional writes to a potentially unsafe root.

Path-component, directory identity and opened-file checks reject observed symlink,
junction and hard-link substitution. Tests include injected junction changes before
activation and after backup. These JavaScript path checks do **not** establish an
atomic Windows handle-relative security boundary against a hostile concurrent
process. Other Windows reparse-point types, power-loss durability and production
permissions need native validation before real game installation is enabled.

Stable/development coexistence produces only a warning; no subscription,
enablement or loading change is authorized by detection. Bulk execution, graphical
progress, actual game-root integration and real download/installation acceptance
remain future work. See [source contract assessment](SOURCE-CONTRACT.md).

Code and original documentation use [PolyForm Noncommercial 1.0.0](LICENSE).
Copyright Kingsley. This is source-available software with noncommercial
restrictions. The unmodified official text was obtained from the PolyForm Project
[versioned repository](https://github.com/polyformproject/polyform-licenses/blob/1.0.0/PolyForm-Noncommercial-1.0.0.md).
The upstream LICENSE is kept unmodified; authorship of this implementation is
recorded here and in source files. No dependency was installed, and no commit,
push, public release or Workshop operation has been performed by this session.

Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

The `Dev Manager checks` workflow is configured to run the isolated Node.js tests
on GitHub's Windows runner for this project's own branch. It does not install
dependencies, publish a release or access game files. Its cloud result has not
been verified before the first branch push. Signature: OpenAI-Codex.

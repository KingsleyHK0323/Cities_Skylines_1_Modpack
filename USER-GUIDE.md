# Dev Manager user guide

## Start

The selected GitHub Release distribution format is a Windows installer,
`Setup.exe`. It installs the application into an empty folder you choose.
Each new version uses a separate empty folder instead of overwriting an existing
installation. After installation, open `DevManager.exe` on Windows x64 with
.NET Framework. The application includes its Node runtime; no separate Node
installation, development toolchain or GitHub login is needed.
Future packages from the current recipe require the pinned official Node.js
v24.22.2 runtime. Existing local portable packages are not automatically upgraded
and must not be confused with a newly verified runtime package.

Destination settings belong to the application folder. A new version may require
you to choose your existing Mods folder again in **Destination...**. Application
installation does not install Development modules or establish game compatibility.

Existing portable folders are local development/testing outputs, not the chosen
GitHub Release distribution. Keep their runtime and accompanying files together
if using them locally. Do not distribute developer shells or the older
`desktop/portable` prototype. This guide describes the intended installer flow;
it does not claim a published installer, cloud installation verification or
in-game acceptance.
The [test readiness](TEST-READINESS.md) report separates implemented local behavior
from published source availability and still-pending GUI/game acceptance.

Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

The application checks published sources on opening and about hourly while idle.
It starts with **Independent Development build branches** selected. Missing or
invalid build branches stay unavailable; the application never automatically
switches to a legacy Release or bundle source. Independent Development build
branches are the only module source offered by the interface. GitHub Releases
are reserved for the Dev Manager `Setup.exe`, not module DLLs or a modpack ZIP.
Historical source code remains internal and is not a player download option.
**Check GitHub** requests another check. GitHub may delay anonymous requests;
the details show the next allowed retry. There is no immediate automatic retry.

**Version managed by Dev Manager** refers to the selected destination. A status
stating that no Dev Manager installation is recorded there does not establish
whether the module is installed from another source. The details say that other
sources, including Steam Workshop, are not checked.

Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
**Verification queued** means request allowance is unavailable or the module is
waiting for its turn. Automatic checks rotate modules instead of repeatedly
checking only the first entries. Candidate discovery does not complete installation
checks. If a large batch exhausts the allowance, wait for retry or select a smaller
batch; unverified files are never installed to work around the limit.
When update verification needs more than one allowance window, the App can reuse
verified immutable commit comparisons during the same session. A later attempt
still checks the current source and local installation. A batch can remain queued
if the source changes or the required request allowance is unavailable.
No artifact branch, missing evidence, an API error or a stale observation means
the source is unavailable. This is not an installed-module failure.
An artifact with no matching source branch also remains unavailable. For a failed
shared source snapshot, a known request limit or local cooldown is reported as
queued; other failures remain unavailable. A 403 without limit details is initially
unavailable and can still start a temporary local cooldown. Other checks may also
be queued while waiting for their turn. Retry after checking the connection or the
reported allowance.
Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
If the system clock moves backward, cached candidates are treated as stale and a
new automatic check is scheduled; request limits can delay that check.

The current registry marks 12 modules as **not configured** and AI Gaming as
**development paused**. These labels are shown in the module list and summary.
A verified publisher manifest can establish a pending module's mapping; registry
presence alone never authorizes downloading it. Development pause is displayed
separately from source verification. Paused modules cannot be installed or updated;
if a batch includes one, none of the selected modules are changed. An intact
installation already owned by this manager can still be removed by the user.

“Installed (source matches; verification pending)” means the installed build ID
matches the observed candidate. It does not mean that the candidate has been fully
verified. Signature: OpenAI-Codex

## Choose a destination

Open **Destination...** and select the game's existing local Mods folder.
The default destination is unconfigured: the application does not inspect game
files until you choose a destination. An optional isolated workspace is available
for testing. Test-workspace files are not installed in the game.

The application manages only folders named `CS1DevManager-<module>` directly below
Mods. It does not adopt a manually installed or unknown folder. Ownership records,
staging and retained backups go in the sibling `CS1DevManagerData-<identifier>`
directory outside Mods, so inactive DLLs are not left inside the active module.
Do not edit or move these records while an operation is running.

## Install and update

1. Close Cities: Skylines before changing files.
2. Check the displayed **Independent Development build branches** source. Each
   module needs its own published artifact branch; missing branches remain unavailable.
3. Search for modules. Check individual boxes or use the candidate selection buttons.
   **Select visible** selects every visible row, including unavailable entries;
   the action remains disabled if any selected row cannot perform that action.
    Selections hidden by a search remain checked. The summary shows their count and
    all three operations stay disabled until you clear the search to review every
    selection, or use **Clear selection**. Candidate-selection buttons change only
    visible rows and leave hidden selections unchanged.
4. Click **Install selected** or **Update selected**. That click starts the operation.
5. Read each module's result before starting the game yourself.

An observed candidate can be shown before a destination is selected or while an
installed module's update order is still unconfirmed. This does not authorize an
installation or establish that an update is newer.
A matching build ID does not prove the installed artifact is identical. If its
manifest differs, or its installation record lacks comparable artifact identity,
the status stays unknown and automatic updates are unavailable. Review the status
message and ask the publisher to resolve the source identity before retrying.
The table shows the update as unconfirmed; select the row to see the reason.
Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

The application revalidates selected sources, build provenance, complete required
assets and actual managed assembly names before changing files. An update needs
confirmed forward ancestry or a later build of the same source, plus a later
publication. A different opaque version ID alone is not an update. Older,
divergent or legacy records with unknown order are not automatically selected.

Every selected package is prepared before writes. Transactions then commit one
module at a time. If a later module fails, earlier committed modules remain and
the remaining modules are not attempted. Inspect results rather than assuming
that a whole batch either succeeded or rolled back together.

If another manager instance changes a selected installation after it was
checked, the affected operation stops. Check the latest state before retrying.
The cooperative lock does not prevent unrelated programs from changing files.
Checks cover owned primary file contents; NTFS alternate data streams and their
behavior during backup or removal have not been verified.

Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

## Remove

Select owned modules or use **Select removable**, then click **Remove selected**.
The manager moves each unchanged owned folder to retained backups. It does not
permanently erase backups, uninstall Workshop subscriptions or remove user files.
Unknown, modified, extra, missing or linked files stop the operation for inspection.
Removal can work offline when local ownership is intact.

## Development builds

Development modules can be unstable or fail to work. The retained planning model
can generate stable/development coexistence warnings; the current desktop does
not detect all stable Workshop copies or perform complete compatibility checks.
The application never unsubscribes, disables another module or blocks the game
from loading one. Review duplicates before testing. Installation
success does not establish game loading, compatibility or gameplay acceptance.

Keep the application open until its bounded operation finishes. A recovery journal
or housekeeping warning requires inspection; do not blindly retry. Backups have
no automatic purge, so retained versions consume disk space.

## Language and appearance

Use **Language** at the top of the desktop App to choose English, 繁體中文,
简体中文 or 日本語. Use **Theme** to choose **Follow Windows**, **Light** or
**Dark**. Each change is saved automatically and restored on the next start.
Existing settings without a language use English; the default theme follows
Windows. Windows high-contrast colors take priority for readability.

Following Windows responds to system preference changes while the App is open.
The App's controls, columns, dialogs, statuses, progress and friendly error
messages use the selected language. Module names, paths, version identities and
unrecognized technical diagnostics keep their original spelling. Unknown
diagnostics are accompanied by a translated explanation.

Preferences and the Mods destination share `.desktop-state.json` beside the App.
One backend serializes both changes; a cross-process lock prevents two instances
from overwriting each other's fields. Saves preserve unknown existing data,
validate the file-size limit and replace settings atomically after staging and
backup. Settings reads are bounded as well. A lock can belong to another active
instance or retained recovery data: wait, and inspect it if the condition persists.
The App does not assume that a retained lock is an active instance. A failed save
reports an error and keeps the previous saved state; a post-commit warning explicitly
says that settings were saved and whether a lock may remain. First-start warnings
are shown too. No automatic cleanup removes backups, partial staging or an unsafe
lock, and no automatic recovery is claimed.
Recovery/lock warnings have a separate persistent notice area. They remain visible
after failed operations, selecting a different module and ordinary refreshes.
Only a later verified clean settings transaction can clear the notice; changing
the selection or language does not clear it.

If another App changes the destination, changing a preference in this App stops
with an explanation rather than silently switching destinations. Reconfirm the
destination or restart before retrying. Preference selectors update only the
chosen field, preserving other preference values and unknown settings.

If the saved destination is unavailable or unsafe, the App preserves the saved
selection and explains the issue. Choose a validated existing Mods folder or an
initialized test workspace before installing, updating or removing modules. An
access error is not treated as a missing settings file.
A newly created test workspace is retained and its path reported if initialization
or saving fails; inspect saved settings before retrying.
If previously observed manager metadata disappears, becomes unreadable or changes
identity during a session, operations stop and the files require inspection.
The App does not have a persistent record proving whether a Mods folder had
manager metadata in a previous session. Path checks cannot eliminate every
external replacement race.
During the same session, choosing a destination again or switching away and back
does not discard the previously observed Mods root or metadata presence.
The same session also retains verified test-workspace identities.
If the App could not verify a test workspace on first inspection, it cannot
confirm that workspace later in the same session, even after repair. Choose a
validated game Mods destination instead. The current interface does not offer a
command to create an alternate test workspace; restarting does not prove identity. If Mods manager
metadata cannot be verified, the App does not assume it was absent. A repair can
be rechecked when the original directory identities were observable and match;
otherwise choose another destination. This does not authenticate old metadata contents.
Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

These controls are implemented in the Windows desktop App. Current verification
uses headless native resource/theme probes and isolated backend restart/failure
tests; visual layout, live Windows preference notifications and accessibility
with assistive technology still need GUI acceptance.
Main controls and the custom destination dialog use the selected palette. Native
window title bars and Windows system message-box chrome remain Windows-controlled.

## Privacy and trust

Requests use the configured public GitHub repository and its release-asset CDN.
No token, login, installation path or file contents are uploaded. Standard network
metadata such as your IP address is visible to those services. Local destination
settings remain beside the application in `.desktop-state.json`.

GitHub TLS, repository control, GitHub run metadata and published asset digests
are the trust model. Build-info files are publisher provenance declarations bound
to the selected run, not independent reproducible-build proof. Checked paths and
ownership protect normal operations, but do not claim absolute protection against
a hostile same-user process racing filesystem changes.

Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

# Workshop metadata candidate

This candidate is separate from the disabled multi-module release uploader. At
preparation on 5 November 2026 it was not deployed and had not submitted an
update. It is limited to the existing AI Improve release item `3782858610` for
game `255710`.

The proposed description retains the public API snapshot and appends the
missing Dev Manager and LICENSE links. The owner must review the local files
under `Resource/Workshop_Metadata_Staging_2026-11-05/`. The script checks the
snapshot and proposal SHA-256 values, checks the live description still matches,
and creates a VDF containing only `appid`, `publishedfileid` and `description`.
It does not select a content folder, image, title, tags or visibility.

The candidate expects an open issue authored by the repository owner, with the
exact title and body below. The owner must apply the
`steam-workshop-metadata-approved` label; its most recent label event must be
from the owner. The existing `workshop-release` environment requires its own
reviewer approval. At preparation on 5 November 2026 the label did not exist
and this candidate had not opened an issue.

Title:

```text
[Workshop metadata] AI Improve 3782858610
```

Body:

```text
Update the AI Improve release description only.
Original SHA-256: E5D53622EE0841720F72C254C5FA8C2990685A7EFDE129212B796F46CD640485
Proposed SHA-256: 537A05C2C4672BA4C82371047B58924C3BD2A7D38F468A4F50452BC762390629
No content, title, preview, tags or visibility changes.
```

The runner uses the existing `MODPACK_ROOT`, `STEAMCMD_EXE`,
`STEAMCMD_USERNAME` and `RUNNER_TEMP` configuration; it does not ask for a
password or Steam Guard code. SteamCMD receives only a limited Windows
environment, excluding the GitHub token and other non-allowlisted variables.
A persistent local claim prevents automatic
retries once a submission is attempted. A timeout or a mismatch on the single
post-submit API check is reported as unknown and requires manual inspection.
The live description is checked again immediately before SteamCMD starts, but
there is still a race before Steam accepts the update. The authenticated Steam
account and actual VDF behavior have not
been verified; local tests cannot establish a successful Workshop update.
The generated VDF is rechecked before launch, but a process running as the same
user could still change it before SteamCMD opens it; the runner must be trusted.

The local preview mode does not launch SteamCMD:

```powershell
pwsh -NoProfile -File ./tools/steam-workshop/Invoke-MetadataOnly.ps1 -Mode PREVIEW
```

Do not run this candidate on the self-hosted runner or publish it without the
required authorization. The candidate does not alter the existing binary-release
workflow and does not yet support private-item description reads or other mods.

Signature: OpenAI-Codex.

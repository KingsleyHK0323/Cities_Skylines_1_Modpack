# Change Log

## Unreleased

- Made Companion session disposal single-entry when cancellation and completion race, preserving token-buffer clearing without repeating process cleanup. Added isolated repeated and concurrent disposal checks using an unstarted synthetic process; a concurrent caller does not wait for the first cleanup to finish. Signature: OpenAI-Codex_AI_Gaming.

- Fixed failed-publisher retries to use the successful compile job's immutable artifact ID and build attempt instead of the current publish attempt. Full reruns retain distinct immutable artifacts. Preserved source/reference provenance and checksum checks. Signature: OpenAI-Codex_release_format_audit.
- Resolve newly created Development drafts by their returned release ID, avoiding first-page release-list lookup. Signature: OpenAI-Codex_release_format_audit.


- Integrated the shared settings/in-game request controller, native navigation and full-text viewport. Confirmation rechecks live-view state, rendered preview and request identity at click time; city unload invalidates pending work. Cloud checks and compilation do not establish subscribed-game UI acceptance. Signature: OpenAI-Codex_Responsible: AI_Gaming.

- Started the UI redesign with a text viewport buffer that retains the full original text and avoids splitting Unicode surrogate pairs; the new UI is not yet connected.

- Added minimal Release and detailed Development diagnostic logs with marked daily files. On Windows, a background check removes eligible files older than 48 hours using verified handles; checks occur every 48 hours. Other platforms use the game log without automatic deletion. The manual city snapshot remains separate.
- Recovered cleanup scheduling from invalid or future timestamps without overwriting unmarked state files; Development interop diagnostics now identify the provider using fixed labels.

- Added selectable English, Traditional Chinese, Simplified Chinese, Japanese, and Korean settings UI languages. English is the default; the saved selection applies immediately.
- Adjusted settings layout to reserve space for wrapped text and expanding preview and response fields; updated the Development version description.
- Added a user-configurable size limit for the complete raw Ollama `/api/chat` HTTP response body. It defaults to 0 (unlimited); a configured 1-2047 MiB limit discards the entire oversized response. The limit includes the JSON envelope and does not set generated tokens. `/api/tags` remains limited to 1 MiB.
- Added automatic detection of game API-reported installed-and-available DLC identifiers to each confirmed local AI request. The request preview displays the full list; the request schema is version 4 while the companion transport protocol remains version 1.
- Added optional utility, built-zoning, traffic/transit, and service-status data groups to the confirmed request preview and city snapshot schema version 3.
- Added required `maxResponseBytes` to companion request schema version 4 so the configured response-body limit is enforced before JSON parsing.
- Separated city-capture and AI-response generations so changing preview inputs discards stale captures without dropping a response that is already in progress.
- Clarified request schema version 4 versus city snapshot schema version 3 in all five UI languages.

## 0.4.0 — Early preview

- Added player-confirmed Ollama Local suggestions through an optional companion, with selectable schemaVersion 1 city fields and advice-only responses.
- Hardened companion startup failure handling and timeout reporting.
- Added localized consent disclosure labeling and content-based initial text height.
- Added a direct-dotnet local packaging helper for matching Release or Development companion and module outputs; it does not run a local runner or workflow.
- Clarified that DevTools=true builds are development-only and may be uploaded only to the matching Development Workshop item, never a Release item.

Documentation update signed by OpenAI-Codex.

Documentation update signed by OpenAI-Codex_Responsible: AI Gaming.

Implementation update signed by OpenAI-Codex_Responsible: AI Gaming.

Implementation update signed by OpenAI-Codex.

Implementation update signed by OpenAI-Codex_Responsible: AI Gaming.

Implementation update signed by OpenAI-Codex.

Implementation update signed by OpenAI-Codex_Responsible: AI Gaming.

## Account-link update — 2026-10-31

- Updated active repository, issue and verified Pages URLs to KingsleyHK0323 in module documentation, localized website text and Workshop description templates. Local data-directory identifiers are unchanged. Signature: OpenAI-Codex_Responsible: AI_Gaming.

## Pending cancellation lifecycle verification

- Added an early cancellation guard before Companion startup for already-cancelled queued requests and a compiled-module regression check. Cloud compilation and execution of this change are pending; no gameplay acceptance is claimed. Signature: OpenAI-Codex_Responsible: AI_Gaming.

- Retained verified Development releases as unpublished drafts so cloud cancellation regression validation cannot automatically publish a public version; preserved release-audit retry fixes. Signature: OpenAI-Codex_Responsible: AI_Gaming.

- Disable the module GitHub Release publishing job while retaining cloud
  compilation and checks; Dev Manager distribution is not yet verified.
  Signature: OpenAI-Codex.

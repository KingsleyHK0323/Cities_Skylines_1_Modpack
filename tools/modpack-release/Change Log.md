# Change Log

## 2026-11-02

- Add the three-module development bundle collector and cloud failure contracts.
- Add three daily Hong Kong schedule slots and manual dispatch.
- Require exact-source successful builds, complete payload verification and
  unchanged-source suppression before draft verification and publication.
- Keep module histories independent and label bundle history within its channel.

Signature: OpenAI-Codex

- Fix unpublished draft lookup by retaining the authenticated release ID; add a regression contract. Signature: OpenAI-Codex.

- Accept only GitHub's observed space-to-dot asset-name normalization, preserving the requested display label and ZIP hash verification. Signature: OpenAI-Codex.

- Retain the POST response release ID throughout upload, verification and publication; do not depend on draft list visibility. Signature: OpenAI-Codex.

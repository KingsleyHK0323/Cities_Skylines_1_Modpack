# Development release automation

> **Current distribution:** Module Release publishing is disabled. The earlier prerelease and draft descriptions below are historical; Dev Manager availability is not yet verified.
<!-- Signature: OpenAI-Codex -->

The AI_Gaming branch now builds and publishes its own uniquely tagged Development prerelease on branch pushes or manual dispatch. The read-only build job restores approved encrypted references, publishes the paired self-contained Companion with native libraries bundled, compiles the mod, verifies outputs, and prepares only allowlisted assets. A separate publisher verifies the internal artifact, creates a draft, uploads individual files, checks downloaded hashes and sizes, and publishes it as a prerelease without marking it Latest. Existing releases and assets are never overwritten.

This change does not include the uncommitted UI redesign. Runtime/gameplay acceptance remains pending.

Signature: OpenAI-Codex_Responsible: AI_Gaming

Subsequent releases use `AI Gaming — Development Build` with `AI_Gaming-dev-<Run ID>-<Attempt>` tags and the approved eight-section release-note order. Shared UI changes are validated by cloud checks before publication; prior releases and tags are retained.
Signature: OpenAI-Codex_Responsible: AI_Gaming

## Current draft-only validation boundary

Pushes and manual runs build, test and verify the nine allowlisted assets, then retain an unpublished Development prerelease draft. This supersedes earlier automatic public-publication descriptions. Publishing an existing draft is a separate authorized action; this workflow does not perform it. Immutable artifact identity, original build attempt, source/reference provenance and direct release-ID verification remain enforced. No existing release or tag is modified or recreated.
<!-- Signature: OpenAI-Codex_Responsible: AI_Gaming -->

## Module Release publication disabled

The module Release publisher is disabled, superseding the prior draft-only and
prerelease descriptions. Compilation and checks remain in the workflow; their
one-day Actions artifact does not establish Dev Manager availability. Existing
releases and tags are not changed by this update.

<!-- Signature: OpenAI-Codex -->

# Earlier local packages

The `desktop/portable` directory and `DevManager-dev-*.exe` files are preserved
development outputs. They are not the final current deliverable and must not be
distributed. A historical v24.19.0 runtime cannot be used as the runtime cache for
the current build recipe: `Build-Portable.ps1` accepts only the pinned official
Node.js v24.22.2 Windows x64 binary, including when `RuntimeCachePath` is supplied.
Retain earlier directories and reference files for history; do not treat them as
updated or approved network-test packages. Current builds create a new
`portable-<UTC>-<suffix>` folder and include their own package hash manifest and
user guide. Never overlay builds. The public distribution format is `Setup.exe`;
local portable outputs are development/testing artifacts.

Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

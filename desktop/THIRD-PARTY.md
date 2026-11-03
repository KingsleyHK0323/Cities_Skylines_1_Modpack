# Bundled runtime notices

Future Windows x64 packages built by the current recipe require the unmodified
official Node.js v24.22.2 runtime. The application does not require a separate
system Node installation. This notice describes the intended package contents;
it does not claim a new runtime package or cloud installation was tested locally.
The application's PolyForm Noncommercial license does not replace the runtime's
own license or its bundled third-party notices.

- Official release: https://nodejs.org/en/blog/release/v24.22.2
- Official full license: https://github.com/nodejs/node/blob/v24.22.2/LICENSE
- Official checksums: https://nodejs.org/dist/v24.22.2/SHASUMS256.txt
- Runtime SHA-256 (`win-x64/node.exe`):
  `1c331340fa6cac5e4294725959070a143fa0507195a78ad77eed7943f719aac9`
- Original license SHA-256 (157609 bytes):
  `5888dbb9a1d2b18f2c3e6c5f6af1b39de658372b402a0577b002777f14c62ace`
- Full original license is preserved as `runtime/LICENSE_NODE.txt`, including
  third-party notices. Legal text is not edited or given an implementation signature.
- The Windows Forms executable uses the system .NET Framework; no framework or
  other external package is installed or bundled by this build.
- Old v24.19.0 legal/checksum files, the archived notice and existing local portable
  packages are retained as historical outputs. They do not satisfy the current
  package recipe. No old package is overwritten by changing these source files.
- Official reference files use `-text` Git attributes to preserve their exact
  upstream bytes. This notice uses LF checkout; normalized notice hashes use UTF-8
  text with CRLF and lone CR converted to LF, without trimming or adding a BOM.

Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

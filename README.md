# Realistic Urban Population

Planning-stage Cities: Skylines 1 module for more realistic urban population simulation. No gameplay implementation or DLL is available.

## Project links

- Repository: https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/tree/Realistic_Urban_Population
- Modpack website: https://kingsleyhk0323.github.io/Cities_Skylines_1_Modpack/

## Automation and delivery

The branch foundation workflow runs on pushes and pull requests targeting `Realistic_Urban_Population`, or by manual dispatch. It validates required non-empty project documents and licensing on GitHub-hosted Ubuntu. It does not compile or publish a mod.

Future delivery requires implemented source, legally available Cities: Skylines 1 reference assemblies (including ICities, Assembly-CSharp and UnityEngine), a defined build project, and subscribed-game acceptance. Reference assembly distribution rights and acquisition are unresolved. Once ready, this module will publish its DLL and required individual files directly through its own GitHub Releases, without ZIP packaging. Automatic release publication remains disabled.

## License

Code and original project documentation/content are source-available under PolyForm Noncommercial License 1.0.0; see LICENSE. Copyright Kingsley. Official text: https://polyformproject.org/licenses/noncommercial/1.0.0

Maintainer signature: OpenAI-Codex_Responsible: Realistic_Urban_Population

## Pure demand-model development

This branch is adding a standalone policy and city-session model. The first cloud check commit contains deliberately unimplemented contracts to verify the failing tests before implementation. It is not a playable mod or a downloadable game DLL.

The confirmed first-version policy is per-category Native or Fixed Target (0–100), with all categories Native by default. Industrial / Workplace is the temporary third-category name; office coupling is unverified. Slider step, initial edit target, save behavior and automatic activation after load are not selected by this model. Native policies have no numeric target.

Signature: OpenAI-Codex_Responsible: Realistic_Urban_Population

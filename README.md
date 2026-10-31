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

The standalone policy and city-session model has no game references, game field writes, UI or save adapter. It is not a playable mod or a downloadable game DLL.

The confirmed first-version policy is per-category Native or Fixed Target (0–100), with all categories Native by default. Industrial / Workplace is the temporary third-category name; office coupling is unverified. Slider step, initial edit target, save behavior and automatic activation after load are not selected by this model. Native policies have no numeric target.

Signature: OpenAI-Codex_Responsible: Realistic_Urban_Population

### Model boundaries and cloud checks

- `Source/Demand/DemandPolicy.cs`: immutable category policies; Native passes the current input unchanged; Fixed Target accepts only an explicitly provided integer from 0 through 100. Missing or invalid policies cannot silently become a fixed target.
- `Source/Demand/DemandSession.cs`: one lock covers city-epoch validation, load readiness, permission changes, atomic settings publication, restoration and shutdown. Stale city events cannot affect a new city. Unknown/conflicting permission revokes activation; returning to Allowed does not automatically reactivate it.
- `Checks/`: ten check groups compile the same source with C# 3 syntax on .NET 8, including controlled transition orders and concurrent snapshot reads. This does not prove .NET Framework 3.5 runtime compatibility or real game-thread behavior.
- The `Urban Population Demand Model` workflow runs these checks on GitHub-hosted Ubuntu for this branch, pull requests targeting it, or manual dispatch. The cloud-only command is `dotnet run --project Checks/DemandChecks.csproj --configuration Release`. No test or build is run on the local PC.

`TryApply` is an explicit model command, not a decision about whether a future save adapter will automatically apply settings after loading. The model does not invent a slider step or initial numeric edit value. Restore stops this model's override and passes subsequent inputs; it does not restore old city demand, undo growth or guarantee an immediate display update. Industrial / Workplace and office coupling still require current game-pipeline evidence before any adapter is implemented.

The test-first commit deliberately fails with unimplemented contracts; subsequent checks must be assessed by their exact commit SHA. Passing pure checks must not be described as a successful game build or gameplay acceptance. No automatic release publication or Workshop operation is configured.

Signature: OpenAI-Codex_Responsible: Realistic_Urban_Population

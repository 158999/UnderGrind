# UnderGrind — instructions for coding agents

These instructions apply to the entire repository. Read them before changing code, assets, scenes, tests, packages, or documentation.

## Sources of truth and project identity

1. **Game design:** the canonical design reference is maintained in the companion context repository at [UnderGrind Game Design Document](https://github.com/158999/unity-context/blob/main/Documentation/UnderGrind_Game_Design_Document.md). Read the relevant sections before proposing or implementing a gameplay feature. Do not invent open design decisions or silently turn a prototype into canon.
2. **Architecture and engineering rules:** consult the companion [Systems Guide](https://github.com/158999/unity-context/blob/main/Documentation/Technical/Systems-Guide.md), [Architecture](https://github.com/158999/unity-context/blob/main/Documentation/Technical/Architecture.md), [Developer Guide](https://github.com/158999/unity-context/blob/main/Documentation/Technical/Developer-Guide.md), and [XML documentation standard](https://github.com/158999/unity-context/blob/main/Documentation/Technical/XML-Documentation-Standard.md). These describe intended boundaries and accepted engineering conventions; verify that a referenced type or assembly exists in this repository before relying on it.
3. **Current implementation:** this repository's code, serialized assets, scenes, tests, and documentation are authoritative for what is actually implemented. Never claim a feature is complete because a design document describes it.
4. **Subsystem documentation:** start with the relevant page under `Documentation/` (especially `Player-Locomotion.md`, `Humanoid-Rig-Pipeline.md`, `Player-Foot-Separation-IK.md`, `Inventory.md`, and `Testing.md`) and inspect the corresponding source and tests.

## Verified project baseline

- Unity Editor: **6000.6.9f1** (see `ProjectSettings/ProjectVersion.txt`).
- Render pipeline: URP **17.6.0**.
- Input System: **1.20.1**.
- Unity Test Framework: **1.8.0**.
- Runtime assembly: `Assets/UnderGrind/Runtime/UnderGrind.Runtime.asmdef`.
- Editor tooling: `Assets/UnderGrind/Editor/`.
- Tests: `Assets/UnderGrind/Tests/EditMode/`, `Tests/Editor/`, and `Tests/PlayMode/`.
- Player locomotion is driven by `HumanLocomotionController` and explicit ScriptableObject settings. Preserve the existing controller/settings contracts unless a change is intentional and tested.
- Blender source and export pipeline live under `Assets/UnderGrind/Art/Source/` and `Tools/Blender/`.

Do not downgrade or upgrade Unity/packages as a side effect. Check the package lifecycle and compatibility for Unity 6000.6 before adding packages. Prefer packages already installed and native Unity APIs where they satisfy the requirement.

## Architecture and implementation rules

- Keep domain/game rules separate from Unity presentation and editor adapters. Do not move gameplay rules into a MonoBehaviour merely because the first caller is a Unity component.
- Find the existing owner of a value before adding a new field or manager. Do not create a generic manager, duplicate state, parallel bootstrap, service locator, or competing settings asset when a focused owner already exists.
- Keep classes small and responsibilities explicit. Validate invariants at boundaries; do not silently hide configuration errors behind undocumented fallbacks.
- Use ScriptableObjects/configuration assets for tunable gameplay values. Do not hard-code values that already belong to project settings assets or JSON defaults.
- Preserve the existing Input System action asset and settings-driven bindings. Do not introduce a second input path.
- Avoid per-frame allocations, repeated component lookups in Update/FixedUpdate, and unnecessary Instantiate/Destroy loops. Profile before making speculative performance changes.
- Use stable identifiers for persistent data. Treat GameObjects and Transforms as presentation/runtime references, not authoritative persistent state.
- Money/prices in the canonical design/domain use `decimal`; do not introduce floating-point money calculations.
- Keep public API and non-trivial lifecycle/coordination methods documented with accurate XML comments. Include meaningful parameter constraints, results, exceptions actually thrown, mutations, events, and lifecycle side effects. Do not write comments that merely restate code.
- Follow existing naming, namespaces, assembly definitions, formatting, and serialization conventions. Avoid gratuitous line wrapping or formatting-only churn.

## Character, animation, and Blender asset safety

- Preserve original male/female source meshes and the source `humans.blend`. Work on derived assets; do not replace source art without explicit authorization.
- The established character pipeline expects two separate Humanoid rigs and six mesh parts per character: Body, Head, TP Arms, TP Hands, FP Arms, FP Hands. Read `Documentation/Humanoid-Rig-Pipeline.md` before changing the rig/export contract.
- The Blender exporter validates both characters before writing either FBX. Use `Tools/Blender/run-export.ps1`; existing FBX files are protected by default. Never pass `-Force` unless replacement is explicitly intended and the source validation has passed.
- Animation clips are imported separately from the character FBX. Do not assume a model file contains animation data.
- Foot-separation IK is a bounded correction, not a substitute for fixing source clips. Validate walking, running, crouching, turning, and direction changes in PlayMode; unit tests alone do not prove visual quality.
- Distinguish structural checks (mesh names, bones, weights, Avatar) from actual deformation/visual checks. Report exactly which were performed.

## Testing and evidence

Use the repository's existing orchestrators; do not create parallel test launchers.

- Standalone diagnostics/tests: `./Tools/Testing/run-tests.ps1`.
- Unity EditMode: `./Tools/Testing/run-unity-tests.ps1 -Platform EditMode`.
- Unity PlayMode: `./Tools/Testing/run-unity-tests.ps1 -Platform PlayMode`.
- Both Unity platforms: `./Tools/Testing/run-unity-tests.ps1 -Platform All`.
- Full orchestrated run: `./Tools/Testing/run-all-tests.ps1` (check its parameters before use).
- Blender export validation: `./Tools/Blender/run-export.ps1`.

Report standalone build/tests, Unity compilation, EditMode, PlayMode, Blender export, and visual scene verification as separate evidence. A source test file is not a test run. A green standalone workflow is not proof that Unity compiled. Headless Unity tests are not proof of visual correctness. Never claim visual verification without an actual Game View/editor capture or an observed play session.

Inspect the generated reports and logs under `TestResults/`; preserve existing retention behavior. Do not commit generated test output unless the repository workflow or user explicitly requires it.

## Workflow and change control

- Inspect current branch, working tree/remote state, relevant files, and tests before editing.
- Work on a dedicated feature branch. Do not force-push or rewrite `main`.
- Make the smallest coherent change. Preserve unrelated user changes and existing assets.
- Add or update tests with behavior changes; add PlayMode coverage for input, physics, lifecycle, scene, animation, or UI behavior.
- Update the relevant Italian documentation in the same change. Never edit generated documentation by hand.
- Run the strongest available checks and record unavailable checks explicitly.
- Before concluding, inspect the complete diff for accidental changes to scenes, .meta files, packages, generated files, and binary assets.
- Prefer a pull request to direct pushes to `main`.

## Installed project skills

Use the focused skills under `.codex/skills/` when their trigger matches. Their guidance is subordinate to these repository-specific rules and the canonical GDD. The skill index is in `Documentation/Agent-Skills.md`.

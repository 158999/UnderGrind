# Player implementation review — 2026-11-17

## Scope and collaborators

Cross-checked the current player implementation against:
- Code Assistant review checklist: correctness, edge cases, API usage, tests and separation of responsibilities.
- Blendstra source inspection and project review: exact source mesh hierarchy and modeling limitations.
- PINOC asset metadata: the actual formats available for the six selected motions.
- GitHub Actions: repository state and CI runs.

This is a source and asset review. It is not a Unity visual QA report or a claim that a separate autonomous agent executed Unity.

## Findings

### Critical — source model cannot currently support head-only hiding

The inspected Blendstra source contains two objects: one complete static mesh for Human_Male and one for Human_Female. Neither has an armature, skin weights or a separate head renderer. The PlayerVisualRig tests use a deliberately split test hierarchy and therefore prove only the component's renderer policy, not first-person visibility on the real source meshes.

Required before accepting first person:
1. Produce non-destructive rigged derivatives.
2. Separate the head/face into an addressable renderer (or an equivalent validated submesh/material solution).
3. Preserve the original low-poly source geometry and inspect the result.
4. Add tests/asset checks against the actual imported prefab hierarchy.

No spatially guessed mesh split is accepted as a substitute for inspecting the geometry.

### Important — PINOC formats were previously conflated

The six selected motions expose two separate FBX formats:
- `splat-skeleton-fbx`: armature-only 441-bone MetaHuman skeleton including facial bones; intended for PINOC Splat Engine Generic mode.
- `mixamo-fbx`: X Bot mesh plus Mixamo-named skeleton; intended for DCC retargeting onto other humanoid rigs.

The Mixamo export is the relevant candidate for UnderGrind's conventional humanoid rig. X Bot is a retarget source, not the player body. No selected clip is currently imported or retargeted in this repository. The animation README has been corrected accordingly.

### Important — renderer bindings

`PlayerVisualRig.Configure` previously mixed explicit renderer arrays with name-based discovery. Explicit configuration is now authoritative and disables discovery. A PlayMode test covers this precedence. Name discovery remains only a prototype fallback.

### Important — Animator parameter safety

`PlayerAnimationDriver` previously cached only parameter hashes and parameter count. It now also detects runtime controller swaps and checks parameter types before calling `SetFloat` or `SetBool`.

### Important — clip selection

The test-scene builder previously matched normal walk/idle/run clips using broad token matching, which could select a crouch clip first depending on import order. Standard locomotion lookup now excludes crouch clips; crouch states use both required tokens.

### Remaining controller/visual risks

- First-person head visibility is not functional on the current single-renderer source meshes until the geometry is separated.
- The camera collision sphere cast uses the configured layer mask; the final player prefab must put the player body on a layer excluded from camera collision, or self-hit filtering must be strengthened.
- The current test-scene builder searches animation clips embedded in `humans.blend`; it does not yet import or discover the external PINOC FBX files.
- Run/Walk/Idle thresholds are tuned to the current 1.40 m/s walk and 1.8667 m/s run values, but still require in-Editor playback validation.
- No production Animator Controller or Humanoid Avatar is included in the repository yet.

## Changes made during this review

- Made explicit PlayerVisualRig bindings authoritative.
- Added a PlayMode test for explicit renderer binding precedence.
- Added Animator parameter-type validation and runtime controller-change detection.
- Prevented crouch clips from being selected as normal locomotion clips.
- Corrected PINOC format documentation and clarified the real-mesh limitation in Player-Locomotion.md.
- Recorded the asset and geometry findings in the Blendstra project review history.

## Verification

- GitHub Actions standalone diagnostics on commits before the latest three review commits: success.
- Latest run for the current main HEAD: in progress at the time this report was written; it runs standalone diagnostics only.
- The user's earlier Unity EditMode and PlayMode run passed on the merged baseline. The review commits added afterwards have not yet been run in Unity.
- No Unity Editor visual inspection was performed in this session.
- Blendstra's first source-probe attempt failed with `DUPLICATE_OR_INVALID_OBJECT_ID`; a retry without explicit part IDs is queued, not complete.

## Acceptance gate

Do not mark the player model or first-/third-person visuals complete until the actual male/female prefabs have a verified humanoid rig, separately addressable head/arms/hands, compatible animation clips and successful Unity import/PlayMode/visual checks.

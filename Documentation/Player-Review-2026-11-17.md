# Player implementation review — 2026-11-17

## Scope and collaborators

Cross-checked the current player implementation against:
- Code Assistant review checklist: correctness, edge cases, API usage, tests and separation of responsibilities.
- Blendstra source inspection and project review: exact source mesh hierarchy, modeling limits and the current rigging operation.
- PINOC asset metadata: actual formats available for the six selected motions.
- GitHub Actions: repository state and CI runs.

This is a source and asset review. It is not a Unity visual QA report or a claim that a separate autonomous agent executed Unity.

## Findings

### Critical — original source model cannot support head-only hiding

The inspected Blendstra source contains two objects: one complete static mesh for Human_Male and one for Human_Female. Neither has an armature, skin weights or a separate head renderer. The earlier PlayerVisualRig tests used a deliberately split test hierarchy and therefore proved only the component's renderer policy, not first-person visibility on the real source meshes.

A new non-destructive Blendstra operation is now running against the exact source version. It duplicates both source objects, normalizes only their placement, creates a Unity/Mixamo-recognizable humanoid skeleton per sex, attempts automatic weights, and separates a provisional head renderer. The head split uses an estimated upper-body threshold and is not accepted until geometry and deformation are inspected. The source meshes are not modified.

### Important — PINOC formats are distinct

The six selected motions expose two separate FBX formats:
- `splat-skeleton-fbx`: armature-only 441-bone MetaHuman skeleton including facial bones; intended for PINOC Splat Engine Generic mode.
- `mixamo-fbx`: X Bot mesh plus Mixamo-named skeleton; intended for DCC retargeting onto other humanoid rigs.

The Mixamo export is the relevant retarget candidate for UnderGrind's conventional humanoid rig. X Bot is a retarget source, not the player body. All six selected assets resolve successfully in PINOC; no selected clip is imported or retargeted in this repository yet.

### Renderer bindings

- Explicit PlayerVisualRig bindings are authoritative; Configure disables heuristic name discovery.
- Added ConfigureFromHierarchy for the generated stable naming convention: `*_Head`, `*_FP_Arms`, `*_FP_Hands`.
- Added a PlayMode test covering the generated naming convention, head hiding, first-person arms visibility, and unrelated hair remaining visible.
- This test validates the binding contract on a synthetic hierarchy; it does not validate the actual generated model.

### Animator parameter safety

PlayerAnimationDriver detects runtime Animator Controller swaps and checks parameter types before calling SetFloat or SetBool.

### Clip selection

The test-scene builder excludes crouch clips from normal idle/walk/run/jump matching and requires both crouch tokens for crouch states.

### Remaining controller/visual risks

- The generated head boundary and automatic skin weights require geometry/deformation review.
- The first-person arms/hand renderer is not yet part of the generated source operation; the naming contract is ready, but a dedicated first-person arm mesh/pose remains a task.
- The camera collision sphere cast uses the configured layer mask; the final player prefab must put the player body on a layer excluded from camera collision, or self-hit filtering must be strengthened.
- The current test-scene builder searches animation clips embedded in `humans.blend`; it does not yet import or discover the external PINOC FBX files.
- Run/Walk/Idle thresholds are tuned to the current 1.40 m/s walk and 1.8667 m/s run values, but still require in-Editor playback validation.
- No production Animator Controller or verified Unity Humanoid Avatar is included in the repository yet.

## Changes made during this work

- Made explicit PlayerVisualRig bindings authoritative.
- Added generated hierarchy binding by stable part names.
- Added PlayMode tests for explicit binding precedence and generated hierarchy binding.
- Added Animator parameter-type validation and runtime controller-change detection.
- Prevented crouch clips from being selected as normal locomotion clips.
- Corrected PINOC format documentation and clarified the real-mesh limitation in Player-Locomotion.md.
- Recorded source/asset findings and the rigging attempt in the Blendstra project review history.

## Verification

- GitHub Actions standalone diagnostics for commit `b64df8d`: success.
- GitHub Actions standalone diagnostics for the latest renderer-binding test commit `e90eb26`: queued at the last check.
- The user's earlier Unity EditMode and PlayMode run passed on the merged baseline. The review commits added afterwards have not yet been run in Unity.
- No Unity Editor visual inspection was performed in this session.
- First Blendstra source-probe attempt failed with `DUPLICATE_OR_INVALID_OBJECT_ID`. The corrected operation is running; it has not saved a derivative yet.

## Acceptance gate

Do not mark the player model or first-/third-person visuals complete until the actual male/female prefabs have a verified humanoid rig, separately addressable head/arms/hands, compatible animation clips, successful Unity import, PlayMode checks and visual inspection.

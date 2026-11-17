# Player and humanoid locomotion

## Reference character and motion contract

The male base is the reference player at 1.78 m. The female base is 1.70 m in the current source artifact. Both source meshes are retained as immutable inputs; the rigged derivatives must remain separate from them.

Default locomotion values:
- Walk: 1.40 m/s.
- Run: walk × 4/3 = 1.8667 m/s.
- Jump apex: 0.40 m above take-off under the current tuning.
- Gravity: 9.81 m/s².
- CharacterController height: 1.78 m.
- Capsule radius: 0.28 m.
- Crouch capsule height: 1.12 m.

The jump apex is a gameplay tuning parameter, not a claim of measured human biomechanics.

## Runtime responsibilities

`HumanLocomotionController` owns movement and the CharacterController. It supports keyboard, mouse and gamepad; acceleration/deceleration; buffered jump and coyote time; crouch clearance; and first-/third-person camera. `PlayerVisualRig` owns renderer visibility: head renderers are hidden in first person, explicitly marked first-person-only renderers are hidden in third person, and all other renderers preserve their original enabled state. `PlayerAnimationDriver` writes only parameters actually present in the assigned Animator Controller and disables root motion so the Animator cannot apply a second displacement.

Production prefabs should call `PlayerVisualRig.ConfigureFromHierarchy(rigRoot)` after importing the rig. It recognizes the generated naming convention:
- `*_Head`: hidden in first person and restored in third person.
- `*_FP_Arms` and `*_FP_Hands`: shown in first person and hidden in third person.
- Other renderers remain visible in both modes unless explicitly configured.

If no separate head renderer is found, `ConfigureFromHierarchy` returns false and leaves the existing configuration unchanged. Explicit renderer arrays remain supported through `Configure`. Name-based heuristic discovery is only a prototype fallback.

First person can hide the head only when the head is a separate Renderer or is explicitly assigned as a separate renderer group; it retains body and arm renderers. Third person restores the head and places the camera behind the player with a sphere-cast obstruction check. Important source limitation: the inspected `Human_Male` and `Human_Female` are each a single static mesh object. There is no separate head renderer, armature, or skinning in the original source version. A new Blendstra derivation is being attempted; until its exact saved result is inspected, the source meshes remain the only verified assets and first-person body visibility is not accepted.

## Inputs

| Action | Keyboard / mouse | Gamepad |
|---|---|---|
| Move | WASD / arrows | Left stick |
| Look | Mouse | Right stick |
| Run | Shift | — |
| Jump | Space | South button |
| Crouch | C | East button |
| Toggle camera | F5 | North button |
| Release cursor | Escape | — |
| Re-lock cursor | Left click | — |

## Animator parameters

`PlayerAnimationDriver` can write `Speed` (m/s), `MoveX`, `MoveY`, `Grounded`, `VerticalSpeed`, `Running`, `Crouching`, `Jumping` and `Falling`. It checks the Animator parameter list before writing, so a partially configured controller does not receive missing-parameter writes. The test-scene builder creates matching parameters and idle/walk/run/jump/crouch states when compatible clips and an Avatar are available. It does not fabricate animation clips or an Avatar when the Blender source lacks them.

## Asset pipeline

The selected PINOC library motions are Standing Idle, Walking, Running, Jump, Crouching Idle and Crouch Walk Forward. The library lookup confirms the selected clips are free to take. Their Mixamo FBX exports contain an X Bot mesh and Mixamo-named skeleton, suitable as a DCC retarget source; the X Bot is not the UnderGrind player body. Their separate Splat FBX exports are armature-only 441-bone MetaHuman clips and are intended for the PINOC Splat Engine Generic workflow, not the conventional Unity Humanoid retarget path. Neither family is imported into this repository yet. Confirm the provider's applicable usage terms before redistributing or shipping downloaded exports.

## Verification boundary

The original source meshes remain unchanged. A new Blendstra operation is running against the exact saved source version to create non-destructive male/female rigged derivatives, separate head renderers and automatic skin weights. The head boundary is a first-pass geometric estimate and must be inspected; automatic weights must be reviewed for deformation quality. Operation ID: `idem_4cc1109cda2da5f6371c7a8cb2022087f568b8c51cdac4299081f998b0254613`.

The player code, renderer visibility policy, Animator bridge and Play Mode tests are committed to `main`. The newest renderer-name binding test was added after the earlier baseline tests and is awaiting its CI run. GitHub standalone diagnostics do not substitute for Unity EditMode/PlayMode or visual inspection. No Unity Editor visual inspection was performed in this session.

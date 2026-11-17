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

`HumanLocomotionController` owns movement and the CharacterController. It supports keyboard, mouse and gamepad; acceleration/deceleration; buffered jump and coyote time; crouch clearance; and first-/third-person camera. `PlayerVisualRig` owns renderer visibility: head renderers are hidden in first person, explicitly marked first-person-only renderers are hidden in third person, and all other renderers preserve their original enabled state. Assign renderer arrays explicitly on production prefabs; name discovery is only a fallback. `PlayerAnimationDriver` writes only parameters actually present in the assigned Animator Controller and disables root motion so the Animator cannot apply a second displacement.

First person hides the head renderer (the head must be a separate renderer) while retaining body and arm renderers. Third person restores the head and places the camera behind the player with a sphere-cast obstruction check. This is a first implementation: renderer-name matching is a temporary convention and should become an explicit body-rig visibility component once the final head/eye/face hierarchy is verified.

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

The selected PINOC library motions are Standing Idle, Walking, Running, Jump, Crouching Idle and Crouch Walk Forward. They are free library assets and are armature-only 441-bone MetaHuman skeleton animations with no mesh. They must be retargeted or otherwise mapped onto the final rig; they are not yet imported into this Unity repository. Do not use the bundled X Bot Mixamo mesh as the player body.

## Verification boundary

The current source meshes are still static, unrigged geometry. The earlier Blendstra rig-generation attempt failed during source-object selection and did not save a rigged version; the source asset remains unchanged. PINOC motions remain external library assets and have not been imported or retargeted. The player code, renderer visibility policy, Animator bridge and Play Mode tests are committed to `main`. No Unity Editor was available to me for this turn, so these new changes have not been compiled or executed here. The user's previously reported standalone/EditMode/PlayMode passes apply to the merged baseline, not to commits added after those runs.

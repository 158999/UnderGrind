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

First person can hide the head only when the head is a separate Renderer or is explicitly assigned as a separate renderer group; it retains body and arm renderers. Third person restores the head and places the camera behind the player with a sphere-cast obstruction check. Important source limitation: the inspected `Human_Male` and `Human_Female` are each a single static mesh object. There is no separate head renderer, armature, or skinning in the source version, so `PlayerVisualRig` cannot currently hide only the head on those meshes. The tests prove the renderer policy on a deliberately split test hierarchy, not on the real human assets. A geometry split/rigged derivative is still required before first-person body visibility can be accepted.

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

The current source meshes are still static, unrigged geometry. The earlier Blendstra rig-generation attempts failed before saving a derivative; the source asset remains unchanged. PINOC Mixamo FBX exports contain an X Bot mesh and Mixamo-named skeleton for retargeting; the separate Splat FBX exports are armature-only 441-bone clips. Neither format is imported or retargeted in this repository. The player code, renderer visibility policy, Animator bridge and Play Mode tests are committed to `main`. GitHub Actions has passed the commits present at the time of the latest check, but those checks do not substitute for Unity visual inspection. The user's previously reported standalone/EditMode/PlayMode passes apply to the merged baseline; the newest review fixes still need a fresh run.

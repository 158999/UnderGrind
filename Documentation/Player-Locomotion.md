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

`HumanLocomotionController` owns movement and the CharacterController. It supports keyboard, mouse and gamepad; acceleration/deceleration; buffered jump and coyote time; crouch clearance; first-/third-person camera; and Animator parameter output. Root motion is disabled so the Animator cannot apply a second displacement.

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

The controller writes `Speed` (m/s), `MoveX`, `MoveY`, `Grounded`, `VerticalSpeed`, `Running`, `Crouching`, `Jumping` and `Falling`. Parameters not present on a controller are ignored by Unity's Animator API only if the corresponding parameter exists; before wiring a final controller, verify the declared parameter set. Use a locomotion blend tree for idle/walk/run, a crouch blend tree for crouch idle/walk and transitions driven by grounded/vertical speed.

## Asset pipeline

The selected PINOC library motions are Standing Idle, Walking, Running, Jump, Crouching Idle and Crouch Walk Forward. They are free library assets and are armature-only 441-bone MetaHuman skeleton animations with no mesh. They must be retargeted or otherwise mapped onto the final rig; they are not yet imported into this Unity repository. Do not use the bundled X Bot Mixamo mesh as the player body.

## Verification boundary

The source changes and tests are committed to the feature branch. Unity 6000.6.7f1 has not been launched in this session. Script compilation, asset import, Play Mode, camera occlusion, animation retargeting, hand visibility and visual quality remain unverified. Do not mark them passed until Unity executes the tests and the scene is inspected.

---
name: undergrind-visual-qa
description: Verify visual quality of UnderGrind locomotion, animation, camera, character rigs, inventory UI, and scenes.
---

# UnderGrind visual QA

Read the relevant project page first: `Documentation/Player-Locomotion.md`, `Humanoid-Rig-Pipeline.md`, `Player-Foot-Separation-IK.md`, `Inventory.md`, or `Testing.md`. Inspect the actual scene, prefab, settings asset, and tests.

## Test matrix
- Character scale/orientation, floor contact, camera framing, clipping, and first-/third-person visibility.
- Idle, walk, run, jump, fall, land, crouch idle, crouch walk, acceleration, deceleration, turns, and direction changes.
- Foot crossing and ground contact on source clips and retargeted player avatar.
- Camera collision, pitch limits, sensitivity, cursor lock/unlock, and camera mode switching.
- Inventory open/close, selection, overflow, weight display, pickup/release, keyboard/gamepad navigation, and input restoration.
- UI readability across target resolutions/aspect ratios and focus states.

Run relevant tests first. Capture actual Unity Game View/editor evidence only with an available screenshot tool or connected uLoop MCP. If no capture capability is available, provide reproducible manual steps and label visual verification **NOT RUN**. Compare before/after at the same resolution, camera pose, scene state, and lighting. Record scene/prefab, steps, expected/observed results, and evidence path.

Numerical IK tests, Avatar import checks, and asset structure checks do not prove visual quality. Do not modify source clips or original character meshes as a quick fix without explicit approval.

# Player animation source assets

This folder is reserved for imported animation source files and their Unity-generated `.meta` files.

Expected clip names (case-insensitive matching in the test-scene builder):
- Idle / Standing Idle
- Walk / Walking
- Run / Running
- Jump
- Crouch Idle / Crouching Idle
- Crouch Walk

Do not place the PINOC 441-bone `splat-skeleton-fbx` clips here for the current humanoid body workflow. Those are intended for the PINOC Splat Engine Generic rig and contain no mesh. For a conventional humanoid rig, use a clip source with a compatible humanoid Avatar or retarget clips after the final body rig has been accepted.

The scene builder currently searches clips embedded in `Assets/UnderGrind/Art/Source/humans.blend`. Imported clips in this folder are a planned extension; until that importer is wired, they are not automatically discovered.

## Provenance

PINOC curated library assets selected for evaluation (all taken at zero credits):
- Standing Idle: `1cbf45b5-5abe-4a88-97cd-a1a21da60d7b`
- Walking: `32d71c7f-e88a-4d1f-9c0c-1fde2da5f060`
- Running: `32c1d7ba-29d6-466c-a2bc-56ff17ca4d1a`
- Jump: `08872e23-d695-493d-8c03-cb595cd55e9c`
- Crouching Idle: `7cd56394-0b47-4fa7-b951-6658d5ea3da6`
- Crouch Walk Forward: `e4f5b9e6-381c-4227-88d4-94d6a0615df8`

Their available Mixamo FBX exports include the X Bot mesh and Mixamo-named skeleton for general DCC retargeting. They are not yet included in this repository. Confirm the provider's applicable usage terms before redistributing or shipping any downloaded export.

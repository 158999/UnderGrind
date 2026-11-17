# UnderGrind humanoid rig pipeline

## Source and preservation

The source project is [humans source in Blendstra](https://blendstra.com/studio?workspace=wsp_fcfd842ff566eb960d6d885ff219b4f02b624b23&project=2cdfaa98-3ea2-4a49-892a-b96488e12bc3).

The inspected immutable source version contains:
- `Human_Male`: 1.78 m, 260 source vertices / 486 source triangles.
- `Human_Female`: 1.70 m, 284 source vertices / 529 source triangles.

Both are single static mesh objects without an armature, skin weights or a separate head renderer. The source version is retained unchanged.

## Current derivative operation

A separate project is being used to avoid writing into or replacing the source project:

[UnderGrind — Rigged Human Derivatives](https://blendstra.com/studio?workspace=wsp_fcfd842ff566eb960d6d885ff219b4f02b624b23&project=2493fca7-8542-4d61-8a81-12487e6a3999)

Operation ID: `idem_1c7680c7f652ee2803d5d9f346dfb9b95e600f595b751fde0f0690eace51c3bf`.

The current script attempts to:
1. Duplicate each source mesh and bake its world transform into the derivative.
2. Normalize ground contact and horizontal centering without changing source proportions.
3. Create a separate `UG_Male_Armature` / `UG_Female_Armature` skeleton using Unity/Mixamo-recognizable humanoid bone names.
4. Split a provisional head renderer at 87.5% of character height.
5. Apply Blender automatic weights to body and head renderers.

The head split is a provisional geometric estimate, not an anatomically validated seam. Automatic weights are not accepted until deformation is inspected. The operation has not yet returned a saved result. Do not import or use it as a production character before those checks pass.

## Unity import and prefab builder

Once a reviewed FBX derivative is exported from Blendstra and imported into Unity:

1. Select the FBX asset in the Project window.
2. Run **UnderGrind → Player → Build Prefabs From Selected Rigged FBX**.
3. The editor utility configures the FBX as Unity Humanoid, requests an Avatar from the model, disables optimized GameObject hierarchy, and creates:
   - `Assets/UnderGrind/Generated/Prefabs/Human_Male_Rigged.prefab`
   - `Assets/UnderGrind/Generated/Prefabs/Human_Female_Rigged.prefab`
4. The builder requires valid Unity Humanoid Avatar data and the named roots `UG_Male_Armature` and `UG_Female_Armature`.
5. It binds `*_Head` renderers through `PlayerVisualRig.ConfigureFromHierarchy`.

The generated rig operation does not yet create dedicated camera-space first-person arms/hands. Until that asset exists, the prefab builder warns that first person will show the rigged body rather than a dedicated FP arms mesh. Do not mark the first-person presentation complete.

## Animation sources

Six PINOC library clips were taken successfully at zero credits:
- Standing Idle
- Walking
- Running
- Jump
- Crouching Idle
- Crouch Walk Forward

The default taken format is the armature-only 441-bone MetaHuman skeleton GLB. It has no mesh and is not the conventional Unity Humanoid retarget format. The same assets also expose `mixamo-fbx` (X Bot mesh plus Mixamo-named skeleton), which is the DCC retarget candidate. X Bot is only a retarget source and must not replace the UnderGrind body. The six clips are not yet in the repository or retargeted to the UnderGrind rig. Confirm applicable provider usage terms before redistribution or shipping.

## Acceptance checklist

- [ ] Blendstra derivative saved and exact version inspected.
- [ ] Original male/female topology retained and compared.
- [ ] Head split seam inspected from front, side and oblique views.
- [ ] Automatic weights checked for all major body regions.
- [ ] Unity Humanoid Avatar valid for both rigs.
- [ ] Male and female prefabs generated.
- [ ] Dedicated first-person arms/hands renderer created and posed.
- [ ] PINOC Mixamo clips retargeted and checked for foot sliding/root motion.
- [ ] Play Mode and visual tests pass in Unity.

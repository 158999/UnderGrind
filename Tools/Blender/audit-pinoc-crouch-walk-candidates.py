import bpy
import json
import math
from pathlib import Path

ROOT = Path("Assets/UnderGrind/Animations/Candidates/PINOC_CrouchWalk")
SOURCE_CLIP = Path("Assets/UnderGrind/Animations/Crouch_Walk_Forward.fbx")
OUTPUT = Path("Tools/Blender/pinoc-crouch-walk-candidate-audit.json")
CROSSING_TOLERANCE = 0.02
GENERATION_TASK_ID = "827b8373-42df-479a-a82f-22b5a339678d"

ASSET_IDS = {
    "Crouch_Walk_Forward.fbx": "e4f5b9e6-381c-4227-88d4-94d6a0615df8",
    "Crouch_Walk_Candidate_01.fbx": "6dff3f2b-250b-4506-8712-7e12f3988296",
    "Crouch_Walk_Candidate_02.fbx": "bc1a6551-9608-43eb-9d60-a08c54e4247a",
    "Crouch_Walk_Candidate_03.fbx": "f74b1c97-33c0-462c-a4b9-fb0186d2e278",
    "Crouch_Walk_Candidate_04.fbx": "657d2212-de9b-4524-8bc2-091767afa6a6",
    "Crouch_Walk_Candidate_05.fbx": "40dac395-75f7-4911-b234-97fb94cf548b",
    "Crouch_Walk_Candidate_06.fbx": "58d94313-09b2-4ecd-a404-300f61c9909c",
    "Crouch_Walk_Candidate_07.fbx": "cac937f4-fa78-4905-b124-edddde4612d8",
    "Crouch_Walk_Candidate_08.fbx": "5d550ae7-20c1-43f6-be9a-9ac91bf77bd5",
}


def find_bone(armature, token):
    token = token.lower()
    matches = [bone for bone in armature.pose.bones if token in bone.name.lower()]
    if not matches:
        return None
    return sorted(matches, key=lambda item: (len(item.name), item.name))[0]


def local_position(hips, bone):
    return hips.matrix.inverted() @ bone.matrix.translation


def audit_candidate(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(path), automatic_bone_orientation=False)

    armatures = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
    if len(armatures) != 1:
        raise RuntimeError(f"expected exactly one armature, found {len(armatures)}")

    armature = armatures[0]
    if armature.animation_data is None or armature.animation_data.action is None:
        actions = list(bpy.data.actions)
        if not actions:
            raise RuntimeError("FBX contains no animation action")
        armature.animation_data_create()
        armature.animation_data.action = actions[0]

    action = armature.animation_data.action
    hips = find_bone(armature, "hips")
    left_upper_leg = find_bone(armature, "leftupleg")
    right_upper_leg = find_bone(armature, "rightupleg")
    left_lower_leg = find_bone(armature, "leftleg")
    right_lower_leg = find_bone(armature, "rightleg")
    left_foot = find_bone(armature, "leftfoot")
    right_foot = find_bone(armature, "rightfoot")
    if not all((hips, left_upper_leg, right_upper_leg, left_lower_leg, right_lower_leg, left_foot, right_foot)):
        names = [bone.name for bone in armature.pose.bones]
        raise RuntimeError("required Humanoid bones missing; imported bones: " + ", ".join(names[:80]))

    # Use the Action's actual keyed range, not the FBX scene range. The scene range
    # can contain padding frames that are not part of Unity's imported AnimationClip.
    action_start, action_end = action.frame_range
    start = math.floor(action_start)
    end = math.ceil(action_end)
    scene = bpy.context.scene
    scene.frame_start = start
    scene.frame_end = end

    rest_lateral = (
        armature.data.bones[left_upper_leg.name].head_local
        - armature.data.bones[right_upper_leg.name].head_local
    )
    if rest_lateral.length_squared < 1e-8:
        raise RuntimeError("rest-pose anatomical hip axis is degenerate")
    rest_lateral.normalize()

    frame_samples = list(range(start, end + 1))
    if len(frame_samples) > 600:
        stride = max(1, math.ceil(len(frame_samples) / 600))
        frame_samples = frame_samples[::stride]
        if frame_samples[-1] != end:
            frame_samples.append(end)

    crossed_frames = []
    crossed_knee_frames = []
    min_foot_separation = float("inf")
    min_knee_separation = float("inf")
    min_static_axis_foot_separation = float("inf")
    max_hip_translation_drift = 0.0
    first_local_feet = None
    last_local_feet = None
    first_hip_position = None

    for frame in frame_samples:
        scene.frame_set(frame)
        bpy.context.view_layer.update()

        current_lateral = left_upper_leg.matrix.translation - right_upper_leg.matrix.translation
        if current_lateral.length_squared < 1e-8:
            raise RuntimeError(f"degenerate anatomical hip axis at frame {frame}")
        current_lateral.normalize()

        foot_delta = left_foot.matrix.translation - right_foot.matrix.translation
        foot_separation = foot_delta.dot(current_lateral)
        static_foot_separation = foot_delta.dot(rest_lateral)
        min_foot_separation = min(min_foot_separation, foot_separation)
        min_static_axis_foot_separation = min(min_static_axis_foot_separation, static_foot_separation)
        if foot_separation < -CROSSING_TOLERANCE:
            crossed_frames.append(frame)

        knee_delta = left_lower_leg.matrix.translation - right_lower_leg.matrix.translation
        knee_separation = knee_delta.dot(current_lateral)
        min_knee_separation = min(min_knee_separation, knee_separation)
        if knee_separation < -CROSSING_TOLERANCE:
            crossed_knee_frames.append(frame)

        hips_position = hips.matrix.translation.copy()
        if first_hip_position is None:
            first_hip_position = hips_position.copy()
        max_hip_translation_drift = max(
            max_hip_translation_drift,
            (hips_position - first_hip_position).length
        )

        local_left_foot = local_position(hips, left_foot)
        local_right_foot = local_position(hips, right_foot)
        if first_local_feet is None:
            first_local_feet = (local_left_foot.copy(), local_right_foot.copy())
        last_local_feet = (local_left_foot.copy(), local_right_foot.copy())

    loop_endpoint_error = max(
        (first_local_feet[0] - last_local_feet[0]).length,
        (first_local_feet[1] - last_local_feet[1]).length
    )

    return {
        "file": str(path).replace("\\", "/"),
        "candidateType": "current-source" if path == SOURCE_CLIP else "generated-candidate",
        "assetPublicId": ASSET_IDS.get(path.name),
        "armature": armature.name,
        "action": action.name,
        "actionFrameRange": [action_start, action_end],
        "sampledFrames": len(frame_samples),
        "leftHipBone": left_upper_leg.name,
        "rightHipBone": right_upper_leg.name,
        "leftKneeBone": left_lower_leg.name,
        "rightKneeBone": right_lower_leg.name,
        "leftFootBone": left_foot.name,
        "rightFootBone": right_foot.name,
        "crossingToleranceBlenderUnits": CROSSING_TOLERANCE,
        "rawBlenderFootCrossingFrames": crossed_frames,
        "rawBlenderFootCrossingCount": len(crossed_frames),
        "rawBlenderMinimumAnatomicalFootSeparation": min_foot_separation,
        "rawBlenderMinimumStaticAxisFootSeparation": min_static_axis_foot_separation,
        "rawBlenderKneeCrossingFrames": crossed_knee_frames,
        "rawBlenderKneeCrossingCount": len(crossed_knee_frames),
        "rawBlenderMinimumAnatomicalKneeSeparation": min_knee_separation,
        "rawBlenderHipTranslationDrift": max_hip_translation_drift,
        "rawBlenderLoopEndpointError": loop_endpoint_error,
        "blenderUnitScaleLength": scene.unit_settings.scale_length
    }


def main():
    source_result = None
    if SOURCE_CLIP.is_file():
        source_result = audit_candidate(SOURCE_CLIP)

    results = []
    for path in sorted(ROOT.glob("*.fbx")):
        try:
            results.append({"status": "ok", **audit_candidate(path)})
        except Exception as exc:
            results.append({"status": "failed", "file": str(path), "error": str(exc)})

    valid = [item for item in results if item["status"] == "ok"]
    valid.sort(key=lambda item: (
        item["rawBlenderFootCrossingCount"],
        item["rawBlenderKneeCrossingCount"],
        item["rawBlenderLoopEndpointError"],
        item["rawBlenderHipTranslationDrift"],
        -item["rawBlenderMinimumAnatomicalFootSeparation"]
    ))
    report = {
        "source": "Raw Blender armature audit; not a Unity Humanoid retarget result",
        "generationTaskId": GENERATION_TASK_ID,
        "currentSource": source_result,
        "unityRetargetValidationRequired": True,
        "method": (
            "Samples the imported FBX Action frame range and projects LeftFoot-RightFoot "
            "onto the current LeftUpperLeg-RightUpperLeg axis; knee separation uses "
            "LeftLeg-RightLeg on the same axis. Tolerance is 0.02 Blender units. "
            "Unity Avatar mapping and Humanoid retargeting can change these measurements; "
            "this report only filters raw FBX motion and cannot certify a Unity candidate."
        ),
        "candidateRanking": [item["file"] for item in valid],
        "candidates": results
    }
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2))
    if not valid:
        raise RuntimeError("No candidate could be audited successfully")


if __name__ == "__main__":
    main()

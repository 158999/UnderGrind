import bpy
import json
import math
from pathlib import Path

ROOT = Path("Assets/UnderGrind/Animations/Candidates/PINOC_CrouchWalk")
OUTPUT = Path("Tools/Blender/pinoc-crouch-walk-candidate-audit.json")
CROSSING_TOLERANCE_METERS = 0.02


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

    hips = find_bone(armature, "hips")
    left_upper_leg = find_bone(armature, "leftupleg")
    right_upper_leg = find_bone(armature, "rightupleg")
    left_foot = find_bone(armature, "leftfoot")
    right_foot = find_bone(armature, "rightfoot")
    if not all((hips, left_upper_leg, right_upper_leg, left_foot, right_foot)):
        names = [bone.name for bone in armature.pose.bones]
        raise RuntimeError("required Humanoid bones missing; imported bones: " + ", ".join(names[:80]))

    # Match HumanoidRigImportTests.MeasureFootCrossing:
    # derive the anatomical lateral axis from LeftUpperLeg - RightUpperLeg,
    # then project LeftFoot - RightFoot onto that axis at every sampled frame.
    rest_lateral = (
        armature.data.bones[left_upper_leg.name].head_local
        - armature.data.bones[right_upper_leg.name].head_local
    )
    if rest_lateral.length_squared < 1e-8:
        raise RuntimeError("rest-pose anatomical hip axis is degenerate")
    rest_lateral.normalize()

    rest_foot_delta = (
        armature.data.bones[left_foot.name].head_local
        - armature.data.bones[right_foot.name].head_local
    )
    rest_separation = rest_foot_delta.dot(rest_lateral)

    scene = bpy.context.scene
    start = int(scene.frame_start)
    end = int(scene.frame_end)
    frame_samples = list(range(start, end + 1))
    if len(frame_samples) > 600:
        stride = max(1, math.ceil(len(frame_samples) / 600))
        frame_samples = frame_samples[::stride]
        if frame_samples[-1] != end:
            frame_samples.append(end)

    crossed_frames = []
    min_anatomical_separation = float("inf")
    min_static_axis_separation = float("inf")
    max_hip_translation_drift = 0.0
    first_local_feet = None
    last_local_feet = None
    first_hip_position = None

    for frame in frame_samples:
        scene.frame_set(frame)
        bpy.context.view_layer.update()

        left_hip_position = left_upper_leg.matrix.translation
        right_hip_position = right_upper_leg.matrix.translation
        current_lateral = left_hip_position - right_hip_position
        if current_lateral.length_squared < 1e-8:
            raise RuntimeError(f"degenerate anatomical hip axis at frame {frame}")
        current_lateral.normalize()

        left_foot_position = left_foot.matrix.translation
        right_foot_position = right_foot.matrix.translation
        foot_delta = left_foot_position - right_foot_position

        anatomical_separation = foot_delta.dot(current_lateral)
        static_axis_separation = foot_delta.dot(rest_lateral)
        min_anatomical_separation = min(min_anatomical_separation, anatomical_separation)
        min_static_axis_separation = min(min_static_axis_separation, static_axis_separation)
        if anatomical_separation < -CROSSING_TOLERANCE_METERS:
            crossed_frames.append(frame)

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
        "armature": armature.name,
        "action": armature.animation_data.action.name,
        "frameStart": start,
        "frameEnd": end,
        "sampledFrames": len(frame_samples),
        "leftHipBone": left_upper_leg.name,
        "rightHipBone": right_upper_leg.name,
        "leftFootBone": left_foot.name,
        "rightFootBone": right_foot.name,
        "restAnatomicalFootSeparation": rest_separation,
        "crossingToleranceMeters": CROSSING_TOLERANCE_METERS,
        "crossedFrames": crossed_frames,
        "crossingCount": len(crossed_frames),
        "minimumAnatomicalFootSeparation": min_anatomical_separation,
        "minimumStaticAxisFootSeparation": min_static_axis_separation,
        "maxHipTranslationDrift": max_hip_translation_drift,
        "loopEndpointError": loop_endpoint_error
    }


def main():
    results = []
    for path in sorted(ROOT.glob("*.fbx")):
        try:
            results.append({"status": "ok", **audit_candidate(path)})
        except Exception as exc:
            results.append({"status": "failed", "file": str(path), "error": str(exc)})

    valid = [item for item in results if item["status"] == "ok"]
    valid.sort(key=lambda item: (
        item["crossingCount"],
        item["loopEndpointError"],
        item["maxHipTranslationDrift"],
        -item["minimumAnatomicalFootSeparation"]
    ))
    report = {
        "source": "PINOC crouched-walk text-to-motion candidates",
        "method": (
            "Blender FBX import; anatomical foot separation matches "
            "HumanoidRigImportTests.MeasureFootCrossing: project LeftFoot-RightFoot "
            "onto the current LeftUpperLeg-RightUpperLeg axis. Crossing tolerance: 0.02m."
        ),
        "ranking": [item["file"] for item in valid],
        "results": results
    }
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2))
    if not valid:
        raise RuntimeError("No candidate could be audited successfully")


if __name__ == "__main__":
    main()

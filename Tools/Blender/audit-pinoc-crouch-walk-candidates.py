import bpy
import json
import math
import os
import re
from pathlib import Path
from mathutils import Vector

ROOT = Path("Assets/UnderGrind/Animations/Candidates/PINOC_CrouchWalk")
OUTPUT = Path("Tools/Blender/pinoc-crouch-walk-candidate-audit.json")


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
    left_foot = find_bone(armature, "leftfoot")
    right_foot = find_bone(armature, "rightfoot")
    left_knee = find_bone(armature, "leftleg")
    right_knee = find_bone(armature, "rightleg")
    if not all((hips, left_foot, right_foot, left_knee, right_knee)):
        names = [bone.name for bone in armature.pose.bones]
        raise RuntimeError("required humanoid leg bones missing; imported bones: " + ", ".join(names[:80]))

    rest_left = armature.data.bones[left_foot.name].head_local.x
    rest_right = armature.data.bones[right_foot.name].head_local.x
    left_is_negative = rest_left < rest_right

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
    min_foot_separation = float("inf")
    min_knee_separation = float("inf")
    max_root_drift = 0.0
    first_positions = None
    last_positions = None

    for frame in frame_samples:
        scene.frame_set(frame)
        bpy.context.view_layer.update()

        lf = local_position(hips, left_foot)
        rf = local_position(hips, right_foot)
        lk = local_position(hips, left_knee)
        rk = local_position(hips, right_knee)

        foot_delta = (rf.x - lf.x) if left_is_negative else (lf.x - rf.x)
        knee_delta = (rk.x - lk.x) if left_is_negative else (lk.x - rk.x)
        min_foot_separation = min(min_foot_separation, foot_delta)
        min_knee_separation = min(min_knee_separation, knee_delta)
        if foot_delta <= 0.0:
            crossed_frames.append(frame)

        hips_position = hips.matrix.translation
        if first_positions is None:
            first_positions = (lf.copy(), rf.copy(), hips_position.copy())
        last_positions = (lf.copy(), rf.copy(), hips_position.copy())
        max_root_drift = max(max_root_drift, (hips_position - first_positions[2]).length)

    def distance(a, b):
        return (a - b).length

    loop_error = max(
        distance(first_positions[0], last_positions[0]),
        distance(first_positions[1], last_positions[1])
    )

    return {
        "file": str(path).replace("\\", "/"),
        "armature": armature.name,
        "action": armature.animation_data.action.name,
        "frameStart": start,
        "frameEnd": end,
        "sampledFrames": len(frame_samples),
        "leftFootBone": left_foot.name,
        "rightFootBone": right_foot.name,
        "leftKneeBone": left_knee.name,
        "rightKneeBone": right_knee.name,
        "crossedFrames": crossed_frames,
        "crossingCount": len(crossed_frames),
        "minimumFootSeparation": min_foot_separation,
        "minimumKneeSeparation": min_knee_separation,
        "rootDrift": max_root_drift,
        "loopEndpointError": loop_error
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
        item["rootDrift"],
        -item["minimumFootSeparation"]
    ))
    report = {
        "source": "PINOC crouched-walk text-to-motion candidates",
        "method": "Blender FBX import; sample evaluated Humanoid leg transforms over the full action range",
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

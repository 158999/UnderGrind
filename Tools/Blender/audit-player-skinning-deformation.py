import bpy
import json
import math
import statistics
from pathlib import Path
from mathutils import Matrix


CHARACTERS = {
    "male": {
        "armature": "UG_Male_Armature",
        "parts": (
            "Human_Male_Body",
            "Human_Male_Head",
            "Human_Male_TP_Arms",
            "Human_Male_TP_Hands",
            "Human_Male_FP_Arms",
            "Human_Male_FP_Hands",
        ),
    },
    "female": {
        "armature": "UG_Female_Armature",
        "parts": (
            "Human_Female_Body",
            "Human_Female_Head",
            "Human_Female_TP_Arms",
            "Human_Female_TP_Hands",
            "Human_Female_FP_Arms",
            "Human_Female_FP_Hands",
        ),
    },
}

POSE_BONES = (
    "Spine", "Chest", "Neck", "Head",
    "LeftShoulder", "LeftUpperArm", "LeftLowerArm", "LeftHand",
    "RightShoulder", "RightUpperArm", "RightLowerArm", "RightHand",
    "LeftUpperLeg", "LeftLowerLeg", "LeftFoot",
    "RightUpperLeg", "RightLowerLeg", "RightFoot",
)
ROTATION_AXES = ("X", "Y", "Z")
POSE_ANGLE_DEGREES = 25.0
MOVED_VERTEX_EPSILON_METERS = 0.0001
SCENE_UNIT_SCALE_METERS = bpy.context.scene.unit_settings.scale_length or 1.0


def get_repository_root():
    blend_path = Path(bpy.data.filepath).resolve()
    for folder in (blend_path.parent, *blend_path.parents):
        if (folder / "Assets" / "UnderGrind" / "Settings" / "Defaults" / "PlayerRigSettings.json").is_file():
            return folder
    raise RuntimeError("Repository UnderGrind non trovato risalendo dal file .blend.")


def percentile(values, fraction):
    if not values:
        return 0.0
    ordered = sorted(values)
    index = min(len(ordered) - 1, max(0, math.ceil(fraction * len(ordered)) - 1))
    return float(ordered[index])


def audit_weights(obj, armature):
    deform_bones = {bone.name for bone in armature.data.bones if bone.use_deform}
    group_by_index = {group.index: group.name for group in obj.vertex_groups}
    weight_sums = []
    influence_counts = []
    dominant_counts = {}
    bone_weight_totals = {}
    unweighted_vertices = 0
    non_deform_influences = 0

    for vertex in obj.data.vertices:
        influences = []
        deform_sum = 0.0
        for assignment in vertex.groups:
            group_name = group_by_index.get(assignment.group)
            if group_name is None or assignment.weight <= 0.000001:
                continue
            if group_name not in deform_bones:
                non_deform_influences += 1
                continue
            influences.append((group_name, float(assignment.weight)))
            deform_sum += float(assignment.weight)
            bone_weight_totals[group_name] = bone_weight_totals.get(group_name, 0.0) + float(assignment.weight)

        if not influences or deform_sum <= 0.000001:
            unweighted_vertices += 1
            continue

        weight_sums.append(deform_sum)
        influence_counts.append(len(influences))
        dominant = max(influences, key=lambda item: item[1])[0]
        dominant_counts[dominant] = dominant_counts.get(dominant, 0) + 1

    histogram = {}
    for count in influence_counts:
        key = str(count) if count <= 4 else "5+"
        histogram[key] = histogram.get(key, 0) + 1

    return {
        "vertexCount": len(obj.data.vertices),
        "weightedVertices": len(weight_sums),
        "unweightedVertices": unweighted_vertices,
        "nonDeformInfluenceAssignments": non_deform_influences,
        "influencesPerVertex": histogram,
        "meanInfluencesPerWeightedVertex": round(statistics.mean(influence_counts), 4) if influence_counts else 0.0,
        "maxInfluencesPerVertex": max(influence_counts, default=0),
        "deformWeightSum": {
            "min": round(min(weight_sums), 6) if weight_sums else 0.0,
            "mean": round(statistics.mean(weight_sums), 6) if weight_sums else 0.0,
            "max": round(max(weight_sums), 6) if weight_sums else 0.0,
            "outside_0_98_1_02": sum(1 for value in weight_sums if value < 0.98 or value > 1.02),
        },
        "dominantBoneVertexCounts": dict(sorted(dominant_counts.items())),
        "deformWeightTotals": {
            name: round(value, 6) for name, value in sorted(bone_weight_totals.items())
        },
    }


def evaluated_world_vertices(obj, depsgraph):
    evaluated_obj = obj.evaluated_get(depsgraph)
    evaluated_mesh = evaluated_obj.to_mesh()
    try:
        return [evaluated_obj.matrix_world @ vertex.co for vertex in evaluated_mesh.vertices]
    finally:
        evaluated_obj.to_mesh_clear()


def deformation_summary(rest_vertices, posed_vertices):
    if len(rest_vertices) != len(posed_vertices):
        raise RuntimeError("Il numero di vertici valutati cambia durante la deformazione.")
    displacements = [(posed - rest).length * SCENE_UNIT_SCALE_METERS
                    for rest, posed in zip(rest_vertices, posed_vertices)]
    moved = sum(value > MOVED_VERTEX_EPSILON_METERS for value in displacements)
    return {
        "movedVertices": moved,
        "vertexCount": len(displacements),
        "maxDisplacementMeters": round(max(displacements, default=0.0), 6),
        "meanDisplacementMeters": round(statistics.mean(displacements), 6) if displacements else 0.0,
        "p95DisplacementMeters": round(percentile(displacements, 0.95), 6),
    }


def audit_character(character_name, config, depsgraph):
    armature = bpy.data.objects.get(config["armature"])
    if armature is None or armature.type != "ARMATURE":
        raise RuntimeError("Armatura mancante: " + config["armature"])

    parts = []
    for part_name in config["parts"]:
        obj = bpy.data.objects.get(part_name)
        if obj is None or obj.type != "MESH":
            raise RuntimeError("Mesh mancante: " + part_name)
        modifiers = [modifier for modifier in obj.modifiers
                     if modifier.type == "ARMATURE" and modifier.object == armature]
        if not modifiers:
            raise RuntimeError(part_name + ": modificatore Armature non collegato al rig previsto.")
        parts.append(obj)

    pose_state = {
        pose_bone.name: (pose_bone.rotation_mode, pose_bone.matrix_basis.copy())
        for pose_bone in armature.pose.bones
    }

    try:
        for pose_bone in armature.pose.bones:
            pose_bone.rotation_mode = "XYZ"
            pose_bone.matrix_basis = Matrix.Identity(4)
        bpy.context.view_layer.update()
        depsgraph = bpy.context.evaluated_depsgraph_get()

        rest_vertices = {obj.name: evaluated_world_vertices(obj, depsgraph) for obj in parts}
        rest_bounds = {}
        for obj in parts:
            coords = rest_vertices[obj.name]
            if not coords:
                raise RuntimeError(obj.name + ": nessun vertice valutato.")
            rest_bounds[obj.name] = {
                "min": [round(min(v[axis] for v in coords), 6) for axis in range(3)],
                "max": [round(max(v[axis] for v in coords), 6) for axis in range(3)],
            }

        deformation_samples = []
        missing_pose_bones = []
        for bone_name in POSE_BONES:
            pose_bone = armature.pose.bones.get(bone_name)
            if pose_bone is None:
                missing_pose_bones.append(bone_name)
                continue

            original_mode, original_basis = pose_state[bone_name]
            for axis_index, axis_name in enumerate(ROTATION_AXES):
                pose_bone.rotation_mode = "XYZ"
                pose_bone.rotation_euler = (0.0, 0.0, 0.0)
                rotation = [0.0, 0.0, 0.0]
                rotation[axis_index] = math.radians(POSE_ANGLE_DEGREES)
                pose_bone.rotation_euler = rotation
                bpy.context.view_layer.update()
                depsgraph = bpy.context.evaluated_depsgraph_get()

                part_results = []
                for obj in parts:
                    posed = evaluated_world_vertices(obj, depsgraph)
                    part_results.append({
                        "mesh": obj.name,
                        **deformation_summary(rest_vertices[obj.name], posed),
                    })
                deformation_samples.append({
                    "bone": bone_name,
                    "axis": axis_name,
                    "angleDegrees": POSE_ANGLE_DEGREES,
                    "parts": part_results,
                })

                pose_bone.rotation_mode = "XYZ"
                pose_bone.matrix_basis = Matrix.Identity(4)
                bpy.context.view_layer.update()
                depsgraph = bpy.context.evaluated_depsgraph_get()

        return {
            "armature": armature.name,
            "boneCount": len(armature.data.bones),
            "deformBoneCount": sum(1 for bone in armature.data.bones if bone.use_deform),
            "missingPoseBones": missing_pose_bones,
            "parts": [
                {
                    "name": obj.name,
                    "weights": audit_weights(obj, armature),
                    "restBoundsWorldMeters": rest_bounds[obj.name],
                }
                for obj in parts
            ],
            "deformationSamples": deformation_samples,
        }
    finally:
        for pose_bone in armature.pose.bones:
            original = pose_state.get(pose_bone.name)
            if original is None:
                continue
            original_mode, original_basis = original
            pose_bone.rotation_mode = original_mode
            pose_bone.matrix_basis = original_basis.copy()
        bpy.context.view_layer.update()


def main():
    if not bpy.data.filepath:
        raise RuntimeError("Il file humans.blend deve essere aperto prima dell'audit.")

    report = {
        "source": bpy.data.filepath,
        "blenderVersion": bpy.app.version_string,
        "poseAngleDegrees": POSE_ANGLE_DEGREES,
        "sceneUnitScaleMeters": SCENE_UNIT_SCALE_METERS,
        "movedVertexEpsilonMeters": MOVED_VERTEX_EPSILON_METERS,
        "characters": {},
        "errors": [],
        "notes": [
            "Audit diagnostico non distruttivo: il file .blend non viene salvato.",
            "Le rotazioni sono test sintetici locali delle ossa, non animazioni finali.",
            "Le statistiche dei pesi non certificano da sole la qualità anatomica.",
        ],
    }

    depsgraph = bpy.context.evaluated_depsgraph_get()
    for character_name, config in CHARACTERS.items():
        report["characters"][character_name] = audit_character(character_name, config, depsgraph)

    repo_root = get_repository_root()
    report_path = repo_root / "Tools" / "Blender" / "player-skinning-deformation-audit.json"
    report_path.write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")

    summary = {}
    for character_name, character in report["characters"].items():
        summary[character_name] = {
            "parts": {
                part["name"]: {
                    "unweightedVertices": part["weights"]["unweightedVertices"],
                    "influenceHistogram": part["weights"]["influencesPerVertex"],
                    "dominantBones": part["weights"]["dominantBoneVertexCounts"],
                }
                for part in character["parts"]
            },
            "poseSamples": len(character["deformationSamples"]),
            "zeroMovementSamples": sum(
                1 for sample in character["deformationSamples"]
                if not any(part["movedVertices"] for part in sample["parts"])
            ),
        }

    print("UNDERGRIND_SKINNING_DEFORMATION_AUDIT=" + json.dumps(summary, ensure_ascii=False))
    print("UNDERGRIND_SKINNING_DEFORMATION_REPORT=" + str(report_path))


if __name__ == "__main__":
    main()

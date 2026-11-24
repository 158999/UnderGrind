import bpy
import json
import math
from pathlib import Path
from mathutils import Vector


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

MAX_INFLUENCES = 4
CENTERLINE_EPSILON = 0.02
WEIGHT_EPSILON = 1e-8
NORMALIZATION_TOLERANCE = 0.0001


def repository_root():
    blend_path = Path(bpy.data.filepath).resolve()
    for folder in (blend_path.parent, *blend_path.parents):
        if (folder / "Assets" / "UnderGrind" / "Settings" / "Defaults" / "PlayerRigSettings.json").is_file():
            return folder
    raise RuntimeError("Repository UnderGrind non trovato risalendo dal file .blend.")


def side_bones(prefix, suffixes):
    return {prefix + suffix for suffix in suffixes}


def positive_x_side_for_armature(armature):
    bones = {bone.name: bone for bone in armature.data.bones}
    left_reference = (bones["LeftUpperLeg"].head_local.x + bones["LeftUpperArm"].head_local.x) * 0.5
    right_reference = (bones["RightUpperLeg"].head_local.x + bones["RightUpperArm"].head_local.x) * 0.5
    if abs(left_reference - right_reference) <= WEIGHT_EPSILON:
        raise RuntimeError(armature.name + ": impossibile determinare il lato anatomico dai riferimenti ossei.")
    return "Left" if left_reference > right_reference else "Right"


def side_for_vertex_x(armature_local_x, positive_x_side):
    if armature_local_x > CENTERLINE_EPSILON:
        return positive_x_side
    if armature_local_x < -CENTERLINE_EPSILON:
        return "Right" if positive_x_side == "Left" else "Left"
    return None


def remap_group_to_anatomical_side(group_name, target_side):
    if target_side is None:
        return group_name
    if group_name.startswith("Left"):
        suffix = group_name[4:]
    elif group_name.startswith("Right"):
        suffix = group_name[5:]
    else:
        return group_name
    return target_side + suffix


def allowed_groups_for_vertex(part_name, armature_local_x, positive_x_side):
    central = {"Hips", "Spine", "Chest", "Neck", "Head"}
    target_side = side_for_vertex_x(armature_local_x, positive_x_side)

    if "_Head" in part_name:
        return {"Head", "Neck", "Chest"}

    if "_Hands" in part_name:
        if target_side is not None:
            return {target_side + "LowerArm", target_side + "Hand"}
        return {"LeftLowerArm", "LeftHand", "RightLowerArm", "RightHand"}

    if "_Arms" in part_name:
        if target_side is not None:
            return {"Chest"} | side_bones(target_side, ("Shoulder", "UpperArm", "LowerArm", "Hand"))
        return {"Chest"} | side_bones("Left", ("Shoulder", "UpperArm", "LowerArm", "Hand")) | side_bones(
            "Right", ("Shoulder", "UpperArm", "LowerArm", "Hand")
        )

    if "_Body" in part_name:
        if target_side is not None:
            return central | side_bones(target_side, ("UpperLeg", "LowerLeg", "Foot", "Toes"))
        return central | side_bones("Left", ("UpperLeg", "LowerLeg", "Foot", "Toes")) | side_bones(
            "Right", ("UpperLeg", "LowerLeg", "Foot", "Toes")
        )

    raise RuntimeError("Tipo mesh non riconosciuto per il reweighting: " + part_name)


def existing_allowed_weights(vertex, group_names, allowed_names, target_side):
    result = {}
    for assignment in vertex.groups:
        group_name = group_names.get(assignment.group)
        mapped_name = remap_group_to_anatomical_side(group_name, target_side) if group_name else None
        if mapped_name in allowed_names and assignment.weight > WEIGHT_EPSILON:
            result[mapped_name] = result.get(mapped_name, 0.0) + float(assignment.weight)
    return result


def point_segment_distance(point, head, tail):
    segment = tail - head
    length_squared = segment.length_squared
    if length_squared <= WEIGHT_EPSILON:
        return (point - head).length
    factor = max(0.0, min(1.0, (point - head).dot(segment) / length_squared))
    closest = head + segment * factor
    return (point - closest).length


def nearest_allowed_bone(point, allowed_names, bones):
    candidates = [
        (point_segment_distance(point, bones[name].head_local, bones[name].tail_local), name)
        for name in allowed_names if name in bones
    ]
    if not candidates:
        raise RuntimeError("Nessun osso valido disponibile per il fallback dei pesi.")
    candidates.sort(key=lambda item: item[0])
    return candidates[0][1]


def existing_allowed_weights(vertex, group_names, allowed_names):
    result = {}
    for assignment in vertex.groups:
        group_name = group_names.get(assignment.group)
        if group_name in allowed_names and assignment.weight > WEIGHT_EPSILON:
            result[group_name] = result.get(group_name, 0.0) + float(assignment.weight)
    return result


def normalized_top_weights(raw_weights):
    selected = sorted(
        ((name, weight) for name, weight in raw_weights.items() if weight > WEIGHT_EPSILON),
        key=lambda item: item[1],
        reverse=True,
    )[:MAX_INFLUENCES]
    total = sum(weight for _, weight in selected)
    if total <= WEIGHT_EPSILON:
        return []
    return [(name, weight / total) for name, weight in selected]


def clear_vertex_groups(obj):
    vertices = obj.data.vertices
    for group in obj.vertex_groups:
        assigned = [
            vertex.index for vertex in vertices
            if any(assignment.group == group.index for assignment in vertex.groups)
        ]
        if assigned:
            group.remove(assigned)


def inspect_reweighted_mesh(obj, allowed_union):
    group_by_index = {group.index: group.name for group in obj.vertex_groups}
    unweighted = 0
    too_many = 0
    normalized_outliers = 0
    forbidden_assignments = 0
    influence_histogram = {}
    dominant_counts = {}
    weight_sums = []

    for vertex in obj.data.vertices:
        influences = []
        for assignment in vertex.groups:
            group_name = group_by_index.get(assignment.group)
            if assignment.weight <= WEIGHT_EPSILON:
                continue
            if group_name not in allowed_union:
                forbidden_assignments += 1
            influences.append((group_name, float(assignment.weight)))

        if not influences:
            unweighted += 1
            continue

        if len(influences) > MAX_INFLUENCES:
            too_many += 1
        total = sum(weight for _, weight in influences)
        weight_sums.append(total)
        if abs(total - 1.0) > NORMALIZATION_TOLERANCE:
            normalized_outliers += 1

        key = str(len(influences))
        influence_histogram[key] = influence_histogram.get(key, 0) + 1
        dominant = max(influences, key=lambda item: item[1])[0]
        dominant_counts[dominant] = dominant_counts.get(dominant, 0) + 1

    return {
        "vertexCount": len(obj.data.vertices),
        "unweightedVertices": unweighted,
        "verticesOverInfluenceLimit": too_many,
        "verticesWithUnnormalizedWeights": normalized_outliers,
        "forbiddenInfluenceAssignments": forbidden_assignments,
        "influencesPerVertex": dict(sorted(influence_histogram.items())),
        "weightSumMin": round(min(weight_sums), 8) if weight_sums else 0.0,
        "weightSumMax": round(max(weight_sums), 8) if weight_sums else 0.0,
        "dominantBoneVertexCounts": dict(sorted(dominant_counts.items())),
    }


def reweight_mesh(obj, armature):
    deform_bones = {bone.name: bone for bone in armature.data.bones if bone.use_deform}
    group_by_index = {group.index: group.name for group in obj.vertex_groups}
    armature_inverse = armature.matrix_world.inverted()
    positive_x_side = positive_x_side_for_armature(armature)
    planned_weights = []
    fallback_vertices = 0
    removed_influence_assignments = 0
    allowed_union = set()
    fallback_samples = []
    side_weight_totals = {
        "positiveX": {"left": 0.0, "right": 0.0, "vertices": 0},
        "negativeX": {"left": 0.0, "right": 0.0, "vertices": 0},
        "center": {"left": 0.0, "right": 0.0, "vertices": 0},
    }

    for vertex in obj.data.vertices:
        armature_point = armature_inverse @ (obj.matrix_world @ vertex.co)
        target_side = side_for_vertex_x(armature_point.x, positive_x_side)
        allowed_names = allowed_groups_for_vertex(obj.name, armature_point.x, positive_x_side)
        allowed_names &= set(deform_bones)
        allowed_union.update(allowed_names)

        side_key = "positiveX" if armature_point.x > CENTERLINE_EPSILON else (
            "negativeX" if armature_point.x < -CENTERLINE_EPSILON else "center"
        )
        side_weight_totals[side_key]["vertices"] += 1
        original_influences = []
        for assignment in vertex.groups:
            group_name = group_by_index.get(assignment.group)
            if group_name is None or assignment.weight <= WEIGHT_EPSILON:
                continue
            original_influences.append((group_name, float(assignment.weight)))
            if group_name.startswith("Left"):
                side_weight_totals[side_key]["left"] += float(assignment.weight)
            elif group_name.startswith("Right"):
                side_weight_totals[side_key]["right"] += float(assignment.weight)

        raw_weights = existing_allowed_weights(vertex, group_by_index, allowed_names, target_side)
        removed_influence_assignments += sum(
            1 for assignment in vertex.groups
            if group_by_index.get(assignment.group) not in allowed_names
            and assignment.weight > WEIGHT_EPSILON
        )

        if not raw_weights:
            fallback_vertices += 1
            fallback_samples.append({
                "vertexIndex": vertex.index,
                "armatureLocalPosition": [round(float(value), 6) for value in armature_point],
                "originalInfluences": [
                    {"bone": name, "weight": round(weight, 6)}
                    for name, weight in sorted(original_influences, key=lambda item: item[1], reverse=True)
                ],
                "targetAnatomicalSide": target_side,
                "allowedGroups": sorted(allowed_names),
            })
            nearest = nearest_allowed_bone(armature_point, allowed_names, deform_bones)
            raw_weights = {nearest: 1.0}

        normalized = normalized_top_weights(raw_weights)
        if not normalized:
            fallback_vertices += 1
            nearest = nearest_allowed_bone(armature_point, allowed_names, deform_bones)
            normalized = [(nearest, 1.0)]
        planned_weights.append(normalized)

    clear_vertex_groups(obj)
    groups = {group.name: group for group in obj.vertex_groups}
    for bone_name in allowed_union:
        if bone_name not in groups:
            groups[bone_name] = obj.vertex_groups.new(name=bone_name)

    for vertex_index, weights in enumerate(planned_weights):
        for bone_name, weight in weights:
            groups[bone_name].add([vertex_index], weight, "REPLACE")

    verification = inspect_reweighted_mesh(obj, allowed_union)
    verification["fallbackVertices"] = fallback_vertices
    verification["fallbackVertexSamples"] = fallback_samples
    verification["removedInfluenceAssignments"] = removed_influence_assignments
    verification["allowedGroups"] = sorted(allowed_union)
    verification["positiveXAnatomicalSide"] = positive_x_side
    verification["originalLeftRightWeightByVertexSide"] = {
        side: {
            "vertices": values["vertices"],
            "leftWeight": round(values["left"], 6),
            "rightWeight": round(values["right"], 6),
        }
        for side, values in side_weight_totals.items()
    }

    if verification["unweightedVertices"] != 0:
        raise RuntimeError(obj.name + ": vertici non pesati dopo il reweighting.")
    if verification["verticesOverInfluenceLimit"] != 0:
        raise RuntimeError(obj.name + ": limite di quattro influenze superato.")
    if verification["verticesWithUnnormalizedWeights"] != 0:
        raise RuntimeError(obj.name + ": pesi non normalizzati dopo il reweighting.")
    if verification["forbiddenInfluenceAssignments"] != 0:
        raise RuntimeError(obj.name + ": gruppi non consentiti ancora assegnati.")

    return verification


def main():
    if not bpy.data.filepath:
        raise RuntimeError("Il file sorgente humans.blend deve essere aperto prima del reweighting.")

    source_path = Path(bpy.data.filepath).resolve()
    root = repository_root()
    output_path = root / "Tools" / "Blender" / "Generated" / "humans_reweighted.blend"
    output_path.parent.mkdir(parents=True, exist_ok=True)

    report = {
        "sourceBlend": str(source_path),
        "derivedBlend": str(output_path),
        "maxInfluencesPerVertex": MAX_INFLUENCES,
        "characters": {},
        "notes": [
            "Il reweighting modifica soltanto le 12 mesh derivate collegate alle armature.",
            "Gli oggetti sorgente Human_Male e Human_Female non vengono modificati.",
            "Le influenze sono filtrate per parte anatomica, limitate a quattro e normalizzate.",
            "I vertici senza influenze ammesse ricevono il peso sull'osso consentito più vicino.",
        ],
    }

    bpy.context.view_layer.update()
    for character_name, config in CHARACTERS.items():
        armature = bpy.data.objects.get(config["armature"])
        if armature is None or armature.type != "ARMATURE":
            raise RuntimeError("Armatura mancante: " + config["armature"])

        character_report = {"armature": armature.name, "parts": []}
        for part_name in config["parts"]:
            obj = bpy.data.objects.get(part_name)
            if obj is None or obj.type != "MESH":
                raise RuntimeError("Mesh mancante: " + part_name)
            if obj.parent != armature:
                raise RuntimeError(part_name + ": parent armatura errato.")
            if not any(modifier.type == "ARMATURE" and modifier.object == armature
                       for modifier in obj.modifiers):
                raise RuntimeError(part_name + ": modificatore Armature non collegato.")
            character_report["parts"].append({
                "name": part_name,
                **reweight_mesh(obj, armature),
            })
        report["characters"][character_name] = character_report

    report_path = root / "Tools" / "Blender" / "weight-repair-report.json"
    report_path.write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
    bpy.ops.wm.save_as_mainfile(filepath=str(output_path))

    summary = {
        name: {
            part["name"]: {
                "fallbackVertices": part["fallbackVertices"],
                "removedInfluenceAssignments": part["removedInfluenceAssignments"],
                "unweighted": part["unweightedVertices"],
                "overFourInfluences": part["verticesOverInfluenceLimit"],
                "unnormalized": part["verticesWithUnnormalizedWeights"],
                "forbidden": part["forbiddenInfluenceAssignments"],
                "dominant": part["dominantBoneVertexCounts"],
            }
            for part in character["parts"]
        }
        for name, character in report["characters"].items()
    }
    print("UNDERGRIND_WEIGHT_REPAIR=" + json.dumps(summary, ensure_ascii=False))
    print("UNDERGRIND_WEIGHT_REPAIR_DERIVED_BLEND=" + str(output_path))


if __name__ == "__main__":
    main()

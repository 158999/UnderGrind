import bpy
import json
import re
from pathlib import Path


CHARACTERS = {
    "male": {
        "armature": "UG_Male_Armature",
        "prefix": "Human_Male",
        "required_parts": (
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
        "prefix": "Human_Female",
        "required_parts": (
            "Human_Female_Body",
            "Human_Female_Head",
            "Human_Female_TP_Arms",
            "Human_Female_TP_Hands",
            "Human_Female_FP_Arms",
            "Human_Female_FP_Hands",
        ),
    },
}

REQUIRED_HUMANOID_BONES = {
    "Hips", "Spine", "Chest", "Neck", "Head",
    "LeftShoulder", "LeftUpperArm", "LeftLowerArm", "LeftHand",
    "RightShoulder", "RightUpperArm", "RightLowerArm", "RightHand",
    "LeftUpperLeg", "LeftLowerLeg", "LeftFoot",
    "RightUpperLeg", "RightLowerLeg", "RightFoot",
}


def find_repository_root():
    blend_path = bpy.data.filepath
    if not blend_path:
        raise RuntimeError("Salva prima il file .blend dentro il repository UnderGrind.")

    for folder in (Path(blend_path).resolve().parent, *Path(blend_path).resolve().parents):
        defaults = folder / "Assets" / "UnderGrind" / "Settings" / "Defaults" / "PlayerRigSettings.json"
        if defaults.is_file():
            return folder, defaults

    raise RuntimeError(
        "Repository UnderGrind non trovato risalendo dalla cartella del .blend. "
        "Salva il file in Assets/UnderGrind/Art/Source."
    )


def read_unity_asset_path(asset_path, field_name, fallback):
    if not asset_path.is_file():
        return fallback

    content = asset_path.read_text(encoding="utf-8")
    match = re.search(r"^\s*" + re.escape(field_name) + r":\s*(.*?)\s*$", content, re.MULTILINE)
    if not match:
        return fallback
    value = match.group(1).strip().strip('"')
    return value or fallback


def output_path(repo_root, relative_path):
    candidate = Path(relative_path)
    return candidate if candidate.is_absolute() else repo_root / candidate


def validate_character(character_name, config):
    armature = bpy.data.objects.get(config["armature"])
    if armature is None or armature.type != "ARMATURE":
        raise RuntimeError("Armatura mancante o non valida: " + config["armature"])

    bone_names = {bone.name for bone in armature.data.bones}
    missing_bones = sorted(REQUIRED_HUMANOID_BONES - bone_names)
    if missing_bones:
        raise RuntimeError(
            character_name + ": ossa Humanoid mancanti: " + ", ".join(missing_bones)
        )

    parts = []
    for part_name in config["required_parts"]:
        part = bpy.data.objects.get(part_name)
        if part is None or part.type != "MESH":
            raise RuntimeError(character_name + ": mesh richiesta mancante: " + part_name)
        if part.parent != armature:
            raise RuntimeError(
                character_name + ": " + part_name + " non è figlia di " + armature.name
            )

        modifiers = [
            modifier for modifier in part.modifiers
            if modifier.type == "ARMATURE" and modifier.object == armature
        ]
        if not modifiers:
            raise RuntimeError(
                character_name + ": " + part_name + " non ha un modificatore Armature valido"
            )

        deform_bones = {bone.name for bone in armature.data.bones if bone.use_deform}
        vertex_group_names = {group.name for group in part.vertex_groups}
        if not deform_bones.intersection(vertex_group_names):
            raise RuntimeError(
                character_name + ": " + part_name + " non ha gruppi di pesi collegati all'armatura"
            )

        group_names = [group.name for group in part.vertex_groups]
        unweighted = 0
        for vertex in part.data.vertices:
            has_deform_weight = any(
                item.weight > 0.0 and
                item.group < len(group_names) and
                group_names[item.group] in deform_bones
                for item in vertex.groups
            )
            if not has_deform_weight:
                unweighted += 1

        if unweighted:
            raise RuntimeError(
                character_name + ": " + part_name + " contiene " +
                str(unweighted) + " vertici senza pesi deformanti"
            )
        if not part.data.polygons:
            raise RuntimeError(character_name + ": mesh vuota: " + part_name)

        parts.append(part)

    return armature, parts


def export_character(character_name, armature, parts, destination):
    destination.parent.mkdir(parents=True, exist_ok=True)
    selected_before = list(bpy.context.selected_objects)
    active_before = bpy.context.view_layer.objects.active

    try:
        if bpy.context.object and bpy.context.object.mode != "OBJECT":
            bpy.ops.object.mode_set(mode="OBJECT")

        bpy.ops.object.select_all(action="DESELECT")
        for obj in [armature, *parts]:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = armature

        bpy.ops.export_scene.fbx(
            filepath=str(destination),
            use_selection=True,
            object_types={"ARMATURE", "MESH"},
            apply_unit_scale=True,
            use_space_transform=True,
            bake_space_transform=False,
            use_mesh_modifiers=True,
            mesh_smooth_type="FACE",
            use_subsurf=False,
            use_mesh_edges=False,
            use_tspace=False,
            use_custom_props=False,
            add_leaf_bones=False,
            use_armature_deform_only=False,
            bake_anim=False,
            path_mode="AUTO",
            axis_forward="-Z",
            axis_up="Y",
        )
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for obj in selected_before:
            if obj.name in bpy.data.objects:
                obj.select_set(True)
        if active_before and active_before.name in bpy.data.objects:
            bpy.context.view_layer.objects.active = active_before

    if not destination.is_file() or destination.stat().st_size == 0:
        raise RuntimeError("Export FBX non prodotto: " + str(destination))

    return {
        "character": character_name,
        "file": str(destination),
        "bytes": destination.stat().st_size,
        "bones": len(armature.data.bones),
        "parts": [
            {
                "name": part.name,
                "vertices": len(part.data.vertices),
                "triangles": sum(max(0, len(poly.vertices) - 2) for poly in part.data.polygons),
            }
            for part in parts
        ],
    }


def main():
    repo_root, defaults_path = find_repository_root()
    defaults = json.loads(defaults_path.read_text(encoding="utf-8"))
    rig_asset_path = repo_root / "Assets" / "UnderGrind" / "Settings" / "PlayerRigSettings.asset"

    male_path = read_unity_asset_path(
        rig_asset_path, "maleSourceAssetPath", defaults["maleSourceAssetPath"]
    )
    female_path = read_unity_asset_path(
        rig_asset_path, "femaleSourceAssetPath", defaults["femaleSourceAssetPath"]
    )
    male_output = output_path(repo_root, male_path)
    female_output = output_path(repo_root, female_path)

    if male_output.resolve() == female_output.resolve():
        raise RuntimeError("I percorsi FBX maschile e femminile devono essere distinti.")

    validated = {}
    for character_name, config in CHARACTERS.items():
        validated[character_name] = validate_character(character_name, config)

    report = [
        export_character("male", *validated["male"], male_output),
        export_character("female", *validated["female"], female_output),
    ]

    report_path = repo_root / "Tools" / "Blender" / "last-export-report.json"
    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
    print("UNDERGRIND_FBX_EXPORT=" + json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()

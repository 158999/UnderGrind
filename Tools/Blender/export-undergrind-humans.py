import bpy
import json
import re
from pathlib import Path
from mathutils import Vector


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

REQUIRED_HUMANOID_ANCESTORS = {
    "Spine": "Hips",
    "Chest": "Spine",
    "Neck": "Chest",
    "Head": "Neck",
    "LeftShoulder": "Chest",
    "LeftUpperArm": "LeftShoulder",
    "LeftLowerArm": "LeftUpperArm",
    "LeftHand": "LeftLowerArm",
    "RightShoulder": "Chest",
    "RightUpperArm": "RightShoulder",
    "RightLowerArm": "RightUpperArm",
    "RightHand": "RightLowerArm",
    "LeftUpperLeg": "Hips",
    "LeftLowerLeg": "LeftUpperLeg",
    "LeftFoot": "LeftLowerLeg",
    "RightUpperLeg": "Hips",
    "RightLowerLeg": "RightUpperLeg",
    "RightFoot": "RightLowerLeg",
}


def inspect_armature_hierarchy(armature, character_name):
    bones = {bone.name: bone for bone in armature.data.bones}
    problems = []

    for bone_name, expected_ancestor_name in REQUIRED_HUMANOID_ANCESTORS.items():
        bone = bones.get(bone_name)
        if bone is None:
            continue

        ancestor = bone.parent
        while ancestor is not None and ancestor.name != expected_ancestor_name:
            ancestor = ancestor.parent

        if ancestor is None:
            problems.append(
                character_name + ": " + bone_name +
                " deve discendere da " + expected_ancestor_name +
                "; parent attuale: " +
                (bone.parent.name if bone.parent else "<root>")
            )

    if problems:
        raise RuntimeError("Gerarchia Humanoid non valida:\\n" + "\\n".join(problems))

    return [
        {
            "name": bone.name,
            "parent": bone.parent.name if bone.parent else None,
            "useDeform": bone.use_deform,
            "head": [round(float(value), 6) for value in bone.head_local],
            "tail": [round(float(value), 6) for value in bone.tail_local],
        }
        for bone in armature.data.bones
    ]


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


def inspect_mesh_materials(part):
    uv_layer = part.data.uv_layers.active or (part.data.uv_layers[0] if part.data.uv_layers else None)
    uv_bounds = None
    if uv_layer is not None and uv_layer.data:
        u_values = [item.uv.x for item in uv_layer.data]
        v_values = [item.uv.y for item in uv_layer.data]
        uv_bounds = {
            "min": [min(u_values), min(v_values)],
            "max": [max(u_values), max(v_values)],
            "layer": uv_layer.name,
        }

    materials = []
    for slot_index, slot in enumerate(part.material_slots):
        material = slot.material
        if material is None:
            materials.append({"slot": slot_index, "material": None, "images": []})
            continue
        images = []
        if material.use_nodes and material.node_tree:
            for node in material.node_tree.nodes:
                if node.type != "TEX_IMAGE" or node.image is None:
                    continue
                image = node.image
                image_path = bpy.path.abspath(image.filepath) if image.filepath else ""
                packed = image.packed_file is not None
                exists = image.source == "GENERATED" or packed or (image_path and Path(image_path).is_file())
                images.append({
                    "name": image.name,
                    "filepath": image.filepath,
                    "resolvedPath": image_path,
                    "packed": packed,
                    "available": bool(exists),
                    "size": list(image.size),
                })
        materials.append({"slot": slot_index, "material": material.name, "images": images})

    return {
        "uvLayerCount": len(part.data.uv_layers),
        "uvBounds": uv_bounds,
        "materialSlotCount": len(part.material_slots),
        "materials": materials,
        "missingImages": [
            image["name"] for material in materials for image in material["images"]
            if not image["available"]
        ],
    }


def matrix_translation(matrix):
    return [round(float(value), 6) for value in matrix.translation]


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

    bone_hierarchy = inspect_armature_hierarchy(armature, character_name)

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
        if len(part.data.uv_layers) == 0:
            raise RuntimeError(character_name + ": " + part_name + " non contiene UV map; impossibile garantire la texture.")

        material_audit = inspect_mesh_materials(part)
        if material_audit["missingImages"]:
            raise RuntimeError(
                character_name + ": immagini texture non trovate per " + part_name + ": " +
                ", ".join(material_audit["missingImages"])
            )

        parts.append(part)

    return armature, parts


def export_character(character_name, armature, parts, destination):
    destination.parent.mkdir(parents=True, exist_ok=True)
    selected_before = list(bpy.context.selected_objects)
    active_before = bpy.context.view_layer.objects.active
    original_armature_matrix = armature.matrix_world.copy()
    original_root_translation = matrix_translation(original_armature_matrix)

    try:
        if bpy.context.object and bpy.context.object.mode != "OBJECT":
            bpy.ops.object.mode_set(mode="OBJECT")

        # The source scene keeps the two characters side by side for authoring.
        # Export each rig at its own origin without changing mesh-to-bone offsets.
        export_armature_matrix = armature.matrix_world.copy()
        export_armature_matrix.translation = Vector((0.0, 0.0, 0.0))
        armature.matrix_world = export_armature_matrix
        bpy.context.view_layer.update()

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
            path_mode="COPY",
            embed_textures=True,
            axis_forward="-Z",
            axis_up="Y",
        )
    finally:
        armature.matrix_world = original_armature_matrix
        bpy.context.view_layer.update()
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
        "boneHierarchy": inspect_armature_hierarchy(armature, character_name),
        "sourceRootTranslation": original_root_translation,
        "exportRootTranslation": [0.0, 0.0, 0.0],
        "parts": [
            {
                "name": part.name,
                "vertices": len(part.data.vertices),
                "triangles": sum(max(0, len(poly.vertices) - 2) for poly in part.data.polygons),
                **inspect_mesh_materials(part),
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

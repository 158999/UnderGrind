import bpy
import bmesh
import json
from pathlib import Path

CHARACTERS = {
    "male": {
        "armature": "UG_Male_Armature",
        "body": "Human_Male_Body",
        "parts": [
            "Human_Male_Body", "Human_Male_Head",
            "Human_Male_TP_Arms", "Human_Male_TP_Hands",
            "Human_Male_FP_Arms", "Human_Male_FP_Hands",
        ],
    },
    "female": {
        "armature": "UG_Female_Armature",
        "body": "Human_Female_Body",
        "parts": [
            "Human_Female_Body", "Human_Female_Head",
            "Human_Female_TP_Arms", "Human_Female_TP_Hands",
            "Human_Female_FP_Arms", "Human_Female_FP_Hands",
        ],
    },
}
CENTER_BONES = ("Hips", "Spine", "Chest", "Neck", "Head")
SKIN_COLOR = (0.72, 0.55, 0.43, 1.0)
SELF_INTERSECTING_MALE_BODY_FACES = {92, 186}
EPS = 1e-5


def get_body_centerline_x(body, armature):
    inverse = armature.matrix_world.inverted()
    coordinates = [
        inverse @ (body.matrix_world @ vertex.co)
        for vertex in body.data.vertices
    ]
    if not coordinates:
        raise RuntimeError(body.name + ": body mesh has no vertices")
    minimum = min(point.x for point in coordinates)
    maximum = max(point.x for point in coordinates)
    return (minimum + maximum) * 0.5


def get_central_bone_x(armature):
    bones = {bone.name: bone for bone in armature.data.bones}
    missing = [name for name in CENTER_BONES if name not in bones]
    if missing:
        raise RuntimeError(armature.name + ": missing central bones: " + ", ".join(missing))
    return sum(float(bones[name].head_local.x) for name in CENTER_BONES) / len(CENTER_BONES)


def shift_armature_to_body_centerline(armature, body):
    body_center_x = get_body_centerline_x(body, armature)
    bone_center_x = get_central_bone_x(armature)
    offset = bone_center_x - body_center_x
    scale = armature.matrix_world.to_3x3().col[0].length
    offset_meters = offset * scale

    if abs(offset_meters) > 0.01:
        bpy.ops.object.mode_set(mode="OBJECT")
        bpy.ops.object.select_all(action="DESELECT")
        armature.select_set(True)
        bpy.context.view_layer.objects.active = armature
        bpy.ops.object.mode_set(mode="EDIT")
        for bone in armature.data.edit_bones:
            bone.head.x -= offset
            bone.tail.x -= offset
        bpy.ops.object.mode_set(mode="OBJECT")
        bpy.context.view_layer.update()

    after_bone_x = get_central_bone_x(armature)
    after_offset = (after_bone_x - get_body_centerline_x(body, armature)) * scale
    if abs(after_offset) > 0.01:
        raise RuntimeError(
            armature.name + ": skeleton still offset by " + str(round(after_offset, 6)) + " m"
        )

    return {
        "bodyCenterlineX": round(body_center_x, 6),
        "boneCenterlineXBefore": round(bone_center_x, 6),
        "localShiftApplied": round(offset, 6),
        "offsetBeforeMeters": round(offset_meters, 6),
        "boneCenterlineXAfter": round(after_bone_x, 6),
        "offsetAfterMeters": round(after_offset, 6),
    }


def make_skin_material():
    material = bpy.data.materials.get("UG_Skin")
    if material is None:
        material = bpy.data.materials.new("UG_Skin")
    material.diffuse_color = SKIN_COLOR
    material.use_nodes = True
    material.use_backface_culling = False
    nodes = material.node_tree.nodes
    principled = next((node for node in nodes if node.type == "BSDF_PRINCIPLED"), None)
    if principled is None:
        principled = nodes.new("ShaderNodeBsdfPrincipled")
    output = next((node for node in nodes if node.type == "OUTPUT_MATERIAL"), None)
    if output is None:
        output = nodes.new("ShaderNodeOutputMaterial")
    if not any(link.from_node == principled and link.to_node == output
               for link in material.node_tree.links):
        material.node_tree.links.new(principled.outputs["BSDF"], output.inputs["Surface"])
    principled.inputs["Base Color"].default_value = SKIN_COLOR
    principled.inputs["Roughness"].default_value = 0.62
    principled.inputs["Metallic"].default_value = 0.0
    principled.inputs["Alpha"].default_value = 1.0
    return material


def triangulate_problem_faces(obj, face_indices):
    if not face_indices:
        return []
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bm.faces.ensure_lookup_table()
    bm.faces.index_update()
    selected = [bm.faces[index] for index in sorted(face_indices) if index < len(bm.faces)]
    if len(selected) != len(face_indices):
        bm.free()
        raise RuntimeError(obj.name + ": expected self-intersecting faces not found")
    before = len(selected)
    result = bmesh.ops.triangulate(
        bm,
        faces=selected,
        quad_method="BEAUTY",
        ngon_method="BEAUTY",
    )
    created = len(result.get("faces", []))
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    return [{"sourceFace": index, "trianglesCreated": created} for index in sorted(face_indices)]


def recalculate_normals(obj):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()


def unwrap_mesh(obj):
    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    if len(obj.data.uv_layers) == 0:
        obj.data.uv_layers.new(name="UVMap")
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(island_margin=0.02)
    bpy.ops.object.mode_set(mode="OBJECT")
    obj.data.update()


def assign_skin_material(obj, material):
    obj.data.materials.clear()
    obj.data.materials.append(material)
    for polygon in obj.data.polygons:
        polygon.material_index = 0


def count_deform_weights(obj, armature):
    deform_names = {bone.name for bone in armature.data.bones if bone.use_deform}
    group_names = {group.index: group.name for group in obj.vertex_groups}
    weighted = 0
    unweighted = 0
    for vertex in obj.data.vertices:
        has_weight = any(
            group_names.get(item.group) in deform_names and item.weight > 1e-6
            for item in vertex.groups
        )
        if has_weight:
            weighted += 1
        else:
            unweighted += 1
    return {"weightedVertices": weighted, "unweightedVertices": unweighted}


def main():
    source_path = Path(bpy.data.filepath).resolve()
    if not source_path.is_file():
        raise RuntimeError("Cannot resolve source .blend path")

    skin = make_skin_material()
    report = {
        "source": str(source_path),
        "blenderVersion": bpy.app.version_string,
        "skinMaterial": {
            "name": skin.name,
            "baseColor": list(SKIN_COLOR),
            "alpha": 1.0,
            "backfaceCulling": bool(skin.use_backface_culling),
        },
        "characters": {},
        "errors": [],
    }

    for character, config in CHARACTERS.items():
        armature = bpy.data.objects.get(config["armature"])
        body = bpy.data.objects.get(config["body"])
        if armature is None or armature.type != "ARMATURE" or body is None or body.type != "MESH":
            raise RuntimeError(character + ": armature or body mesh missing")

        alignment = shift_armature_to_body_centerline(armature, body)
        parts = []
        for part_name in config["parts"]:
            part = bpy.data.objects.get(part_name)
            if part is None or part.type != "MESH":
                raise RuntimeError(character + ": mesh missing: " + part_name)
            modifiers = [modifier for modifier in part.modifiers
                         if modifier.type == "ARMATURE" and modifier.object == armature]
            if not modifiers:
                raise RuntimeError(part_name + ": expected Armature modifier missing")

            weights_before = count_deform_weights(part, armature)
            if weights_before["unweightedVertices"] > 0:
                raise RuntimeError(part_name + ": unweighted vertices found before repair")

            triangulated = []
            if part_name == "Human_Male_Body":
                triangulated = triangulate_problem_faces(part, SELF_INTERSECTING_MALE_BODY_FACES)
            recalculate_normals(part)
            unwrap_mesh(part)
            assign_skin_material(part, skin)

            weights_after = count_deform_weights(part, armature)
            if weights_after != weights_before:
                raise RuntimeError(part_name + ": vertex weights changed during visual repair")
            if len(part.data.materials) != 1 or part.data.materials[0] != skin:
                raise RuntimeError(part_name + ": skin material was not assigned")
            uv_layer = part.data.uv_layers.active
            if uv_layer is None or not uv_layer.data:
                raise RuntimeError(part_name + ": UV unwrap failed")
            uv_values = [loop.uv for loop in uv_layer.data]
            uv_span = [
                max(value.x for value in uv_values) - min(value.x for value in uv_values),
                max(value.y for value in uv_values) - min(value.y for value in uv_values),
            ]
            if min(uv_span) < 0.01:
                raise RuntimeError(part_name + ": UV unwrap collapsed")

            parts.append({
                "name": part_name,
                "vertices": len(part.data.vertices),
                "polygonsAfter": len(part.data.polygons),
                "triangulatedFaces": triangulated,
                "material": skin.name,
                "uvSpan": [round(float(value), 6) for value in uv_span],
                "weights": weights_after,
                "armature": armature.name,
            })

        # The static full-body source meshes remain available as non-rigged references.
        static_mesh = bpy.data.objects.get("Human_Male" if character == "male" else "Human_Female")
        if static_mesh is not None and static_mesh.type == "MESH":
            assign_skin_material(static_mesh, skin)
            recalculate_normals(static_mesh)

        report["characters"][character] = {
            "armature": armature.name,
            "alignment": alignment,
            "parts": parts,
        }

    bpy.ops.wm.save_as_mainfile(filepath=str(source_path))
    output = Path("Tools/Blender/player-character-repair-report.json")
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print("UNDERGRIND_PLAYER_CHARACTER_REPAIR=" + json.dumps(report, sort_keys=True))


if __name__ == "__main__":
    main()

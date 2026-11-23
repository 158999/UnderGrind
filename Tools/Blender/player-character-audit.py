import bpy
import json
import math
from pathlib import Path
from mathutils import Vector

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
EPS = 1e-9


def orient(a, b, c):
    return (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])


def proper_segments_intersect(a, b, c, d):
    o1, o2 = orient(a, b, c), orient(a, b, d)
    o3, o4 = orient(c, d, a), orient(c, d, b)
    return o1 * o2 < -EPS and o3 * o4 < -EPS


def polygon_self_intersections(mesh, polygon):
    ids = list(polygon.vertices)
    if len(ids) < 4:
        return []
    normal = polygon.normal
    drop = max(range(3), key=lambda axis: abs(normal[axis]))
    axes = [axis for axis in range(3) if axis != drop]
    points = [(mesh.vertices[index].co[axes[0]], mesh.vertices[index].co[axes[1]]) for index in ids]
    hits = []
    count = len(points)
    for i in range(count):
        a, b = points[i], points[(i + 1) % count]
        for j in range(i + 1, count):
            if j == i or j == (i + 1) % count or (j + 1) % count == i:
                continue
            c, d = points[j], points[(j + 1) % count]
            if proper_segments_intersect(a, b, c, d):
                hits.append({"edges": [i, (i + 1) % count, j, (j + 1) % count],
                             "vertices": [ids[i], ids[(i + 1) % count], ids[j], ids[(j + 1) % count]]})
    return hits


def matrix_rows(matrix):
    return [[round(float(value), 6) for value in row] for row in matrix]


def audit_mesh(obj, armature):
    mesh = obj.data
    inv_armature = armature.matrix_world.inverted()
    local_vertices = [inv_armature @ (obj.matrix_world @ vertex.co) for vertex in mesh.vertices]
    if local_vertices:
        minimum = [min(v[axis] for v in local_vertices) for axis in range(3)]
        maximum = [max(v[axis] for v in local_vertices) for axis in range(3)]
        center = [(minimum[i] + maximum[i]) * 0.5 for i in range(3)]
    else:
        minimum = maximum = center = [0.0, 0.0, 0.0]

    deform_names = {bone.name for bone in armature.data.bones if bone.use_deform}
    group_names = {group.index: group.name for group in obj.vertex_groups}
    weighted = 0
    unweighted = 0
    for vertex in mesh.vertices:
        has_weight = any(
            group_names.get(item.group) in deform_names and item.weight > 1e-6
            for item in vertex.groups
        )
        if has_weight:
            weighted += 1
        else:
            unweighted += 1

    uv_bounds = None
    uv_layer = mesh.uv_layers.active or (mesh.uv_layers[0] if mesh.uv_layers else None)
    if uv_layer and uv_layer.data:
        us = [item.uv.x for item in uv_layer.data]
        vs = [item.uv.y for item in uv_layer.data]
        uv_bounds = {"min": [min(us), min(vs)], "max": [max(us), max(vs)]}

    intersections = []
    zero_area_faces = []
    for polygon in mesh.polygons:
        if polygon.area <= 1e-10:
            zero_area_faces.append(polygon.index)
        hits = polygon_self_intersections(mesh, polygon)
        if hits:
            intersections.append({"polygon": polygon.index, "hits": hits})

    return {
        "name": obj.name,
        "vertices": len(mesh.vertices),
        "polygons": len(mesh.polygons),
        "triangles": sum(max(0, len(poly.vertices) - 2) for poly in mesh.polygons),
        "armatureParent": obj.parent.name if obj.parent else None,
        "armatureModifiers": [m.object.name if m.object else None for m in obj.modifiers if m.type == "ARMATURE"],
        "vertexGroups": len(obj.vertex_groups),
        "weightedVertices": weighted,
        "unweightedVertices": unweighted,
        "armatureLocalBounds": {
            "min": [round(float(v), 6) for v in minimum],
            "max": [round(float(v), 6) for v in maximum],
            "center": [round(float(v), 6) for v in center],
        },
        "uvLayerCount": len(mesh.uv_layers),
        "uvBounds": uv_bounds,
        "materials": [slot.material.name if slot.material else None for slot in obj.material_slots],
        "normalLengthMin": round(min((poly.normal.length for poly in mesh.polygons), default=0.0), 6),
        "zeroAreaFaces": zero_area_faces,
        "selfIntersectingPolygons": intersections,
        "matrixWorld": matrix_rows(obj.matrix_world),
    }


def main():
    report = {
        "blenderVersion": bpy.app.version_string,
        "source": bpy.data.filepath,
        "characters": {},
        "errors": [],
    }

    for name, config in CHARACTERS.items():
        armature = bpy.data.objects.get(config["armature"])
        body = bpy.data.objects.get(config["body"])
        if armature is None or armature.type != "ARMATURE" or body is None or body.type != "MESH":
            report["errors"].append(name + ": armature or body mesh missing")
            continue

        bones = {bone.name: bone for bone in armature.data.bones}
        missing = [bone for bone in CENTER_BONES if bone not in bones]
        if missing:
            report["errors"].append(name + ": central bones missing: " + ", ".join(missing))
            continue

        center_bone_x = sum(float(bones[bone].head_local.x) for bone in CENTER_BONES) / len(CENTER_BONES)
        body_result = audit_mesh(body, armature)
        body_center_x = body_result["armatureLocalBounds"]["center"][0]
        parts = []
        for part_name in config["parts"]:
            part = bpy.data.objects.get(part_name)
            if part is None or part.type != "MESH":
                report["errors"].append(name + ": missing mesh " + part_name)
                continue
            parts.append(audit_mesh(part, armature))

        report["characters"][name] = {
            "armature": armature.name,
            "armatureMatrixWorld": matrix_rows(armature.matrix_world),
            "armatureScale": [round(float(v), 6) for v in armature.scale],
            "bodyCenterlineXInArmatureSpace": round(float(body_center_x), 6),
            "centralBoneCenterlineX": round(float(center_bone_x), 6),
            "skeletonOffsetFromBodyMeters": round(float((center_bone_x - body_center_x) * armature.matrix_world.to_3x3().col[0].length), 6),
            "centralBones": {
                bone: {
                    "head": [round(float(v), 6) for v in bones[bone].head_local],
                    "tail": [round(float(v), 6) for v in bones[bone].tail_local],
                } for bone in CENTER_BONES
            },
            "allBones": [
                {"name": bone.name, "parent": bone.parent.name if bone.parent else None,
                 "head": [round(float(v), 6) for v in bone.head_local],
                 "tail": [round(float(v), 6) for v in bone.tail_local]}
                for bone in armature.data.bones
            ],
            "parts": parts,
        }

    output = Path("Tools/Blender/player-character-audit.json")
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print("UNDERGRIND_PLAYER_CHARACTER_AUDIT=" + json.dumps(report, sort_keys=True))
    if report["errors"]:
        raise RuntimeError("; ".join(report["errors"]))


if __name__ == "__main__":
    main()

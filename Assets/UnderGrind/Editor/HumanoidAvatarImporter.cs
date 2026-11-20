using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnderGrind.Editor
{
    /// <summary>
    /// Generates an explicit Humanoid bone map when Unity's automatic FBX mapping fails.
    /// </summary>
    public static class HumanoidAvatarImporter
    {
        private static readonly string[] RequiredBones =
        {
            "Root", "Hips", "Spine", "Chest", "Neck", "Head",
            "LeftUpperArm", "LeftLowerArm", "LeftHand",
            "RightUpperArm", "RightLowerArm", "RightHand",
            "LeftUpperLeg", "LeftLowerLeg", "LeftFoot",
            "RightUpperLeg", "RightLowerLeg", "RightFoot"
        };

        public static bool EnsureValidAvatar(string assetPath, string armatureRootName)
        {
            if (string.IsNullOrWhiteSpace(assetPath) || string.IsNullOrWhiteSpace(armatureRootName))
                return false;

            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (importer == null || model == null)
            {
                Debug.LogError("UnderGrind: impossibile preparare l'Avatar Humanoid: " + assetPath);
                return false;
            }

            var importerChanged = importer.animationType != ModelImporterAnimationType.Human ||
                                  importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

            if (!importerChanged && FindValidAvatar(assetPath) != null)
                return true;

            var armature = FindDeepChild(model.transform, armatureRootName);
            if (armature == null)
            {
                Debug.LogError("UnderGrind: armatura '" + armatureRootName + "' non trovata in " + assetPath);
                return false;
            }

            var transforms = armature.GetComponentsInChildren<Transform>(true)
                .Where(item => item != armature)
                .ToArray();
            var byName = transforms.GroupBy(item => item.name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var missing = RequiredBones.Where(name => !byName.ContainsKey(name)).ToArray();
            if (missing.Length > 0)
            {
                Debug.LogError("UnderGrind: mapping Humanoid incompleto in " + assetPath +
                    ". Ossa mancanti: " + string.Join(", ", missing));
                return false;
            }

            var skeleton = transforms.Select(item => new SkeletonBone
            {
                name = item.name,
                position = item.localPosition,
                rotation = item.localRotation,
                scale = item.localScale
            }).ToArray();

            var human = RequiredBones.Where(name => name != "Root").Select(name => CreateHumanBone(name)).ToList();
            AddOptionalBone(human, byName, "LeftShoulder");
            AddOptionalBone(human, byName, "RightShoulder");
            AddOptionalBone(human, byName, "LeftToes");
            AddOptionalBone(human, byName, "RightToes");

            importer.humanDescription = new HumanDescription
            {
                human = human.ToArray(),
                skeleton = skeleton,
                armStretch = 0.05f,
                legStretch = 0.05f,
                upperArmTwist = 0.5f,
                lowerArmTwist = 0.5f,
                upperLegTwist = 0.5f,
                lowerLegTwist = 0.5f,
                feetSpacing = 0f,
                hasTranslationDoF = false
            };
            importer.SaveAndReimport();

            if (FindValidAvatar(assetPath) == null)
            {
                Debug.LogError("UnderGrind: Unity non ha generato un Avatar Humanoid valido nemmeno con mapping esplicito per " +
                    assetPath + ". Controllare orientamento e proporzioni delle ossa.");
                return false;
            }

            return true;
        }

        private static Avatar FindValidAvatar(string path) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>()
                .FirstOrDefault(item => item != null && item.isHuman && item.isValid);

        private static HumanBone CreateHumanBone(string name) => new HumanBone
        {
            humanName = name,
            boneName = name,
            limit = new HumanLimit { useDefaultValues = true }
        };

        private static void AddOptionalBone(List<HumanBone> human,
            IReadOnlyDictionary<string, Transform> byName, string name)
        {
            if (byName.ContainsKey(name))
                human.Add(CreateHumanBone(name));
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            if (string.Equals(root.name, name, StringComparison.OrdinalIgnoreCase))
                return root;
            foreach (Transform child in root)
            {
                var found = FindDeepChild(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnderGrind.Editor
{
    /// <summary>
    /// Imports FBX Humanoid avatars while preserving valid manual pose corrections.
    /// If Unity's automatic map is invalid, repairs mappings by exact transform names
    /// before falling back to a clean automatic import.
    /// </summary>
    public static class HumanoidAvatarImporter
    {
        public static bool EnsureValidAvatar(string assetPath, string armatureRootName)
        {
            if (string.IsNullOrWhiteSpace(assetPath) || string.IsNullOrWhiteSpace(armatureRootName))
                return false;

            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null || AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) == null)
            {
                Debug.LogError("UnderGrind: ModelImporter o FBX non disponibile: " + assetPath);
                return false;
            }

            var description = importer.humanDescription;
            var hasManualMapping = description.human != null && description.human.Length > 0 ||
                                  description.skeleton != null && description.skeleton.Length > 0;
            var importerSettingsChanged = importer.animationType != ModelImporterAnimationType.Human ||
                                          importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel;

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

            if (importerSettingsChanged)
            {
                importer.SaveAndReimport();
                importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                if (importer == null)
                {
                    Debug.LogError("UnderGrind: ModelImporter non disponibile dopo la reimportazione: " + assetPath);
                    return false;
                }
            }

            var avatar = LoadValidAvatar(assetPath);
            if (avatar == null)
            {
                // Unity's auto-mapper can assign a real RightHand transform to RightLowerArm.
                // Rebuild the Humanoid map by exact transform names while preserving the
                // importer-generated skeleton and its sampled pose.
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (TryBuildExactNameMapping(model, description, out var exactDescription))
                {
                    importer.humanDescription = exactDescription;
                    importer.SaveAndReimport();
                    importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                    avatar = LoadValidAvatar(assetPath);
                }
            }

            if (avatar == null && hasManualMapping)
            {
                // Unity's Avatar Configuration writes pose corrections into humanDescription.
                // Keep those corrections whenever they produce a valid Avatar; only discard
                // an invalid manual description before falling back to automatic mapping.
                description = importer.humanDescription;
                description.human = Array.Empty<HumanBone>();
                description.skeleton = Array.Empty<SkeletonBone>();
                importer.humanDescription = description;
                importer.SaveAndReimport();
                avatar = LoadValidAvatar(assetPath);
            }

            if (avatar == null)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                var currentDescription = importer.humanDescription;
                var mappedBones = currentDescription.human == null
                    ? "nessun mapping Humanoid serializzato"
                    : string.Join(", ", currentDescription.human.Select(item =>
                        item.humanName + " -> " + item.boneName));
                var skeletonBones = currentDescription.skeleton == null
                    ? "nessuno skeleton serializzato"
                    : string.Join(", ", currentDescription.skeleton.Select(item =>
                        item.name));
                var hierarchy = model == null
                    ? "FBX hierarchy non disponibile"
                    : string.Join(Environment.NewLine, model.GetComponentsInChildren<Transform>(true)
                        .Select(GetTransformPath));

                Debug.LogError("UnderGrind: Avatar Humanoid non valido: " + assetPath +
                               Environment.NewLine + "Mapping importer: " + mappedBones +
                               Environment.NewLine + "Skeleton importer: " + skeletonBones +
                               Environment.NewLine + "Transform FBX importati:" +
                               Environment.NewLine + hierarchy +
                               Environment.NewLine + "Root armature configurata: " + armatureRootName);
                return false;
            }

            return true;
        }

        private static bool TryBuildExactNameMapping(
            GameObject model,
            HumanDescription sourceDescription,
            out HumanDescription correctedDescription)
        {
            correctedDescription = sourceDescription;
            if (model == null || sourceDescription.skeleton == null || sourceDescription.skeleton.Length == 0)
                return false;

            var transforms = model.GetComponentsInChildren<Transform>(true)
                .GroupBy(transform => transform.name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

            var requiredBones = new[]
            {
                "Hips", "Spine", "Chest", "Neck", "Head",
                "LeftShoulder", "RightShoulder",
                "LeftUpperArm", "RightUpperArm",
                "LeftLowerArm", "RightLowerArm",
                "LeftHand", "RightHand",
                "LeftUpperLeg", "RightUpperLeg",
                "LeftLowerLeg", "RightLowerLeg",
                "LeftFoot", "RightFoot"
            };

            if (requiredBones.Any(boneName => !transforms.ContainsKey(boneName)))
            {
                Debug.LogError("UnderGrind: mapping Humanoid esplicito impossibile; ossa mancanti: " +
                               string.Join(", ", requiredBones.Where(boneName => !transforms.ContainsKey(boneName))));
                return false;
            }

            var mappings = HumanTrait.BoneName
                .Where(boneName => !string.IsNullOrEmpty(boneName) && transforms.ContainsKey(boneName))
                .Select(boneName => new HumanBone
                {
                    humanName = boneName,
                    boneName = boneName,
                    limit = new HumanLimit { useDefaultValues = true }
                })
                .ToArray();

            correctedDescription.human = mappings;
            return requiredBones.All(required =>
                mappings.Any(mapping => mapping.humanName == required && mapping.boneName == required));
        }

        private static string GetTransformPath(Transform transform)
        {
            var segments = new System.Collections.Generic.Stack<string>();
            while (transform != null)
            {
                segments.Push(transform.name);
                transform = transform.parent;
            }

            return string.Join("/", segments);
        }

        private static Avatar LoadValidAvatar(string assetPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Avatar>()
                .FirstOrDefault(item => item != null && item.isHuman && item.isValid);
        }
    }
}
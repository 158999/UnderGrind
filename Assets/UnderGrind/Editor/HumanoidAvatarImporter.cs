using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnderGrind.Editor
{
    /// <summary>
    /// Keeps FBX Humanoid import on Unity's native hierarchy-aware Avatar mapping.
    /// Valid manual Avatar corrections (including Enforce T-Pose) are preserved.
    /// Invalid manual descriptions fall back to Unity's automatic hierarchy mapping.
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
                Debug.LogError("UnderGrind: Unity non ha generato un Avatar Humanoid valido per " +
                               assetPath + ". Verificare il mapping e la posa del rig.");
                return false;
            }

            return true;
        }

        private static Avatar LoadValidAvatar(string assetPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Avatar>()
                .FirstOrDefault(item => item != null && item.isHuman && item.isValid);
        }
    }
}
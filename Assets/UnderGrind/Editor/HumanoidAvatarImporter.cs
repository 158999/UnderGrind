using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnderGrind.Editor
{
    /// <summary>
    /// Keeps FBX Humanoid import on Unity's native hierarchy-aware Avatar mapping.
    /// A hand-built HumanDescription cannot represent parent links through SkeletonBone
    /// and can flatten the skeleton, so manual mappings are deliberately discarded.
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
            var changed = importer.animationType != ModelImporterAnimationType.Human ||
                          importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel ||
                          hasManualMapping;

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

            if (hasManualMapping)
            {
                description.human = System.Array.Empty<HumanBone>();
                description.skeleton = System.Array.Empty<SkeletonBone>();
                importer.humanDescription = description;
            }

            if (changed)
                importer.SaveAndReimport();

            var avatar = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Avatar>()
                .FirstOrDefault(item => item != null && item.isHuman && item.isValid);
            if (avatar == null)
            {
                Debug.LogError("UnderGrind: Unity non ha generato un Avatar Humanoid valido per " +
                               assetPath + ". Il mapping automatico non è stato alterato manualmente.");
                return false;
            }

            return true;
        }
    }
}
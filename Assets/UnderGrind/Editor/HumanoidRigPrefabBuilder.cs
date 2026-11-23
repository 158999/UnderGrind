using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnderGrind.Locomotion;

namespace UnderGrind.Editor
{
    /// <summary>
    /// Creates separate male and female Humanoid prefabs from their own rigged FBX sources.
    /// Each prefab receives the Avatar imported from its corresponding FBX.
    /// </summary>
    public static class HumanoidRigPrefabBuilder
    {
        private const string OutputFolder = "Assets/UnderGrind/Generated/Prefabs";
        private const string RigSettingsPath = "Assets/UnderGrind/Settings/PlayerRigSettings.asset";
        private const string LocomotionSettingsPath = "Assets/UnderGrind/Settings/PlayerLocomotionSettings.asset";
        private const string SkinMaterialPath = "Assets/UnderGrind/Generated/Fallback_Skin.mat";

        [MenuItem("UnderGrind/Player/Build Male and Female Prefabs From Configured FBX")]
        public static void BuildFromConfiguredSources()
        {
            PlayerGameplayAssetBuilder.EnsureAssets();
            var rigSettings = AssetDatabase.LoadAssetAtPath<PlayerRigSettings>(RigSettingsPath);
            var locomotionSettings = AssetDatabase.LoadAssetAtPath<PlayerLocomotionSettings>(LocomotionSettingsPath);
            if (rigSettings == null || locomotionSettings == null)
            {
                Debug.LogError("UnderGrind: asset PlayerRigSettings o PlayerLocomotionSettings mancante.");
                return;
            }

            var malePath = rigSettings.MaleSourceAssetPath;
            var femalePath = rigSettings.FemaleSourceAssetPath;
            if (!IsFbxPath(malePath) || !IsFbxPath(femalePath) ||
                string.Equals(malePath, femalePath, StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("Sorgenti rig non valide",
                    "Configura due percorsi FBX distinti in PlayerRigSettings: uno per il modello maschile e uno per quello femminile.",
                    "OK");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(malePath) == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(femalePath) == null)
            {
                EditorUtility.DisplayDialog("FBX riggati mancanti",
                    "Non trovo entrambi i file configurati in PlayerRigSettings.\n\nMaschile: " + malePath +
                    "\nFemminile: " + femalePath +
                    "\n\nImporta i due FBX nel progetto e ripeti l'operazione.",
                    "OK");
                return;
            }

            var prefabPaths = new[]
            {
                OutputFolder + "/" + rigSettings.MalePrefabName + ".prefab",
                OutputFolder + "/" + rigSettings.FemalePrefabName + ".prefab"
            };
            var existingPrefabPaths = prefabPaths
                .Where(path => AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                .ToArray();
            var overwriteDetails = existingPrefabPaths.Length > 0
                ? "\n\nPrefab esistenti da sostituire:\n- " + string.Join("\n- ", existingPrefabPaths)
                : "\n\nNon esistono ancora prefab di destinazione.";

            if (!EditorUtility.DisplayDialog(
                "Generare i prefab Humanoid?",
                "Verranno importati due FBX distinti, ciascuno con il proprio Avatar Humanoid. " +
                "Le sorgenti sono:\nMaschile: " + malePath + "\nFemminile: " + femalePath +
                overwriteDetails,
                "Continua", "Annulla"))
                return;

            if (!TryPrepareSource(malePath, rigSettings, rigSettings.MaleArmatureRootName, out var maleSource, out var maleAvatar))
                return;
            if (!TryPrepareSource(femalePath, rigSettings, rigSettings.FemaleArmatureRootName, out var femaleSource, out var femaleAvatar))
                return;

            if (maleAvatar == femaleAvatar)
            {
                Debug.LogError("UnderGrind: i due FBX hanno restituito lo stesso Avatar. I prefab non verranno generati.");
                return;
            }

            var maleTemporary = CreatePrefabInstance(maleSource, maleAvatar,
                rigSettings.MaleArmatureRootName, rigSettings.MalePrefabName,
                locomotionSettings.CharacterHeight, rigSettings);
            var femaleTemporary = CreatePrefabInstance(femaleSource, femaleAvatar,
                rigSettings.FemaleArmatureRootName, rigSettings.FemalePrefabName,
                locomotionSettings.FemaleCharacterHeight, rigSettings);

            if (maleTemporary == null || femaleTemporary == null)
            {
                if (maleTemporary != null) UnityEngine.Object.DestroyImmediate(maleTemporary);
                if (femaleTemporary != null) UnityEngine.Object.DestroyImmediate(femaleTemporary);
                Debug.LogError("UnderGrind: validazione dei due rig non superata. Nessun nuovo prefab salvato.");
                return;
            }

            EnsureFolder("Assets/UnderGrind/Generated");
            EnsureFolder(OutputFolder);
            var malePrefab = PrefabUtility.SaveAsPrefabAsset(maleTemporary,
                OutputFolder + "/" + rigSettings.MalePrefabName + ".prefab");
            var femalePrefab = PrefabUtility.SaveAsPrefabAsset(femaleTemporary,
                OutputFolder + "/" + rigSettings.FemalePrefabName + ".prefab");
            UnityEngine.Object.DestroyImmediate(maleTemporary);
            UnityEngine.Object.DestroyImmediate(femaleTemporary);

            if (malePrefab == null || femalePrefab == null)
            {
                Debug.LogError("UnderGrind: salvataggio di uno o entrambi i prefab non riuscito.");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("UnderGrind",
                "Prefab generati in " + OutputFolder + "\nMaschio: " + rigSettings.MalePrefabName +
                "\nFemmina: " + rigSettings.FemalePrefabName,
                "OK");
        }

        private static bool TryPrepareSource(string path, PlayerRigSettings settings, string armatureRootName,
            out GameObject source, out Avatar avatar)
        {
            source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            avatar = null;
            if (source == null)
            {
                Debug.LogError("UnderGrind: FBX sorgente non trovato: " + path);
                return false;
            }

            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError("UnderGrind: ModelImporter non disponibile per " + path);
                return false;
            }

            var changed = false;
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                changed = true;
            }
            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                changed = true;
            }
            if (importer.importAnimation != settings.ImportAnimation)
            {
                importer.importAnimation = settings.ImportAnimation;
                changed = true;
            }
            if (importer.materialImportMode != (settings.ImportMaterials ? ModelImporterMaterialImportMode.ImportViaMaterialDescription : ModelImporterMaterialImportMode.None))
            {
                importer.materialImportMode = settings.ImportMaterials ? ModelImporterMaterialImportMode.ImportViaMaterialDescription : ModelImporterMaterialImportMode.None;
                changed = true;
            }
            if (importer.importCameras != settings.ImportCameras)
            {
                importer.importCameras = settings.ImportCameras;
                changed = true;
            }
            if (importer.importLights != settings.ImportLights)
            {
                importer.importLights = settings.ImportLights;
                changed = true;
            }
            if (importer.optimizeGameObjects != settings.OptimizeGameObjects)
            {
                importer.optimizeGameObjects = settings.OptimizeGameObjects;
                changed = true;
            }
            if (importer.importVisibility != settings.ImportVisibility)
            {
                importer.importVisibility = settings.ImportVisibility;
                changed = true;
            }
            if (changed) importer.SaveAndReimport();

            if (!HumanoidAvatarImporter.EnsureValidAvatar(path, armatureRootName))
            {
                source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                avatar = null;
                return false;
            }

            source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>()
                .FirstOrDefault(item => item != null && item.isHuman && item.isValid);
            if (source == null || avatar == null)
            {
                Debug.LogError("UnderGrind: Unity non ha generato un Avatar Humanoid valido per " + path +
                    ". Controlla il mapping delle ossa nel Model Importer.");
                return false;
            }
            return true;
        }
        private static GameObject CreatePrefabInstance(GameObject source, Avatar avatar, string rootName,
            string prefabName, float targetHeight, PlayerRigSettings rigSettings)
        {
            var sourceArmature = FindDeepChild(source.transform, rootName);
            if (sourceArmature == null)
            {
                Debug.LogError("UnderGrind: armature root non trovato in " + source.name + ": " + rootName);
                return null;
            }

            // Keep the FBX root: the skinned meshes are often siblings of the armature,
            // not children of the armature transform itself.
            var temporary = UnityEngine.Object.Instantiate(source);
            temporary.name = prefabName;
            // The authored human source faces -X. Rotate it into the Player local +Z forward
            // before measuring bounds and saving the prefab; the offset remains Inspector-configurable.
            temporary.transform.localRotation = Quaternion.Euler(0f, rigSettings.ModelYawOffset, 0f);
            var animator = temporary.GetComponent<Animator>();
            if (animator == null) animator = temporary.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.applyRootMotion = false;

            var visualRig = temporary.GetComponent<PlayerVisualRig>();
            if (visualRig == null) visualRig = temporary.AddComponent<PlayerVisualRig>();
            if (!visualRig.ConfigureFromHierarchy(temporary.transform, rigSettings))
            {
                Debug.LogError("UnderGrind: il rig " + rootName +
                    " non soddisfa la configurazione di renderer testa/braccia.");
                UnityEngine.Object.DestroyImmediate(temporary);
                return null;
            }

            // Generated prefabs must open as complete third-person characters.
            // First-person visibility is selected later by the gameplay camera mode.
            visualRig.SetThirdPerson(true);

            if (!AssignOpaqueSkinMaterial(temporary))
            {
                UnityEngine.Object.DestroyImmediate(temporary);
                return null;
            }

            if (temporary.GetComponentsInChildren<Renderer>(true).Length == 0)
            {
                Debug.LogError("UnderGrind: nessun Renderer presente nel rig " + rootName);
                UnityEngine.Object.DestroyImmediate(temporary);
                return null;
            }

            try
            {
                FitToHeight(temporary, targetHeight);
            }
            catch (Exception exception)
            {
                Debug.LogError("UnderGrind: impossibile normalizzare l'altezza di " + prefabName + ": " + exception.Message);
                UnityEngine.Object.DestroyImmediate(temporary);
                return null;
            }
            return temporary;
        }

        private static bool AssignOpaqueSkinMaterial(GameObject root)
        {
            var skin = AssetDatabase.LoadAssetAtPath<Material>(SkinMaterialPath);
            if (skin == null)
            {
                Debug.LogError("UnderGrind: materiale pelle non disponibile: " + SkinMaterialPath);
                return false;
            }

            var flesh = new Color(0.72f, 0.55f, 0.43f, 1f);
            if (skin.HasProperty("_BaseColor")) skin.SetColor("_BaseColor", flesh);
            if (skin.HasProperty("_Color")) skin.SetColor("_Color", flesh);
            if (skin.HasProperty("_Surface")) skin.SetFloat("_Surface", 0f);
            if (skin.HasProperty("_AlphaClip")) skin.SetFloat("_AlphaClip", 0f);
            if (skin.HasProperty("_ZWrite")) skin.SetFloat("_ZWrite", 1f);
            if (skin.HasProperty("_Cull")) skin.SetFloat("_Cull", 0f);
            skin.renderQueue = -1;
            EditorUtility.SetDirty(skin);

            var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length == 0)
            {
                Debug.LogError("UnderGrind: nessuno SkinnedMeshRenderer da colorare in " + root.name);
                return false;
            }

            foreach (var renderer in renderers)
            {
                var subMeshCount = renderer.sharedMesh != null ? renderer.sharedMesh.subMeshCount : 1;
                renderer.sharedMaterials = Enumerable.Repeat(skin, Mathf.Max(1, subMeshCount)).ToArray();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            return true;
        }

        private static bool IsFbxPath(string path) =>
            !string.IsNullOrWhiteSpace(path) &&
            path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) &&
            path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase);

        private static Transform FindDeepChild(Transform root, string targetName)
        {
            if (root.name.Equals(targetName, StringComparison.OrdinalIgnoreCase)) return root;
            foreach (Transform child in root)
            {
                var found = FindDeepChild(child, targetName);
                if (found != null) return found;
            }
            return null;
        }

        private static void FitToHeight(GameObject model, float targetHeight)
        {
            var bounds = GetBounds(model);
            if (bounds.size.y <= Mathf.Epsilon || targetHeight <= Mathf.Epsilon)
                throw new InvalidOperationException("Altezza del rig o altezza obiettivo non valida.");

            model.transform.localScale *= targetHeight / bounds.size.y;
            bounds = GetBounds(model);
            model.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        private static Bounds GetBounds(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(model.transform.position, Vector3.zero);
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
            var name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnderGrind.Locomotion;

namespace UnderGrind.Editor
{
    /// <summary>
    /// Validates a rigged Humanoid FBX and creates the configured male/female prefabs.
    /// The source mesh must already contain a valid skeleton and a separate head renderer.
    /// </summary>
    public static class HumanoidRigPrefabBuilder
    {
        private const string OutputFolder = "Assets/UnderGrind/Generated/Prefabs";
        private const string RigSettingsPath = "Assets/UnderGrind/Settings/PlayerRigSettings.asset";
        private const string LocomotionSettingsPath = "Assets/UnderGrind/Settings/PlayerLocomotionSettings.asset";

        [MenuItem("UnderGrind/Player/Build Prefabs From Selected Rigged FBX")]
        public static void BuildFromSelected()
        {
            PlayerGameplayAssetBuilder.EnsureAssets();
            var rigSettings = AssetDatabase.LoadAssetAtPath<PlayerRigSettings>(RigSettingsPath);
            var locomotionSettings = AssetDatabase.LoadAssetAtPath<PlayerLocomotionSettings>(LocomotionSettingsPath);
            if (rigSettings == null || locomotionSettings == null)
            {
                Debug.LogError("UnderGrind: asset PlayerRigSettings o PlayerLocomotionSettings mancante.");
                return;
            }

            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("UnderGrind", "Seleziona il file FBX riggato nel Project window.", "OK");
                return;
            }

            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError("UnderGrind: il file selezionato non ha un ModelImporter: " + path);
                return;
            }

            var prefabPaths = new[]
            {
                OutputFolder + "/" + rigSettings.MalePrefabName + ".prefab",
                OutputFolder + "/" + rigSettings.FemalePrefabName + ".prefab"
            };
            var existingPrefabPaths = prefabPaths
                .Where(prefabPath => AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                .ToArray();
            var overwriteDetails = existingPrefabPaths.Length > 0
                ? "\n\nPrefab esistenti da sostituire:\n- " + string.Join("\n- ", existingPrefabPaths)
                : "\n\nNon esistono ancora prefab di destinazione.";

            if (!EditorUtility.DisplayDialog(
                "Configurare il FBX e generare i prefab?",
                "Le impostazioni di importazione del FBX selezionato verranno impostate su Humanoid e Avatar From This Model. " +
                "La generazione può sostituire i prefab già presenti." + overwriteDetails,
                "Continua", "Annulla"))
                return;

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = rigSettings.ImportAnimation;
            importer.optimizeGameObjects = rigSettings.OptimizeGameObjects;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = rigSettings.ImportVisibility;
            importer.SaveAndReimport();

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>()
                .FirstOrDefault(x => x != null && x.isHuman && x.isValid);
            if (source == null || avatar == null)
            {
                Debug.LogError("UnderGrind: Unity non ha generato un Avatar Humanoid valido per " + path +
                    ". Controlla il mapping delle ossa nel Model Importer.");
                EditorUtility.DisplayDialog("Avatar Humanoid non valido",
                    "Il FBX è stato configurato come Humanoid, ma Unity non ha generato un Avatar valido. " +
                    "I prefab non sono stati creati.", "OK");
                return;
            }

            EnsureFolder("Assets/UnderGrind/Generated");
            EnsureFolder(OutputFolder);
            var male = BuildPrefab(source, avatar, rigSettings.MaleArmatureRootName,
                rigSettings.MalePrefabName, locomotionSettings.CharacterHeight, rigSettings);
            var female = BuildPrefab(source, avatar, rigSettings.FemaleArmatureRootName,
                rigSettings.FemalePrefabName, locomotionSettings.FemaleCharacterHeight, rigSettings);
            if (male == null || female == null)
            {
                Debug.LogError("UnderGrind: generazione prefab incompleta. Verificare radici scheletro e renderer separati.");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("UnderGrind",
                "Prefab generati in " + OutputFolder + "\n" +
                "Maschio: " + rigSettings.MalePrefabName + ".prefab\n" +
                "Femmina: " + rigSettings.FemalePrefabName + ".prefab",
                "OK");
        }

        private static GameObject BuildPrefab(GameObject source, Avatar avatar, string rootName,
            string prefabName, float targetHeight, PlayerRigSettings rigSettings)
        {
            var sourceRoot = FindDeepChild(source.transform, rootName);
            if (sourceRoot == null)
            {
                Debug.LogError("UnderGrind: armature root non trovato: " + rootName);
                return null;
            }

            var temporary = UnityEngine.Object.Instantiate(sourceRoot.gameObject);
            temporary.name = prefabName;
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

            var renderers = temporary.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Debug.LogError("UnderGrind: nessun Renderer presente nel rig " + rootName);
                UnityEngine.Object.DestroyImmediate(temporary);
                return null;
            }

            if (!rigSettings.RequireFirstPersonArms &&
                !renderers.Any(x => x.name.IndexOf(rigSettings.FirstPersonArmsToken,
                    StringComparison.OrdinalIgnoreCase) >= 0))
                Debug.LogWarning("UnderGrind: " + prefabName +
                    " non contiene renderer FP dedicati; in prima persona resterà visibile il corpo riggato.");

            FitToHeight(temporary, targetHeight);
            var prefabPath = OutputFolder + "/" + prefabName + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(temporary, prefabPath);
            UnityEngine.Object.DestroyImmediate(temporary);
            if (prefab == null) Debug.LogError("UnderGrind: salvataggio prefab fallito: " + prefabPath);
            else Debug.Log("UnderGrind: prefab creato: " + prefabPath);
            return prefab;
        }

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
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
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
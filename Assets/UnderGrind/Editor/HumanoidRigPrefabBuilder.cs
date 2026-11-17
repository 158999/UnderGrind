using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnderGrind.Locomotion;

namespace UnderGrind.Editor
{
    /// <summary>
    /// Configures an exported UnderGrind humanoid FBX as a Unity Humanoid model
    /// and creates male/female prefabs from its named armature roots.
    /// </summary>
    public static class HumanoidRigPrefabBuilder
    {
        private const string OutputFolder = "Assets/UnderGrind/Generated/Prefabs";

        [MenuItem("UnderGrind/Player/Build Prefabs From Selected Rigged FBX")]
        public static void BuildFromSelected()
        {
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

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.optimizeGameObjects = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = true;
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
            var male = BuildPrefab(source, avatar, "UG_Male_Armature", "Human_Male_Rigged", 1.78f);
            var female = BuildPrefab(source, avatar, "UG_Female_Armature", "Human_Female_Rigged", 1.70f);
            if (male == null || female == null)
            {
                Debug.LogError("UnderGrind: generazione prefab incompleta. Verificare i nomi degli armature root nel FBX.");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("UnderGrind",
                "Prefab generati in " + OutputFolder + "\n" +
                "Maschio: Human_Male_Rigged.prefab\n" +
                "Femmina: Human_Female_Rigged.prefab\n\n" +
                "Il binding testa è configurato. Le braccia FP dedicate richiedono ancora una mesh/posa separata.",
                "OK");
        }

        private static GameObject BuildPrefab(GameObject source, Avatar avatar, string rootName,
            string prefabName, float targetHeight)
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
            if (!visualRig.ConfigureFromHierarchy(temporary.transform))
            {
                Debug.LogError("UnderGrind: nessun renderer separato *_Head trovato in " + rootName);
                UnityEngine.Object.DestroyImmediate(temporary);
                return null;
            }

            if (temporary.GetComponentsInChildren<Renderer>(true).All(x =>
                    !x.name.Contains("_FP_Arms", StringComparison.OrdinalIgnoreCase) &&
                    !x.name.Contains("_FP_Hands", StringComparison.OrdinalIgnoreCase)))
                Debug.LogWarning("UnderGrind: " + prefabName +
                    " non contiene renderer FP dedicati; in prima persona verrà mostrato il corpo riggato.");

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
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            if (bounds.size.y < 0.05f) return;

            model.transform.localScale *= targetHeight / bounds.size.y;
            bounds = GetBounds(model);
            model.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        private static Bounds GetBounds(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true);
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

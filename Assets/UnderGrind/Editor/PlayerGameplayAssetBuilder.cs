using System.IO;
using UnityEditor;
using UnityEngine;
using UnderGrind.Inventory;
using UnderGrind.Locomotion;

namespace UnderGrind.Editor
{
    [InitializeOnLoad]
    public static class PlayerGameplayAssetBuilder
    {
        private const string SettingsFolder = "Assets/UnderGrind/Settings";
        private const string DefaultsFolder = SettingsFolder + "/Defaults";
        private const string InteractionSettingsPath = SettingsFolder + "/PlayerInteractionSettings.asset";
        private const string InventoryUISettingsPath = SettingsFolder + "/PlayerInventoryUISettings.asset";
        private const string AnimationSettingsPath = SettingsFolder + "/PlayerAnimationSettings.asset";
        private const string RigSettingsPath = SettingsFolder + "/PlayerRigSettings.asset";

        static PlayerGameplayAssetBuilder()
        {
            EditorApplication.delayCall += EnsureAssets;
        }

        [MenuItem("UnderGrind/Player/Create or Repair Inventory Assets")]
        public static void EnsureAssets()
        {
            EnsureFolder("Assets/UnderGrind");
            EnsureFolder(SettingsFolder);
            EnsureFolder(DefaultsFolder);

            EnsureAsset<PlayerInteractionSettings>(
                InteractionSettingsPath, DefaultsFolder + "/PlayerInteractionSettings.json");
            EnsureAsset<InventoryUISettings>(
                InventoryUISettingsPath, DefaultsFolder + "/InventoryUISettings.json");
            EnsureAsset<PlayerAnimationSettings>(
                AnimationSettingsPath, DefaultsFolder + "/PlayerAnimationSettings.json");
            EnsureAsset<PlayerRigSettings>(
                RigSettingsPath, DefaultsFolder + "/PlayerRigSettings.json");

            AssetDatabase.SaveAssets();
        }

        private static T EnsureAsset<T>(string assetPath, string defaultsPath) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            if (File.Exists(defaultsPath))
                JsonUtility.FromJsonOverwrite(File.ReadAllText(defaultsPath), asset);
            else
                Debug.LogError("UnderGrind: file di configurazione non trovato: " + defaultsPath);

            AssetDatabase.CreateAsset(asset, assetPath);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
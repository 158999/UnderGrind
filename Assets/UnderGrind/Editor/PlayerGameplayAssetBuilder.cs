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
            var inventoryUISettings = EnsureAsset<InventoryUISettings>(
                InventoryUISettingsPath, DefaultsFolder + "/InventoryUISettings.json");
            MigrateInventoryUISettings(inventoryUISettings, DefaultsFolder + "/InventoryUISettings.json");
            var animationSettings = EnsureAsset<PlayerAnimationSettings>(
                AnimationSettingsPath, DefaultsFolder + "/PlayerAnimationSettings.json");
            MigrateAnimationSettings(animationSettings, DefaultsFolder + "/PlayerAnimationSettings.json");
            var rigSettings = EnsureAsset<PlayerRigSettings>(
                RigSettingsPath, DefaultsFolder + "/PlayerRigSettings.json");
            MigrateRigSettings(rigSettings, DefaultsFolder + "/PlayerRigSettings.json");

            AssetDatabase.SaveAssets();
        }

        private static void MigrateInventoryUISettings(InventoryUISettings asset, string defaultsPath)
        {
            if (asset == null || !File.Exists(defaultsPath)) return;
            var defaults = ScriptableObject.CreateInstance<InventoryUISettings>();
            JsonUtility.FromJsonOverwrite(File.ReadAllText(defaultsPath), defaults);
            var current = new SerializedObject(asset);
            var template = new SerializedObject(defaults);
            var version = current.FindProperty("settingsVersion");
            if (version == null || version.intValue >= 1)
            {
                Object.DestroyImmediate(defaults);
                return;
            }

            if (current.FindProperty("panelAnchorMax").vector2Value == Vector2.zero)
            {
                CopyInt(current, template, "canvasRenderMode");
                CopyInt(current, template, "canvasScaleMode");
                CopyVector2(current, template, "panelAnchorMin");
                CopyVector2(current, template, "panelAnchorMax");
                CopyVector2(current, template, "panelPivot");
                CopyVector2(current, template, "titleAnchorMin");
                CopyVector2(current, template, "titleAnchorMax");
                CopyVector2(current, template, "titlePivot");
                CopyVector2(current, template, "weightAnchorMin");
                CopyVector2(current, template, "weightAnchorMax");
                CopyVector2(current, template, "weightPivot");
                CopyVector2(current, template, "gridAnchorMin");
                CopyVector2(current, template, "gridAnchorMax");
                CopyInt(current, template, "gridStartCorner");
                CopyInt(current, template, "gridStartAxis");
                CopyInt(current, template, "gridConstraint");
                CopyInt(current, template, "titleAlignment");
                CopyInt(current, template, "weightAlignment");
                CopyInt(current, template, "slotAlignment");
                CopyInt(current, template, "gridChildAlignment");
            }

            if (current.FindProperty("iconAnchorMax").vector2Value == Vector2.zero)
            {
                CopyVector2(current, template, "iconAnchorMin");
                CopyVector2(current, template, "iconAnchorMax");
                CopyVector2(current, template, "labelAnchorMin");
                CopyVector2(current, template, "labelAnchorMax");
                CopyBool(current, template, "preserveIconAspect");
            }

            version.intValue = 1;
            current.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            Object.DestroyImmediate(defaults);
        }

        private static void MigrateAnimationSettings(PlayerAnimationSettings asset, string defaultsPath)
        {
            if (asset == null || !File.Exists(defaultsPath)) return;
            var defaults = ScriptableObject.CreateInstance<PlayerAnimationSettings>();
            JsonUtility.FromJsonOverwrite(File.ReadAllText(defaultsPath), defaults);
            var current = new SerializedObject(asset);
            var template = new SerializedObject(defaults);
            var version = current.FindProperty("settingsVersion");
            var defaultVersion = template.FindProperty("settingsVersion");
            if (version != null && defaultVersion != null && version.intValue < defaultVersion.intValue)
            {
                version.intValue = defaultVersion.intValue;
                current.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }
            Object.DestroyImmediate(defaults);
        }

        private static void MigrateRigSettings(PlayerRigSettings asset, string defaultsPath)
        {
            if (asset == null || !File.Exists(defaultsPath)) return;
            var defaults = ScriptableObject.CreateInstance<PlayerRigSettings>();
            JsonUtility.FromJsonOverwrite(File.ReadAllText(defaultsPath), defaults);
            var current = new SerializedObject(asset);
            var template = new SerializedObject(defaults);
            var version = current.FindProperty("settingsVersion");
            var defaultVersion = template.FindProperty("settingsVersion");
            CopyStringIfEmpty(current, template, "maleSourceAssetPath");
            CopyStringIfEmpty(current, template, "femaleSourceAssetPath");
            CopyStringIfEmpty(current, template, "thirdPersonArmsToken");
            CopyStringIfEmpty(current, template, "thirdPersonHandsToken");
            if (version != null && defaultVersion != null && version.intValue < defaultVersion.intValue)
                version.intValue = defaultVersion.intValue;

            current.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            Object.DestroyImmediate(defaults);
        }

        private static void CopyStringIfEmpty(SerializedObject target, SerializedObject source, string name)
        {
            var property = target.FindProperty(name);
            var defaultProperty = source.FindProperty(name);
            if (property != null && defaultProperty != null && string.IsNullOrWhiteSpace(property.stringValue))
                property.stringValue = defaultProperty.stringValue;
        }

        private static void CopyVector2(SerializedObject target, SerializedObject source, string name)
        {
            target.FindProperty(name).vector2Value = source.FindProperty(name).vector2Value;
        }

        private static void CopyInt(SerializedObject target, SerializedObject source, string name)
        {
            target.FindProperty(name).intValue = source.FindProperty(name).intValue;
        }

        private static void CopyBool(SerializedObject target, SerializedObject source, string name)
        {
            target.FindProperty(name).boolValue = source.FindProperty(name).boolValue;
        }
        private static T EnsureAsset<T>(string assetPath, string defaultsPath) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null) return asset;

            if (!File.Exists(defaultsPath))
            {
                Debug.LogError("UnderGrind: file di configurazione non trovato: " + defaultsPath);
                return null;
            }

            asset = ScriptableObject.CreateInstance<T>();
            JsonUtility.FromJsonOverwrite(File.ReadAllText(defaultsPath), asset);
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
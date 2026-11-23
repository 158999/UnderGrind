using System.IO;
using UnityEditor;
using UnityEngine;
using UnderGrind.Inventory;
using UnderGrind.Locomotion;
using UnderGrind.Testing;

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
        private const string GrayboxSettingsPath = SettingsFolder + "/PlayerGrayboxSettings.asset";

        static PlayerGameplayAssetBuilder()
        {
            EditorApplication.delayCall += EnsureAssets;
        }

        [MenuItem("UnderGrind/Player/Create or Repair Player Settings Assets")]
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
            EnsureAsset<PlayerGrayboxSettings>(
                GrayboxSettingsPath, DefaultsFolder + "/PlayerGrayboxSettings.json");

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
            if (version == null)
            {
                Object.DestroyImmediate(defaults);
                return;
            }

            if (version.intValue < 1 &&
                current.FindProperty("panelAnchorMax").vector2Value == Vector2.zero)
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

            if (version.intValue < 1 &&
                current.FindProperty("iconAnchorMax").vector2Value == Vector2.zero)
            {
                CopyVector2(current, template, "iconAnchorMin");
                CopyVector2(current, template, "iconAnchorMax");
                CopyVector2(current, template, "labelAnchorMin");
                CopyVector2(current, template, "labelAnchorMax");
                CopyBool(current, template, "preserveIconAspect");
            }

            if (version.intValue < 2)
            {
                CopyStringIfEmpty(current, template, "fallbackFontResourceName");
                version.intValue = 2;
            }
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
            var previousVersion = version != null ? version.intValue : 0;
            if (version != null && defaultVersion != null && previousVersion < defaultVersion.intValue)
            {
                CopyBool(current, template, "configureFbxImports");
                CopyInt(current, template, "fbxRigType");
                CopyBool(current, template, "createAvatarFromFbx");
                CopyBool(current, template, "importAnimationClips");
                CopyBool(current, template, "importMaterials");
                CopyBool(current, template, "importCameras");
                CopyBool(current, template, "importLights");
                CopyBool(current, template, "optimizeFbxGameObjects");

                // The former default (2 m/s) exceeded this Player's 1.8667 m/s run speed,
                // making the Run blend-tree child unreachable. Preserve custom lower values.
                var runThreshold = current.FindProperty("runThreshold");
                var defaultRunThreshold = template.FindProperty("runThreshold");
                if (previousVersion < 5 && runThreshold != null && defaultRunThreshold != null &&
                    runThreshold.floatValue >= 2f)
                    runThreshold.floatValue = defaultRunThreshold.floatValue;

                // The previous 0.1 threshold made the Run child dominate at normal walk speed.
                // Update only the legacy default and preserve intentional custom thresholds.
                var walkThreshold = current.FindProperty("walkThreshold");
                var defaultWalkThreshold = template.FindProperty("walkThreshold");
                if (previousVersion < 6 && walkThreshold != null && defaultWalkThreshold != null &&
                    Mathf.Approximately(walkThreshold.floatValue, 0.1f))
                    walkThreshold.floatValue = defaultWalkThreshold.floatValue;

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
            CopyStringIfEmpty(current, template, "bodyRendererToken");
            if (version != null && defaultVersion != null)
            {
                var previousVersion = version.intValue;
                if (previousVersion < 3)
                {
                    CopyBool(current, template, "importMaterials");
                    CopyBool(current, template, "importCameras");
                    CopyBool(current, template, "importLights");
                }
                if (previousVersion < 4)
                {
                    current.FindProperty("firstPersonBodyRevealPitch").floatValue =
                        template.FindProperty("firstPersonBodyRevealPitch").floatValue;
                    current.FindProperty("firstPersonBodyHidePitch").floatValue =
                        template.FindProperty("firstPersonBodyHidePitch").floatValue;
                }
                if (previousVersion < 5)
                    CopyBool(current, template, "requireFirstPersonArms");
                if (previousVersion < 6)
                {
                    // This field was present in the defaults JSON but missing from the old ScriptableObject.
                    CopyBool(current, template, "requireThirdPersonArms");
                    CopyInt(current, template, "defaultCharacter");
                }
                if (previousVersion < 7)
                    CopyFloat(current, template, "modelYawOffset");
                if (previousVersion < 8)
                    CopyBool(current, template, "enforceHumanoidTPose");
                if (previousVersion < defaultVersion.intValue)
                    version.intValue = defaultVersion.intValue;
            }

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

        private static void CopyFloat(SerializedObject target, SerializedObject source, string name)
        {
            target.FindProperty(name).floatValue = source.FindProperty(name).floatValue;
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
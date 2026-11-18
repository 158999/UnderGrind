using UnityEditor;
using UnityEngine;
using UnderGrind.Inventory;
using UnderGrind.Locomotion;

namespace UnderGrind.Editor
{
    [InitializeOnLoad]
    public static class PlayerGameplayAssetBuilder
    {
        private const string InteractionSettingsPath = "Assets/UnderGrind/Settings/PlayerInteractionSettings.asset";
        private const string InventoryUISettingsPath = "Assets/UnderGrind/Settings/PlayerInventoryUISettings.asset";
        private const string AnimationSettingsPath = "Assets/UnderGrind/Settings/PlayerAnimationSettings.asset";
        private const string RigSettingsPath = "Assets/UnderGrind/Settings/PlayerRigSettings.asset";

        static PlayerGameplayAssetBuilder()
        {
            EditorApplication.delayCall += EnsureAssets;
        }

        [MenuItem("UnderGrind/Player/Create or Repair Player Assets")]
        public static void EnsureAssets()
        {
            EnsureFolder("Assets/UnderGrind");
            EnsureFolder("Assets/UnderGrind/Settings");
            EnsureInteractionSettings();
            EnsureInventoryUISettings();
            EnsureAnimationSettings();
            EnsureRigSettings();
            AssetDatabase.SaveAssets();
        }

        private static void EnsureRigSettings()
        {
            var asset = AssetDatabase.LoadAssetAtPath<PlayerRigSettings>(RigSettingsPath);
            if (asset != null) return;
            asset = ScriptableObject.CreateInstance<PlayerRigSettings>();
            AssetDatabase.CreateAsset(asset, RigSettingsPath);

            var serialized = new SerializedObject(asset);
            SetString(serialized, "maleArmatureRootName", "UG_Male_Armature");
            SetString(serialized, "femaleArmatureRootName", "UG_Female_Armature");
            SetString(serialized, "malePrefabName", "Human_Male_Rigged");
            SetString(serialized, "femalePrefabName", "Human_Female_Rigged");
            SetString(serialized, "headRendererSuffix", "_Head");
            SetString(serialized, "firstPersonArmsToken", "_FP_Arms");
            SetString(serialized, "firstPersonHandsToken", "_FP_Hands");
            SetBool(serialized, "requireSeparateHead", true);
            SetBool(serialized, "requireFirstPersonArms", false);
            SetBool(serialized, "importAnimation", false);
            SetBool(serialized, "optimizeGameObjects", false);
            SetBool(serialized, "importVisibility", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void EnsureAnimationSettings()
        {
            var asset = AssetDatabase.LoadAssetAtPath<PlayerAnimationSettings>(AnimationSettingsPath);
            if (asset != null) return;
            asset = ScriptableObject.CreateInstance<PlayerAnimationSettings>();
            AssetDatabase.CreateAsset(asset, AnimationSettingsPath);

            var serialized = new SerializedObject(asset);
            SetString(serialized, "speedParameterName", "Speed");
            SetString(serialized, "moveXParameterName", "MoveX");
            SetString(serialized, "moveYParameterName", "MoveY");
            SetString(serialized, "groundedParameterName", "Grounded");
            SetString(serialized, "verticalSpeedParameterName", "VerticalSpeed");
            SetString(serialized, "runningParameterName", "Running");
            SetString(serialized, "crouchingParameterName", "Crouching");
            SetString(serialized, "jumpingParameterName", "Jumping");
            SetString(serialized, "fallingParameterName", "Falling");
            SetString(serialized, "landedParameterName", "Landed");
            SetFloat(serialized, "idleThreshold", 0f);
            SetFloat(serialized, "walkThreshold", 0.1f);
            SetFloat(serialized, "runThreshold", 2f);
            SetFloat(serialized, "crouchWalkThreshold", 0.1f);
            SetFloat(serialized, "speedDampTime", 0.1f);
            SetFloat(serialized, "transitionDuration", 0.15f);
            SetFloat(serialized, "landingExitTime", 0.9f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void EnsureInteractionSettings()
        {
            var asset = AssetDatabase.LoadAssetAtPath<PlayerInteractionSettings>(InteractionSettingsPath);
            if (asset != null) return;
            asset = ScriptableObject.CreateInstance<PlayerInteractionSettings>();
            AssetDatabase.CreateAsset(asset, InteractionSettingsPath);

            var serialized = new SerializedObject(asset);
            SetFloat(serialized, "interactionDistance", 2.5f);
            SetFloat(serialized, "pickupRayRadius", 0.08f);
            SetLayerMask(serialized, "interactionLayers", -1);
            SetFloat(serialized, "dropForwardDistance", 0.9f);
            SetFloat(serialized, "dropHeightOffset", 0.25f);
            SetFloat(serialized, "dropImpulse", 1.5f);
            SetInt(serialized, "dropQuantityPerAction", 1);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void EnsureInventoryUISettings()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InventoryUISettings>(InventoryUISettingsPath);
            if (asset != null) return;
            asset = ScriptableObject.CreateInstance<InventoryUISettings>();
            AssetDatabase.CreateAsset(asset, InventoryUISettingsPath);

            var serialized = new SerializedObject(asset);
            SetString(serialized, "panelTitle", "INVENTARIO");
            SetString(serialized, "weightTextFormat", "Peso: {0:0.0} / {1:0.0} kg");
            SetString(serialized, "emptySlotText", "Vuoto");
            SetInt(serialized, "titleFontSize", 24);
            SetInt(serialized, "bodyFontSize", 18);
            SetInt(serialized, "slotFontSize", 14);
            SetVector2(serialized, "referenceResolution", new Vector2(1920f, 1080f));
            SetVector2(serialized, "panelSize", new Vector2(720f, 520f));
            SetVector2(serialized, "panelOffset", Vector2.zero);
            SetInt(serialized, "columns", 6);
            SetVector2(serialized, "slotSize", new Vector2(96f, 96f));
            SetVector2(serialized, "slotSpacing", new Vector2(8f, 8f));
            SetInt(serialized, "panelPadding", 20);
            SetFloat(serialized, "headerHeight", 48f);
            SetInt(serialized, "maxVisibleSlots", 24);
            SetInt(serialized, "sortingOrder", 10);
            SetColor(serialized, "panelColor", new Color(0.06f, 0.07f, 0.08f, 0.94f));
            SetColor(serialized, "slotColor", new Color(0.16f, 0.18f, 0.20f, 1f));
            SetColor(serialized, "selectedSlotColor", new Color(0.78f, 0.62f, 0.30f, 1f));
            SetColor(serialized, "textColor", Color.white);
            SetColor(serialized, "overloadedWeightColor", new Color(0.95f, 0.28f, 0.24f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void SetString(SerializedObject obj, string name, string value)
        {
            var property = obj.FindProperty(name);
            if (property != null) property.stringValue = value;
        }

        private static void SetInt(SerializedObject obj, string name, int value)
        {
            var property = obj.FindProperty(name);
            if (property != null) property.intValue = value;
        }

        private static void SetFloat(SerializedObject obj, string name, float value)
        {
            var property = obj.FindProperty(name);
            if (property != null) property.floatValue = value;
        }

        private static void SetLayerMask(SerializedObject obj, string name, int value)
        {
            var property = obj.FindProperty(name);
            if (property != null) property.intValue = value;
        }

        private static void SetVector2(SerializedObject obj, string name, Vector2 value)
        {
            var property = obj.FindProperty(name);
            if (property != null) property.vector2Value = value;
        }

        private static void SetBool(SerializedObject obj, string name, bool value)
        {
            var property = obj.FindProperty(name);
            if (property != null) property.boolValue = value;
        }

        private static void SetColor(SerializedObject obj, string name, Color value)
        {
            var property = obj.FindProperty(name);
            if (property != null) property.colorValue = value;
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
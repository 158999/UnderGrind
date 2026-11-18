using UnityEditor;
using UnityEngine;
using UnderGrind.Inventory;

namespace UnderGrind.Editor
{
    [InitializeOnLoad]
    public static class PlayerGameplayAssetBuilder
    {
        private const string InteractionSettingsPath = "Assets/UnderGrind/Settings/PlayerInteractionSettings.asset";
        private const string InventoryUISettingsPath = "Assets/UnderGrind/Settings/PlayerInventoryUISettings.asset";

        static PlayerGameplayAssetBuilder()
        {
            EditorApplication.delayCall += EnsureAssets;
        }

        [MenuItem("UnderGrind/Player/Create or Repair Inventory Assets")]
        public static void EnsureAssets()
        {
            EnsureFolder("Assets/UnderGrind");
            EnsureFolder("Assets/UnderGrind/Settings");
            EnsureInteractionSettings();
            EnsureInventoryUISettings();
            AssetDatabase.SaveAssets();
        }

        private static void EnsureInteractionSettings()
        {
            var asset = AssetDatabase.LoadAssetAtPath<PlayerInteractionSettings>(InteractionSettingsPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<PlayerInteractionSettings>();
                AssetDatabase.CreateAsset(asset, InteractionSettingsPath);
            }

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
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<InventoryUISettings>();
                AssetDatabase.CreateAsset(asset, InventoryUISettingsPath);
            }

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
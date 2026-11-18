using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnderGrind.Locomotion;

namespace UnderGrind.Editor
{
    [InitializeOnLoad]
    public static class PlayerInputAssetBuilder
    {
        private const string SettingsFolder = "Assets/UnderGrind/Settings";
        private const string InputAssetPath = SettingsFolder + "/PlayerInputActions.asset";
        private const string SettingsAssetPath = SettingsFolder + "/PlayerInputSettings.asset";
        private const string DefaultsPath = SettingsFolder + "/Defaults/PlayerInputSettings.json";

        [Serializable]
        private sealed class InputSettingsDefaults
        {
            public int settingsVersion;
            public string actionMapName;
            public string moveActionName;
            public string lookMouseActionName;
            public string lookGamepadActionName;
            public string jumpActionName;
            public string runActionName;
            public string crouchActionName;
            public string toggleCameraActionName;
            public string cancelActionName;
            public string captureCursorActionName;
            public string interactActionName;
            public string dropActionName;
            public string toggleInventoryActionName;
            public string nextSlotActionName;
            public string previousSlotActionName;
            public bool lockCursorOnStart;
            public bool recaptureCursorOnClick;
            public float movementDeadzone;
            public float gamepadLookDeadzone;
            public float rebindTimeoutSeconds;
            public string overrideStorageKey;
        }

        static PlayerInputAssetBuilder()
        {
            EditorApplication.delayCall += EnsureAssets;
        }

        [MenuItem("UnderGrind/Player/Create or Repair Input Actions")]
        public static void EnsureAssets()
        {
            EnsureFolder("Assets/UnderGrind");
            EnsureFolder(SettingsFolder);

            var defaults = File.Exists(DefaultsPath)
                ? JsonUtility.FromJson<InputSettingsDefaults>(File.ReadAllText(DefaultsPath))
                : null;
            if (defaults == null)
            {
                Debug.LogError("UnderGrind: configurazione input non trovata: " + DefaultsPath);
                return;
            }

            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            if (actions == null)
            {
                actions = CreateDefaultActions();
                AssetDatabase.CreateAsset(actions, InputAssetPath);
            }

            var settings = AssetDatabase.LoadAssetAtPath<PlayerInputSettings>(SettingsAssetPath);
            var created = settings == null;
            if (created)
            {
                settings = ScriptableObject.CreateInstance<PlayerInputSettings>();
                JsonUtility.FromJsonOverwrite(File.ReadAllText(DefaultsPath), settings);
                AssetDatabase.CreateAsset(settings, SettingsAssetPath);
            }

            var serialized = new SerializedObject(settings);
            if (serialized.FindProperty("actionAsset").objectReferenceValue == null)
                SetObject(serialized, "actionAsset", actions);

            SetStringIfEmpty(serialized, "actionMapName", defaults.actionMapName);
            SetStringIfEmpty(serialized, "moveActionName", defaults.moveActionName);
            SetStringIfEmpty(serialized, "lookMouseActionName", defaults.lookMouseActionName);
            SetStringIfEmpty(serialized, "lookGamepadActionName", defaults.lookGamepadActionName);
            SetStringIfEmpty(serialized, "jumpActionName", defaults.jumpActionName);
            SetStringIfEmpty(serialized, "runActionName", defaults.runActionName);
            SetStringIfEmpty(serialized, "crouchActionName", defaults.crouchActionName);
            SetStringIfEmpty(serialized, "toggleCameraActionName", defaults.toggleCameraActionName);
            SetStringIfEmpty(serialized, "cancelActionName", defaults.cancelActionName);
            SetStringIfEmpty(serialized, "captureCursorActionName", defaults.captureCursorActionName);
            SetStringIfEmpty(serialized, "interactActionName", defaults.interactActionName);
            SetStringIfEmpty(serialized, "dropActionName", defaults.dropActionName);
            SetStringIfEmpty(serialized, "toggleInventoryActionName", defaults.toggleInventoryActionName);
            SetStringIfEmpty(serialized, "nextSlotActionName", defaults.nextSlotActionName);
            SetStringIfEmpty(serialized, "previousSlotActionName", defaults.previousSlotActionName);
            SetStringIfEmpty(serialized, "overrideStorageKey", defaults.overrideStorageKey);

            var version = serialized.FindProperty("settingsVersion");
            if (version != null && version.intValue < defaults.settingsVersion)
            {
                SetFloatIfNotPositive(serialized, "movementDeadzone", defaults.movementDeadzone);
                SetFloatIfNotPositive(serialized, "gamepadLookDeadzone", defaults.gamepadLookDeadzone);
                SetFloatIfNotPositive(serialized, "rebindTimeoutSeconds", defaults.rebindTimeoutSeconds);
                version.intValue = defaults.settingsVersion;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        private static InputActionAsset CreateDefaultActions()
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "PlayerInputActions";
            var map = new InputActionMap("Player");
            asset.AddActionMap(map);

            var move = map.AddAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");

            map.AddAction("LookMouse", InputActionType.Value, expectedControlType: "Vector2")
                .AddBinding("<Mouse>/delta");
            map.AddAction("LookGamepad", InputActionType.Value, expectedControlType: "Vector2")
                .AddBinding("<Gamepad>/rightStick");
            AddButton(map, "Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            AddButton(map, "Run", "<Keyboard>/leftShift", "<Gamepad>/leftStickPress");
            AddButton(map, "Crouch", "<Keyboard>/c", "<Gamepad>/buttonEast");
            AddButton(map, "ToggleCamera", "<Keyboard>/f5", "<Gamepad>/buttonNorth");
            AddButton(map, "Cancel", "<Keyboard>/escape", "<Gamepad>/start");
            AddButton(map, "CaptureCursor", "<Mouse>/leftButton");
            AddButton(map, "Interact", "<Keyboard>/e", "<Gamepad>/buttonWest");
            AddButton(map, "Drop", "<Keyboard>/g", "<Gamepad>/dpad/down");
            AddButton(map, "ToggleInventory", "<Keyboard>/tab", "<Gamepad>/select");
            AddButton(map, "NextSlot", "<Keyboard>/rightBracket", "<Gamepad>/dpad/right");
            AddButton(map, "PreviousSlot", "<Keyboard>/leftBracket", "<Gamepad>/dpad/left");
            return asset;
        }

        private static void AddButton(InputActionMap map, string name, params string[] bindings)
        {
            var action = map.AddAction(name, InputActionType.Button, expectedControlType: "Button");
            foreach (var binding in bindings) action.AddBinding(binding);
        }

        private static void SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void SetStringIfEmpty(SerializedObject serialized, string propertyName, string value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null && string.IsNullOrWhiteSpace(property.stringValue))
                property.stringValue = value;
        }

        private static void SetFloatIfNotPositive(SerializedObject serialized, string propertyName, float value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null && property.floatValue <= 0f) property.floatValue = value;
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
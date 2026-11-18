using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnderGrind.Locomotion;

namespace UnderGrind.Editor
{
    [InitializeOnLoad]
    public static class PlayerInputAssetBuilder
    {
        private const string InputAssetPath = "Assets/UnderGrind/Settings/PlayerInputActions.asset";
        private const string SettingsAssetPath = "Assets/UnderGrind/Settings/PlayerInputSettings.asset";

        static PlayerInputAssetBuilder()
        {
            EditorApplication.delayCall += EnsureAssets;
        }

        [MenuItem("UnderGrind/Player/Create or Repair Input Actions")]
        public static void EnsureAssets()
        {
            EnsureFolder("Assets/UnderGrind");
            EnsureFolder("Assets/UnderGrind/Settings");

            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            if (actions == null)
            {
                actions = CreateDefaultActions();
                AssetDatabase.CreateAsset(actions, InputAssetPath);
            }

            var settings = AssetDatabase.LoadAssetAtPath<PlayerInputSettings>(SettingsAssetPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PlayerInputSettings>();
                AssetDatabase.CreateAsset(settings, SettingsAssetPath);
            }

            var serialized = new SerializedObject(settings);
            SetObject(serialized, "actionAsset", actions);
            SetString(serialized, "actionMapName", "Player");
            SetString(serialized, "moveActionName", "Move");
            SetString(serialized, "lookMouseActionName", "LookMouse");
            SetString(serialized, "lookGamepadActionName", "LookGamepad");
            SetString(serialized, "jumpActionName", "Jump");
            SetString(serialized, "runActionName", "Run");
            SetString(serialized, "crouchActionName", "Crouch");
            SetString(serialized, "toggleCameraActionName", "ToggleCamera");
            SetString(serialized, "cancelActionName", "Cancel");
            SetString(serialized, "captureCursorActionName", "CaptureCursor");
            SetString(serialized, "interactActionName", "Interact");
            SetString(serialized, "dropActionName", "Drop");
            SetString(serialized, "toggleInventoryActionName", "ToggleInventory");
            SetString(serialized, "nextSlotActionName", "NextSlot");
            SetString(serialized, "previousSlotActionName", "PreviousSlot");
            SetBool(serialized, "lockCursorOnStart", true);
            SetBool(serialized, "recaptureCursorOnClick", true);
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

        private static void SetObject(SerializedObject serialized, string propertyName, Object value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void SetString(SerializedObject serialized, string propertyName, string value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null) property.stringValue = value;
        }

        private static void SetBool(SerializedObject serialized, string propertyName, bool value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null) property.boolValue = value;
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
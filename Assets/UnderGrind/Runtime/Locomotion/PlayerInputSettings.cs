using UnityEngine;
using UnityEngine.InputSystem;

namespace UnderGrind.Locomotion
{
    public enum PlayerInputActionId
    {
        Move,
        LookMouse,
        LookGamepad,
        Jump,
        Run,
        Crouch,
        ToggleCamera,
        Cancel,
        CaptureCursor,
        Interact,
        Drop,
        ToggleInventory,
        NextSlot,
        PreviousSlot
    }

    [CreateAssetMenu(menuName = "UnderGrind/Player/Input Settings", fileName = "PlayerInputSettings")]
    public sealed class PlayerInputSettings : ScriptableObject
    {
        [Header("Asset e action map")]
        [SerializeField, HideInInspector] private int settingsVersion;
        [SerializeField] private InputActionAsset actionAsset;
        [SerializeField] private string actionMapName;

        [Header("Azioni")]
        [SerializeField] private string moveActionName;
        [SerializeField] private string lookMouseActionName;
        [SerializeField] private string lookGamepadActionName;
        [SerializeField] private string jumpActionName;
        [SerializeField] private string runActionName;
        [SerializeField] private string crouchActionName;
        [SerializeField] private string toggleCameraActionName;
        [SerializeField] private string cancelActionName;
        [SerializeField] private string captureCursorActionName;
        [SerializeField] private string interactActionName;
        [SerializeField] private string dropActionName;
        [SerializeField] private string toggleInventoryActionName;
        [SerializeField] private string nextSlotActionName;
        [SerializeField] private string previousSlotActionName;

        [Header("Dead zone")]
        [SerializeField, Range(0f, 0.95f)] private float movementDeadzone;
        [SerializeField, Range(0f, 0.95f)] private float gamepadLookDeadzone;

        [Header("Rimappatura e salvataggio")]
        [SerializeField, Min(0.1f)] private float rebindTimeoutSeconds;
        [SerializeField] private string overrideStorageKey;

        [Header("Comportamento cursore")]
        [SerializeField] private bool lockCursorOnStart;
        [SerializeField] private bool recaptureCursorOnClick;

        public InputActionAsset ActionAsset => actionAsset;
        public string ActionMapName => actionMapName;
        public float MovementDeadzone => movementDeadzone;
        public float GamepadLookDeadzone => gamepadLookDeadzone;
        public float RebindTimeoutSeconds => rebindTimeoutSeconds;
        public string OverrideStorageKey => overrideStorageKey;
        public bool LockCursorOnStart => lockCursorOnStart;
        public bool RecaptureCursorOnClick => recaptureCursorOnClick;

        public InputActionMap FindActionMap()
        {
            return actionAsset != null && !string.IsNullOrWhiteSpace(actionMapName)
                ? actionAsset.FindActionMap(actionMapName, false) : null;
        }

        public InputAction GetAction(PlayerInputActionId actionId)
        {
            var map = FindActionMap();
            if (map == null) return null;
            var actionName = actionId switch
            {
                PlayerInputActionId.Move => moveActionName,
                PlayerInputActionId.LookMouse => lookMouseActionName,
                PlayerInputActionId.LookGamepad => lookGamepadActionName,
                PlayerInputActionId.Jump => jumpActionName,
                PlayerInputActionId.Run => runActionName,
                PlayerInputActionId.Crouch => crouchActionName,
                PlayerInputActionId.ToggleCamera => toggleCameraActionName,
                PlayerInputActionId.Cancel => cancelActionName,
                PlayerInputActionId.CaptureCursor => captureCursorActionName,
                PlayerInputActionId.Interact => interactActionName,
                PlayerInputActionId.Drop => dropActionName,
                PlayerInputActionId.ToggleInventory => toggleInventoryActionName,
                PlayerInputActionId.NextSlot => nextSlotActionName,
                PlayerInputActionId.PreviousSlot => previousSlotActionName,
                _ => null
            };
            return string.IsNullOrWhiteSpace(actionName) ? null : map.FindAction(actionName, false);
        }

        public bool EnableActionMap()
        {
            var map = FindActionMap();
            if (map == null) return false;
            map.Enable();
            return true;
        }

        public void DisableActionMap()
        {
            FindActionMap()?.Disable();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            movementDeadzone = Mathf.Clamp(movementDeadzone, 0f, 0.95f);
            gamepadLookDeadzone = Mathf.Clamp(gamepadLookDeadzone, 0f, 0.95f);
            rebindTimeoutSeconds = Mathf.Max(0.1f, rebindTimeoutSeconds);
            if (actionAsset != null && !string.IsNullOrWhiteSpace(actionMapName) &&
                actionAsset.FindActionMap(actionMapName, false) == null)
                Debug.LogError("UnderGrind: action map non trovato: " + actionMapName, this);
        }
#endif
    }
}
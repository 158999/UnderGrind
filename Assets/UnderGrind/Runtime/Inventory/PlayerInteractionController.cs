using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnderGrind.Locomotion;

namespace UnderGrind.Inventory
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInventory))]
    public sealed class PlayerInteractionController : MonoBehaviour
    {
        [SerializeField] private PlayerInputSettings inputSettings;
        [SerializeField] private PlayerInteractionSettings settings;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private HumanLocomotionController locomotion;
        [SerializeField] private PlayerInventoryUI inventoryUI;
        [SerializeField] private Camera playerCamera;

        private InputAction _interactAction;
        private InputAction _dropAction;
        private bool _missingConfigurationReported;

        public bool IsConfigured => inputSettings != null && settings != null && inventory != null;

        public void Configure(PlayerInputSettings input, PlayerInteractionSettings interaction,
            PlayerInventory playerInventory, HumanLocomotionController playerLocomotion,
            PlayerInventoryUI ui, Camera camera)
        {
            inputSettings = input;
            settings = interaction;
            inventory = playerInventory;
            locomotion = playerLocomotion;
            inventoryUI = ui;
            playerCamera = camera;
            ResolveActions();
        }

        private void Awake()
        {
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (locomotion == null) locomotion = GetComponent<HumanLocomotionController>();
            if (inventoryUI == null) inventoryUI = GetComponent<PlayerInventoryUI>();
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>(true);
            ResolveActions();
        }

        private void Update()
        {
            if (!IsConfigured)
            {
                if (!_missingConfigurationReported)
                {
                    Debug.LogError("UnderGrind: assegna PlayerInputSettings e PlayerInteractionSettings al PlayerInteractionController.", this);
                    _missingConfigurationReported = true;
                }
                return;
            }

            if (_interactAction != null && _interactAction.WasPressedThisFrame() &&
                (inventoryUI == null || !inventoryUI.IsOpen))
                TryInteract();

            if (_dropAction != null && _dropAction.WasPressedThisFrame())
                DropSelectedStack();
        }

        public bool TryInteract()
        {
            if (settings == null || inventory == null || playerCamera == null) return false;
            var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            var hits = Physics.SphereCastAll(ray, settings.PickupRayRadius, settings.InteractionDistance,
                settings.InteractionLayers, QueryTriggerInteraction.Collide);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform)) continue;
                var pickup = hit.collider.GetComponentInParent<WorldItemPickup>();
                if (pickup != null) return pickup.TryPickup(inventory);
            }
            return false;
        }

        public bool DropSelectedStack()
        {
            if (settings == null || inventory == null) return false;
            var slotIndex = inventory.SelectedSlotIndex;
            var slot = inventory.SelectedSlot;
            if (slot == null || slot.IsEmpty) return false;
            if (slot.Item.WorldPrefab == null)
            {
                Debug.LogWarning("UnderGrind: l'oggetto " + slot.Item.ItemId +
                    " non ha un World Prefab assegnato e non può essere rilasciato.", slot.Item);
                return false;
            }
            if (slot.Item.WorldPrefab.GetComponentInChildren<Collider>(true) == null)
            {
                Debug.LogWarning("UnderGrind: il World Prefab di " + slot.Item.ItemId +
                    " non contiene un Collider; il pickup non sarebbe raccoglibile.", slot.Item);
                return false;
            }

            var quantity = Mathf.Min(settings.DropQuantityPerAction, slot.Quantity);
            var origin = playerCamera != null ? playerCamera.transform : transform;
            var direction = Vector3.ProjectOnPlane(origin.forward, Vector3.up).normalized;
            var position = origin.position + direction * settings.DropForwardDistance +
                           Vector3.up * settings.DropHeightOffset;
            var droppedObject = Instantiate(slot.Item.WorldPrefab, position, Quaternion.identity);
            var pickup = droppedObject.GetComponent<WorldItemPickup>();
            if (pickup == null) pickup = droppedObject.AddComponent<WorldItemPickup>();
            if (!pickup.Configure(slot.Item, quantity))
            {
                Destroy(droppedObject);
                return false;
            }

            var result = inventory.RemoveFromSlot(slotIndex, quantity);
            if (!result.Completed)
            {
                if (result.Processed > 0) inventory.AddItem(slot.Item, result.Processed);
                Destroy(droppedObject);
                return false;
            }

            var body = droppedObject.GetComponent<Rigidbody>();
            if (body != null && settings.DropImpulse > 0f)
                body.AddForce(direction * settings.DropImpulse, ForceMode.Impulse);
            return true;
        }

        private void ResolveActions()
        {
            _interactAction = inputSettings != null
                ? inputSettings.GetAction(PlayerInputActionId.Interact) : null;
            _dropAction = inputSettings != null
                ? inputSettings.GetAction(PlayerInputActionId.Drop) : null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (locomotion == null) locomotion = GetComponent<HumanLocomotionController>();
            if (inventoryUI == null) inventoryUI = GetComponent<PlayerInventoryUI>();
        }
#endif
    }
}
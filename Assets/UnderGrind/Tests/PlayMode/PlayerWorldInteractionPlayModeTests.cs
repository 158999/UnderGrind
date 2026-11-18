using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnderGrind.Inventory;
using UnderGrind.Locomotion;

namespace UnderGrind.Tests
{
    public sealed class PlayerWorldInteractionPlayModeTests
    {
        private GameObject _player;
        private GameObject _worldItem;
        private GameObject _worldPrefab;
        private InventorySettings _inventorySettings;
        private PlayerInteractionSettings _interactionSettings;
        private ItemDefinition _item;
        private InventoryUISettings _uiSettings;
        private InputActionAsset _inputAsset;
        private PlayerInputSettings _inputSettings;

        [UnityTest]
        public IEnumerator Interact_PicksUpWorldItemIntoInventory()
        {
            CreateSettings();
            _player = new GameObject("Interaction player");
            var inventory = _player.AddComponent<PlayerInventory>();
            inventory.Configure(_inventorySettings);
            var cameraObject = new GameObject("Player camera");
            cameraObject.transform.SetParent(_player.transform, false);
            var camera = cameraObject.AddComponent<Camera>();

            _worldItem = new GameObject("World item");
            _worldItem.transform.position = new Vector3(0f, 0f, 1f);
            _worldItem.AddComponent<BoxCollider>();
            var pickup = _worldItem.AddComponent<WorldItemPickup>();
            pickup.Configure(_item, 1);

            var interaction = _player.AddComponent<PlayerInteractionController>();
            interaction.Configure(_inputSettings, _interactionSettings, inventory, null, null, camera);
            Physics.SyncTransforms();
            yield return null;

            Assert.IsTrue(interaction.TryInteract());
            Assert.AreSame(_item, inventory.GetSlot(0).Item);
            Assert.AreEqual(1, inventory.GetSlot(0).Quantity);
        }

        [UnityTest]
        public IEnumerator DropSelectedStack_SpawnsConfiguredWorldPrefabAndRemovesOneItem()
        {
            CreateSettings();
            _worldPrefab = new GameObject("Drop prefab");
            _worldPrefab.AddComponent<BoxCollider>();
            _worldPrefab.AddComponent<Rigidbody>();
            _worldPrefab.AddComponent<WorldItemPickup>();
            SetField(_item, "worldPrefab", _worldPrefab);

            _player = new GameObject("Drop player");
            var inventory = _player.AddComponent<PlayerInventory>();
            inventory.Configure(_inventorySettings);
            inventory.AddItem(_item, 2);

            var interaction = _player.AddComponent<PlayerInteractionController>();
            interaction.Configure(_inputSettings, _interactionSettings, inventory, null, null, null);

            Assert.IsTrue(interaction.DropSelectedStack());
            Assert.AreEqual(1, inventory.GetSlot(0).Quantity);

            yield return null;
            var spawned = Object.FindObjectsOfType<WorldItemPickup>();
            var foundDrop = false;
            foreach (var candidate in spawned)
            {
                if (candidate == null || candidate.gameObject == _worldPrefab) continue;
                if (candidate.Item == _item && candidate.Quantity == 1) foundDrop = true;
            }
            Assert.IsTrue(foundDrop);
        }

        [UnityTest]
        public IEnumerator InventoryUI_RefreshesWhenConfiguredAfterAwake()
        {
            CreateSettings();
            _uiSettings = ScriptableObject.CreateInstance<InventoryUISettings>();
            SetField(_uiSettings, "panelTitle", "INVENTORY");
            SetField(_uiSettings, "weightTextFormat", "Weight {0:0.0}/{1:0.0}");
            SetField(_uiSettings, "emptySlotText", "Empty");
            SetField(_uiSettings, "titleFontSize", 24);
            SetField(_uiSettings, "bodyFontSize", 18);
            SetField(_uiSettings, "slotFontSize", 14);
            SetField(_uiSettings, "referenceResolution", new Vector2(1920f, 1080f));
            SetField(_uiSettings, "panelSize", new Vector2(720f, 520f));
            SetField(_uiSettings, "panelOffset", Vector2.zero);
            SetField(_uiSettings, "columns", 6);
            SetField(_uiSettings, "slotSize", new Vector2(96f, 96f));
            SetField(_uiSettings, "slotSpacing", new Vector2(8f, 8f));
            SetField(_uiSettings, "panelPadding", 20);
            SetField(_uiSettings, "headerHeight", 48f);
            SetField(_uiSettings, "maxVisibleSlots", 4);
            SetField(_uiSettings, "sortingOrder", 10);
            SetField(_uiSettings, "panelColor", Color.black);
            SetField(_uiSettings, "slotColor", Color.gray);
            SetField(_uiSettings, "selectedSlotColor", Color.yellow);
            SetField(_uiSettings, "textColor", Color.white);
            SetField(_uiSettings, "overloadedWeightColor", Color.red);

            _player = new GameObject("Inventory UI player");
            var inventory = _player.AddComponent<PlayerInventory>();
            inventory.Configure(_inventorySettings);
            var ui = _player.AddComponent<PlayerInventoryUI>();
            ui.Configure(_uiSettings, _inputSettings, inventory, null);
            inventory.AddItem(_item, 1);
            yield return null;

            var labels = ui.GetComponentsInChildren<Text>(true);
            Assert.IsTrue(System.Array.Exists(labels,
                label => label.text.Contains("World test item")),
                "The UI must refresh its slot text after Configure subscribes to inventory changes.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_player != null) Object.Destroy(_player);
            if (_worldItem != null) Object.Destroy(_worldItem);
            if (_worldPrefab != null) Object.Destroy(_worldPrefab);
            if (_inventorySettings != null) Object.Destroy(_inventorySettings);
            if (_interactionSettings != null) Object.Destroy(_interactionSettings);
            if (_item != null) Object.Destroy(_item);
            if (_uiSettings != null) Object.Destroy(_uiSettings);
            if (_inputSettings != null) Object.Destroy(_inputSettings);
            if (_inputAsset != null) Object.Destroy(_inputAsset);
            yield return null;
        }

        private void CreateSettings()
        {
            _inventorySettings = ScriptableObject.CreateInstance<InventorySettings>();
            SetField(_inventorySettings, "maxSlots", 4);
            SetField(_inventorySettings, "maxCarryWeightKg", 50f);
            SetField(_inventorySettings, "overloadedWalkSpeedReductionPercent", 25f);

            _interactionSettings = ScriptableObject.CreateInstance<PlayerInteractionSettings>();
            SetField(_interactionSettings, "interactionDistance", 2f);
            SetField(_interactionSettings, "pickupRayRadius", 0.1f);
            SetField(_interactionSettings, "interactionLayers", (LayerMask)(-1));
            SetField(_interactionSettings, "dropForwardDistance", 0.9f);
            SetField(_interactionSettings, "dropHeightOffset", 0.25f);
            SetField(_interactionSettings, "dropImpulse", 1.5f);
            SetField(_interactionSettings, "dropQuantityPerAction", 1);

            _item = ScriptableObject.CreateInstance<ItemDefinition>();
            SetField(_item, "itemId", "test.world.item");
            SetField(_item, "displayName", "World test item");
            SetField(_item, "maxStackSize", 10);
            SetField(_item, "unitWeightKg", 1f);
            _inputAsset = ScriptableObject.CreateInstance<InputActionAsset>();
            var playerMap = new InputActionMap("Player");
            _inputAsset.AddActionMap(playerMap);
            playerMap.AddAction("Interact", InputActionType.Button);
            playerMap.AddAction("Drop", InputActionType.Button);
            _inputSettings = ScriptableObject.CreateInstance<PlayerInputSettings>();
            SetField(_inputSettings, "actionAsset", _inputAsset);
            SetField(_inputSettings, "actionMapName", "Player");
            SetField(_inputSettings, "interactActionName", "Interact");
            SetField(_inputSettings, "dropActionName", "Drop");
        }

        private static void SetField<T>(T target, string name, object value)
        {
            typeof(T).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
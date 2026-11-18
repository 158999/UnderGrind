using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnderGrind.Inventory;

namespace UnderGrind.Tests
{
    public sealed class PlayerInventoryTests
    {
        private GameObject _owner;
        private InventorySettings _settings;
        private ItemDefinition _item;
        private PlayerInventory _inventory;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<InventorySettings>();
            SetField(_settings, "maxSlots", 3);
            SetField(_settings, "maxCarryWeightKg", 10f);
            SetField(_settings, "overloadedWalkSpeedReductionPercent", 25f);

            _item = ScriptableObject.CreateInstance<ItemDefinition>();
            SetField(_item, "itemId", "test.item");
            SetField(_item, "displayName", "Test item");
            SetField(_item, "maxStackSize", 10);
            SetField(_item, "unitWeightKg", 4f);

            _owner = new GameObject("Inventory test");
            _inventory = _owner.AddComponent<PlayerInventory>();
            _inventory.Configure(_settings);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_owner);
            Object.DestroyImmediate(_settings);
            Object.DestroyImmediate(_item);
        }

        [Test]
        public void AddItem_AllowsWeightAboveCapacity()
        {
            var result = _inventory.AddItem(_item, 3);

            Assert.AreEqual(3, result.Processed);
            Assert.AreEqual(0, result.Remaining);
            Assert.AreEqual(12f, _inventory.CurrentWeightKg);
            Assert.IsTrue(_inventory.IsOverloaded);
        }

        [Test]
        public void AddItem_ReportsRemainderWhenSlotsAreFull()
        {
            SetField(_item, "maxStackSize", 1);
            var result = _inventory.AddItem(_item, 5);

            Assert.AreEqual(3, result.Processed);
            Assert.AreEqual(2, result.Remaining);
            Assert.AreEqual(3, _inventory.Slots.Count);
        }

        [Test]
        public void RemoveItem_UpdatesWeightAndOverloadState()
        {
            _inventory.AddItem(_item, 3);

            var result = _inventory.RemoveItem(_item, 1);

            Assert.AreEqual(1, result.Processed);
            Assert.AreEqual(8f, _inventory.CurrentWeightKg);
            Assert.IsFalse(_inventory.IsOverloaded);
        }

        [Test]
        public void MoveStack_DoesNotChangeTotalWeight()
        {
            _inventory.AddItem(_item, 2);
            var weightBefore = _inventory.CurrentWeightKg;

            Assert.IsTrue(_inventory.MoveStack(0, 1, 1));

            Assert.AreEqual(weightBefore, _inventory.CurrentWeightKg);
        }

        [Test]
        public void SplitAndMerge_PreserveTotalWeight()
        {
            _inventory.AddItem(_item, 4);
            var weightBefore = _inventory.CurrentWeightKg;

            Assert.IsTrue(_inventory.SplitStack(0, 1, 2));
            Assert.AreEqual(weightBefore, _inventory.CurrentWeightKg);
            Assert.IsTrue(_inventory.MoveStack(1, 0, 2));
            Assert.AreEqual(weightBefore, _inventory.CurrentWeightKg);
            Assert.IsTrue(_inventory.GetSlot(1).IsEmpty);
        }

        [Test]
        public void ExactWeightCapacity_IsNotOverloaded()
        {
            SetField(_item, "unitWeightKg", 5f);
            _inventory.AddItem(_item, 2);

            Assert.AreEqual(_inventory.MaxWeightKg, _inventory.CurrentWeightKg);
            Assert.IsFalse(_inventory.IsOverloaded);
        }

        [Test]
        public void ZeroWeightItems_AreAllowedAndDoNotIncreaseWeight()
        {
            SetField(_item, "unitWeightKg", 0f);
            var result = _inventory.AddItem(_item, 3);

            Assert.AreEqual(3, result.Processed);
            Assert.AreEqual(0f, _inventory.CurrentWeightKg);
            Assert.IsFalse(_inventory.IsOverloaded);
        }

        [Test]
        public void SelectSlot_ChangesActiveSlotAndRejectsOutOfRangeIndices()
        {
            Assert.AreEqual(0, _inventory.SelectedSlotIndex);
            Assert.IsTrue(_inventory.SelectSlot(2));
            Assert.AreEqual(2, _inventory.SelectedSlotIndex);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => _inventory.SelectSlot(3));
        }

        [Test]
        public void RemoveFromSlot_RemovesOnlyRequestedQuantityAndUpdatesWeight()
        {
            _inventory.AddItem(_item, 4);
            var result = _inventory.RemoveFromSlot(0, 2);

            Assert.AreEqual(2, result.Processed);
            Assert.AreEqual(2, _inventory.GetSlot(0).Quantity);
            Assert.AreEqual(8f, _inventory.CurrentWeightKg);
        }

        [Test]
        public void WorldPickup_PartiallyTransfersWhenInventoryHasOneFreeSlot()
        {
            SetField(_item, "maxStackSize", 1);
            _inventory.AddItem(_item, 2);
            var pickupObject = new GameObject("World pickup test");
            var pickup = pickupObject.AddComponent<WorldItemPickup>();
            Assert.IsTrue(pickup.Configure(_item, 2));

            Assert.IsTrue(pickup.TryPickup(_inventory));

            Assert.AreEqual(1, pickup.Quantity);
            Assert.AreEqual(3, _inventory.Slots.Count);
            Assert.AreEqual(3f, _inventory.Slots[0].Quantity +
                _inventory.Slots[1].Quantity + _inventory.Slots[2].Quantity);
            Object.DestroyImmediate(pickupObject);
        }

        [Test]
        public void CapacityReduction_DoesNotAcceptNewItemsIntoOccupiedOverflowSlots()
        {
            SetField(_item, "maxStackSize", 1);
            Assert.AreEqual(2, _inventory.AddItem(_item, 2).Processed);

            SetField(_settings, "maxSlots", 1);
            _inventory.Configure(_settings);
            var result = _inventory.AddItem(_item, 1);

            Assert.AreEqual(0, result.Processed);
            Assert.AreEqual(1, _inventory.ConfiguredSlotCapacity);
            Assert.AreEqual(2, _inventory.SlotCapacity);
            Assert.AreEqual(8f, _inventory.CurrentWeightKg);
        }

        private static void SetField<T>(T target, string name, object value)
        {
            typeof(T).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
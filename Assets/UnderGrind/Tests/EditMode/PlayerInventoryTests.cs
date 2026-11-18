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

        private static void SetField<T>(T target, string name, object value)
        {
            typeof(T).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
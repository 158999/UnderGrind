using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnderGrind.Inventory;
using UnderGrind.Locomotion;

namespace UnderGrind.Tests
{
    public sealed class PlayerOverloadPlayModeTests
    {
        private GameObject _player;
        private InventorySettings _settings;
        private ItemDefinition _item;

        [UnityTest]
        public IEnumerator Overload_DisablesRunAndJumpAndReducesWalkSpeedByConfiguredPercent()
        {
            _settings = ScriptableObject.CreateInstance<InventorySettings>();
            SetField(_settings, "maxSlots", 4);
            SetField(_settings, "maxCarryWeightKg", 10f);
            SetField(_settings, "overloadedWalkSpeedReductionPercent", 25f);

            _item = ScriptableObject.CreateInstance<ItemDefinition>();
            SetField(_item, "itemId", "test.heavy");
            SetField(_item, "displayName", "Heavy item");
            SetField(_item, "maxStackSize", 4);
            SetField(_item, "unitWeightKg", 4f);

            _player = new GameObject("Player overload test");
            _player.AddComponent<CharacterController>();
            var inventory = _player.AddComponent<PlayerInventory>();
            inventory.Configure(_settings);
            var locomotion = _player.AddComponent<HumanLocomotionController>();

            inventory.AddItem(_item, 3);
            yield return null;

            Assert.IsTrue(locomotion.IsOverloaded);
            Assert.IsFalse(locomotion.CanRun);
            Assert.IsFalse(locomotion.CanJump);
            Assert.AreEqual(locomotion.WalkSpeed * inventory.OverloadedWalkSpeedMultiplier, locomotion.EffectiveWalkSpeed, 0.001f);

            inventory.RemoveItem(_item, 1);
            yield return null;

            Assert.IsFalse(locomotion.IsOverloaded);
            Assert.IsTrue(locomotion.CanRun);
            Assert.IsTrue(locomotion.CanJump);
            Assert.AreEqual(locomotion.WalkSpeed, locomotion.EffectiveWalkSpeed, 0.001f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_player != null) Object.Destroy(_player);
            if (_settings != null) Object.Destroy(_settings);
            if (_item != null) Object.Destroy(_item);
            yield return null;
        }

        private static void SetField<T>(T target, string name, object value)
        {
            typeof(T).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
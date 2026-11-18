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
        private PlayerLocomotionSettings _locomotionSettings;

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
            _locomotionSettings = CreateLocomotionSettings();
            var locomotion = _player.AddComponent<HumanLocomotionController>();
            locomotion.ConfigureSettings(_locomotionSettings);

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
            if (_locomotionSettings != null) Object.Destroy(_locomotionSettings);
            yield return null;
        }

        private static PlayerLocomotionSettings CreateLocomotionSettings()
        {
            var settings = ScriptableObject.CreateInstance<PlayerLocomotionSettings>();
            SetField(settings, "characterHeight", 1.78f);
            SetField(settings, "walkSpeed", 1.8f);
            SetField(settings, "runMultiplier", 4f / 3f);
            SetField(settings, "jumpHeight", 0.4f);
            SetField(settings, "gravity", 9.81f);
            SetField(settings, "acceleration", 12f);
            SetField(settings, "deceleration", 16f);
            SetField(settings, "airControl", 0.65f);
            SetField(settings, "coyoteTime", 0.1f);
            SetField(settings, "jumpBufferTime", 0.12f);
            SetField(settings, "groundedStickVelocity", 2f);
            SetField(settings, "movementInputThreshold", 0.001f);
            SetField(settings, "directionThreshold", 0.001f);
            SetField(settings, "jumpApexStateThreshold", 0.05f);
            SetField(settings, "crouchHeight", 1.12f);
            SetField(settings, "crouchSpeedMultiplier", 0.5f);
            SetField(settings, "stanceChangeSpeed", 8f);
            SetField(settings, "capsuleRadius", 0.28f);
            SetField(settings, "standingClearance", 0.01f);
            SetField(settings, "stanceOverlapRadiusMultiplier", 0.95f);
            SetField(settings, "characterStepOffset", 0.32f);
            SetField(settings, "characterStepOffsetHeightRatio", 0.18f);
            SetField(settings, "characterSlopeLimit", 48f);
            SetField(settings, "characterMinMoveDistance", 0f);
            SetField(settings, "characterSkinWidth", 0.08f);
            SetField(settings, "characterDetectCollisions", true);
            SetField(settings, "characterEnableOverlapRecovery", true);
            SetField(settings, "mouseSensitivity", 0.12f);
            SetField(settings, "gamepadLookSensitivity", 150f);
            SetField(settings, "maxLookAngle", 82f);
            SetField(settings, "standingViewHeight", 1.62f);
            SetField(settings, "crouchingViewHeight", 0.98f);
            SetField(settings, "thirdPersonCameraOffset", new Vector3(0f, 0.12f, 0f));
            SetField(settings, "thirdPersonDistance", 3.25f);
            SetField(settings, "cameraCollisionRadius", 0.16f);
            SetField(settings, "cameraCollisionSurfaceOffset", 0.18f);
            SetField(settings, "cameraFollowSharpness", 14f);
            SetField(settings, "thirdPersonRotationSharpness", 12f);
            SetField(settings, "cameraNearClipPlane", 0.04f);
            SetField(settings, "cameraFieldOfView", 75f);
            return settings;
        }

        private static void SetField<T>(T target, string name, object value)
        {
            typeof(T).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
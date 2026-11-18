using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnderGrind.Locomotion;

namespace UnderGrind.Tests
{
    public sealed class HumanLocomotionTuningTests
    {
        private const string SettingsPath = "Assets/UnderGrind/Settings/PlayerLocomotionSettings.asset";
        private PlayerLocomotionSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = AssetDatabase.LoadAssetAtPath<PlayerLocomotionSettings>(SettingsPath);
            Assert.That(_settings, Is.Not.Null, "L'asset di configurazione locomozione deve esistere.");
        }

        [Test]
        public void RunIsExactlyOneThirdFasterThanWalk()
        {
            Assert.That(_settings.RunSpeed, Is.EqualTo(_settings.WalkSpeed * 4f / 3f).Within(0.0001f));
            Assert.That(_settings.RunSpeed / _settings.WalkSpeed, Is.EqualTo(4f / 3f).Within(0.0001f));
        }

        [Test]
        public void JumpVelocityProducesConfiguredApexHeight()
        {
            var height = _settings.JumpVelocity * _settings.JumpVelocity / (2f * _settings.Gravity);
            Assert.That(height, Is.EqualTo(_settings.JumpHeight).Within(0.0001f));
        }

        [Test]
        public void CharacterHeightMatchesHumanBaseSpecification()
        {
            Assert.That(_settings.CharacterHeight, Is.EqualTo(1.78f).Within(0.0001f));
        }

        [Test]
        public void ControllerReferencesSettingsInsteadOfDuplicatingGameplayValues()
        {
            var player = new GameObject("Inspector test player");
            try
            {
                var movement = player.AddComponent<HumanLocomotionController>();
                var serialized = new SerializedObject(movement);
                Assert.That(serialized.FindProperty("settings"), Is.Not.Null);
                Assert.That(serialized.FindProperty("walkSpeed"), Is.Null);
                Assert.That(serialized.FindProperty("jumpHeight"), Is.Null);
                Assert.That(serialized.FindProperty("gravity"), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void AnimationClipLoopModesAreInspectorConfigurable()
        {
            var animationSettings = ScriptableObject.CreateInstance<PlayerAnimationSettings>();
            try
            {
                var serialized = new SerializedObject(animationSettings);
                var fields = new[]
                {
                    "idleLoops", "walkLoops", "runLoops", "jumpLoops",
                    "fallLoops", "landLoops", "crouchIdleLoops", "crouchWalkLoops"
                };
                foreach (var field in fields)
                    Assert.That(serialized.FindProperty(field), Is.Not.Null,
                        "Missing serialized animation loop setting: " + field);

                serialized.FindProperty("idleLoops").boolValue = true;
                serialized.FindProperty("walkLoops").boolValue = false;
                serialized.FindProperty("runLoops").boolValue = true;
                serialized.FindProperty("jumpLoops").boolValue = false;
                serialized.FindProperty("fallLoops").boolValue = true;
                serialized.FindProperty("landLoops").boolValue = false;
                serialized.FindProperty("crouchIdleLoops").boolValue = true;
                serialized.FindProperty("crouchWalkLoops").boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.IsTrue(animationSettings.IdleLoops);
                Assert.IsFalse(animationSettings.WalkLoops);
                Assert.IsTrue(animationSettings.RunLoops);
                Assert.IsFalse(animationSettings.JumpLoops);
                Assert.IsTrue(animationSettings.FallLoops);
                Assert.IsFalse(animationSettings.LandLoops);
                Assert.IsTrue(animationSettings.CrouchIdleLoops);
                Assert.IsFalse(animationSettings.CrouchWalkLoops);
            }
            finally
            {
                Object.DestroyImmediate(animationSettings);
            }
        }

        [Test]
        public void AllEightConfigurationGroupsAreSerializedOnSettingsAsset()
        {
            var serialized = new SerializedObject(_settings);
            var fields = new[]
            {
                "walkSpeed", "runMultiplier", "jumpHeight", "gravity", "airControl",
                "acceleration", "deceleration", "coyoteTime", "jumpBufferTime",
                "characterHeight", "femaleCharacterHeight", "crouchHeight", "capsuleRadius",
                "characterStepOffset", "characterSlopeLimit", "characterSkinWidth",
                "mouseSensitivity", "gamepadLookSensitivity", "maxLookAngle",
                "thirdPersonDistance", "cameraCollisionRadius", "cameraFollowSharpness", "cameraFieldOfView"
            };
            foreach (var field in fields)
                Assert.That(serialized.FindProperty(field), Is.Not.Null, "Missing serialized setting: " + field);
        }
    }
}
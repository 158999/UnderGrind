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
        public void AnimationRunThresholdFallsBetweenWalkAndRunSpeeds()
        {
            var animationSettings = AssetDatabase.LoadAssetAtPath<PlayerAnimationSettings>(
                "Assets/UnderGrind/Settings/PlayerAnimationSettings.asset");
            Assert.That(animationSettings, Is.Not.Null,
                "L'asset di configurazione animazioni deve esistere.");
            Assert.That(animationSettings.RunThreshold, Is.GreaterThan(_settings.WalkSpeed));
            Assert.That(animationSettings.RunThreshold, Is.LessThan(_settings.RunSpeed));
        }

        [Test]
        public void PlayerVisualRigHidesBodyInFirstPersonAndShowsItInThirdPerson()
        {
            var root = new GameObject("Visual Rig Test");
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(root.transform, false);
            var view = new GameObject("View").transform;
            view.SetParent(root.transform, false);
            var renderer = body.GetComponent<Renderer>();
            var rig = root.AddComponent<PlayerVisualRig>();
            rig.Configure(new Renderer[0], new[] { renderer },
                new Renderer[0], new Renderer[0]);

            rig.SetViewReference(view);
            Assert.IsFalse(renderer.enabled, "Il corpo proxy deve essere nascosto in prima persona.");
            rig.SetThirdPerson(true);
            Assert.IsTrue(renderer.enabled, "Il corpo deve essere visibile in terza persona.");
            rig.SetThirdPerson(false);
            Assert.IsFalse(renderer.enabled, "Il corpo deve tornare nascosto in prima persona.");

            Object.DestroyImmediate(root);
        }

        [Test]
        public void JumpVelocityProducesConfiguredApexHeight()
        {
            var height = _settings.JumpVelocity * _settings.JumpVelocity / (2f * _settings.Gravity);
            Assert.That(height, Is.EqualTo(_settings.JumpHeight).Within(0.0001f));
        }

        [Test]
        public void CharacterHeightsMatchMaleAndFemaleReferenceModels()
        {
            Assert.That(_settings.GetCharacterHeight(PlayerCharacterVariant.Male), Is.EqualTo(1.78f).Within(0.0001f));
            Assert.That(_settings.GetCharacterHeight(PlayerCharacterVariant.Female), Is.EqualTo(1.70f).Within(0.0001f));
        }

        [Test]
        public void FemaleCameraHeightsScaleWithFemaleCharacterHeight()
        {
            var ratio = _settings.FemaleCharacterHeight / _settings.CharacterHeight;
            Assert.That(_settings.GetStandingViewHeight(PlayerCharacterVariant.Female),
                Is.EqualTo(_settings.StandingViewHeight * ratio).Within(0.0001f));
            Assert.That(_settings.GetCrouchingViewHeight(PlayerCharacterVariant.Female),
                Is.EqualTo(_settings.CrouchingViewHeight * ratio).Within(0.0001f));
        }

        [Test]
        public void FemaleVariantAppliesFemaleCharacterControllerHeight()
        {
            var player = new GameObject("Female height test");
            try
            {
                var characterController = player.AddComponent<CharacterController>();
                var movement = player.AddComponent<HumanLocomotionController>();
                movement.ConfigureCharacterVariant(PlayerCharacterVariant.Female);
                movement.ConfigureSettings(_settings);
                Assert.That(characterController.height,
                    Is.EqualTo(_settings.FemaleCharacterHeight).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
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
        public void FallAndLandingClipsAreOptionalForControllerBuild()
        {
            var settings = ScriptableObject.CreateInstance<PlayerAnimationSettings>();
            var clips = new AnimationClip[6];
            try
            {
                var serialized = new SerializedObject(settings);
                var fields = new[]
                {
                    "idleClip", "walkClip", "runClip", "jumpClip",
                    "crouchIdleClip", "crouchWalkClip"
                };

                for (var i = 0; i < fields.Length; i++)
                {
                    clips[i] = new AnimationClip();
                    serialized.FindProperty(fields[i]).objectReferenceValue = clips[i];
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(settings.GetMissingRequiredClipNames(), Is.Empty,
                    "Fall and Landing must remain optional.");

                serialized.FindProperty("fallClip").objectReferenceValue = null;
                serialized.FindProperty("landClip").objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(settings.GetMissingRequiredClipNames(), Is.Empty);

                serialized.FindProperty("idleClip").objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(settings.GetMissingRequiredClipNames(),
                    Does.Contain("Idle / Standing Idle"));
            }
            finally
            {
                Object.DestroyImmediate(settings);
                foreach (var clip in clips)
                    if (clip != null) Object.DestroyImmediate(clip);
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
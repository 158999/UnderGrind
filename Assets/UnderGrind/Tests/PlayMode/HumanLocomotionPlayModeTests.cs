using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnderGrind.Locomotion;

namespace UnderGrind.Tests
{
    public sealed class HumanLocomotionPlayModeTests
    {
        [UnityTest]
        public IEnumerator PlayerInitializesWithHumanScaleAndConfiguredMovement()
        {
            var player = new GameObject("PlayMode locomotion test");
            try
            {
                var settings = CreateSettings();
                var movement = player.AddComponent<HumanLocomotionController>();
                movement.ConfigureSettings(settings);
                yield return null;

                var controller = player.GetComponent<CharacterController>();
                Assert.That(controller, Is.Not.Null);
                Assert.That(controller.height, Is.EqualTo(settings.CharacterHeight).Within(0.001f));
                Assert.That(movement.WalkSpeed, Is.EqualTo(settings.WalkSpeed).Within(0.001f));
                Assert.That(movement.RunSpeed / movement.WalkSpeed, Is.EqualTo(settings.RunMultiplier).Within(0.001f));
                Assert.That(movement.JumpHeight, Is.EqualTo(settings.JumpHeight).Within(0.001f));
                Assert.That(movement.IsCrouching, Is.False);
                Assert.That(movement.MoveInput, Is.EqualTo(Vector2.zero));
                Assert.That(movement.RunSpeed, Is.EqualTo(settings.RunSpeed).Within(0.001f));
                Object.Destroy(settings);
            }
            finally
            {
                Object.Destroy(player);
            }

            yield return null;
        }

        private static PlayerLocomotionSettings CreateSettings()
        {
            var settings = ScriptableObject.CreateInstance<PlayerLocomotionSettings>();
            Set(settings, "characterHeight", 1.78f);
            Set(settings, "femaleCharacterHeight", 1.70f);
            Set(settings, "walkSpeed", 1.8f);
            Set(settings, "runMultiplier", 4f / 3f);
            Set(settings, "jumpHeight", 0.4f);
            Set(settings, "gravity", 9.81f);
            Set(settings, "acceleration", 12f);
            Set(settings, "deceleration", 16f);
            Set(settings, "airControl", 0.65f);
            Set(settings, "coyoteTime", 0.1f);
            Set(settings, "jumpBufferTime", 0.12f);
            Set(settings, "groundedStickVelocity", 2f);
            Set(settings, "movementInputThreshold", 0.001f);
            Set(settings, "directionThreshold", 0.001f);
            Set(settings, "jumpApexStateThreshold", 0.05f);
            Set(settings, "crouchHeight", 1.12f);
            Set(settings, "crouchSpeedMultiplier", 0.5f);
            Set(settings, "stanceChangeSpeed", 8f);
            Set(settings, "capsuleRadius", 0.28f);
            Set(settings, "standingClearance", 0.01f);
            Set(settings, "stanceOverlapRadiusMultiplier", 0.95f);
            Set(settings, "characterStepOffset", 0.32f);
            Set(settings, "characterStepOffsetHeightRatio", 0.18f);
            Set(settings, "characterSlopeLimit", 48f);
            Set(settings, "characterMinMoveDistance", 0f);
            Set(settings, "characterSkinWidth", 0.08f);
            Set(settings, "characterDetectCollisions", true);
            Set(settings, "characterEnableOverlapRecovery", true);
            Set(settings, "mouseSensitivity", 0.12f);
            Set(settings, "gamepadLookSensitivity", 150f);
            Set(settings, "maxLookAngle", 82f);
            Set(settings, "standingViewHeight", 1.62f);
            Set(settings, "crouchingViewHeight", 0.98f);
            Set(settings, "thirdPersonCameraOffset", new Vector3(0f, 0.12f, 0f));
            Set(settings, "thirdPersonDistance", 3.25f);
            Set(settings, "cameraCollisionRadius", 0.16f);
            Set(settings, "cameraCollisionSurfaceOffset", 0.18f);
            Set(settings, "cameraFollowSharpness", 14f);
            Set(settings, "thirdPersonRotationSharpness", 12f);
            Set(settings, "cameraNearClipPlane", 0.04f);
            Set(settings, "cameraFieldOfView", 75f);
            return settings;
        }

        private static void Set<T>(PlayerLocomotionSettings settings, string fieldName, T value)
        {
            var field = typeof(PlayerLocomotionSettings).GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing serialized setting: " + fieldName);
            field.SetValue(settings, value);
        }

        [UnityTest]
        public IEnumerator FirstPersonHidesHeadButKeepsBodyAndArmsVisible()
        {
            var player = new GameObject("Visual rig test");
            var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var arms = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                head.name = "Head Mesh";
                body.name = "Torso Mesh";
                arms.name = "FirstPerson_Arms";
                head.transform.SetParent(player.transform, false);
                body.transform.SetParent(player.transform, false);
                arms.transform.SetParent(player.transform, false);
                var rig = player.AddComponent<PlayerVisualRig>();
                yield return null;

                rig.SetThirdPerson(false);
                Assert.That(head.GetComponent<Renderer>().enabled, Is.False);
                Assert.That(body.GetComponent<Renderer>().enabled, Is.True);
                Assert.That(arms.GetComponent<Renderer>().enabled, Is.True);

                rig.SetThirdPerson(true);
                Assert.That(head.GetComponent<Renderer>().enabled, Is.True);
                Assert.That(body.GetComponent<Renderer>().enabled, Is.True);
                Assert.That(arms.GetComponent<Renderer>().enabled, Is.False);
            }
            finally
            {
                Object.Destroy(player);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator VisualRigPreservesInitiallyDisabledRenderers()
        {
            var player = new GameObject("Visual rig disabled renderer test");
            var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                head.name = "Head Mesh";
                head.transform.SetParent(player.transform, false);
                head.GetComponent<Renderer>().enabled = false;
                var rig = player.AddComponent<PlayerVisualRig>();
                yield return null;
                rig.SetThirdPerson(true);
                Assert.That(head.GetComponent<Renderer>().enabled, Is.False);
            }
            finally
            {
                Object.Destroy(player);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator AnimationDriverWithoutAnimatorIsSafe()
        {
            var player = new GameObject("Animation driver no animator test");
            try
            {
                var driver = player.AddComponent<PlayerAnimationDriver>();
                yield return null;
                Assert.DoesNotThrow(() => driver.Apply(1.2f, Vector2.up, true, 0f, false, false, false, false));
            }
            finally
            {
                Object.Destroy(player);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator VisualRigRefreshDoesNotLoseOriginalRendererState()
        {
            var player = new GameObject("Visual rig refresh test");
            var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                head.name = "Head Mesh";
                head.transform.SetParent(player.transform, false);
                var rig = player.AddComponent<PlayerVisualRig>();
                yield return null;

                rig.SetThirdPerson(false);
                Assert.That(head.GetComponent<Renderer>().enabled, Is.False);
                rig.RefreshRenderers();
                rig.SetThirdPerson(true);
                Assert.That(head.GetComponent<Renderer>().enabled, Is.True);
            }
            finally
            {
                Object.Destroy(player);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator VisualRigBindsStableGeneratedPartNames()
        {
            var player = new GameObject("Generated rig binding test");
            var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var fpArms = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var unrelatedHair = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                head.name = "UG_Male_Head";
                body.name = "UG_Male_Body";
                fpArms.name = "UG_Male_FP_Arms";
                unrelatedHair.name = "UG_Male_Hair";
                head.transform.SetParent(player.transform, false);
                body.transform.SetParent(player.transform, false);
                fpArms.transform.SetParent(player.transform, false);
                unrelatedHair.transform.SetParent(player.transform, false);

                var rig = player.AddComponent<PlayerVisualRig>();
                yield return null;

                Assert.That(rig.ConfigureFromHierarchy(player.transform), Is.True);
                rig.SetThirdPerson(false);
                Assert.That(head.GetComponent<Renderer>().enabled, Is.False);
                Assert.That(body.GetComponent<Renderer>().enabled, Is.True);
                Assert.That(fpArms.GetComponent<Renderer>().enabled, Is.True);
                Assert.That(unrelatedHair.GetComponent<Renderer>().enabled, Is.True);

                rig.SetThirdPerson(true);
                Assert.That(head.GetComponent<Renderer>().enabled, Is.True);
                Assert.That(fpArms.GetComponent<Renderer>().enabled, Is.False);
                Assert.That(unrelatedHair.GetComponent<Renderer>().enabled, Is.True);
            }
            finally
            {
                Object.Destroy(player);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator ExplicitVisualRigBindingsOverrideNameDiscovery()
        {
            var player = new GameObject("Explicit visual rig test");
            var explicitHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var unrelatedHair = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                explicitHead.name = "Head Mesh";
                unrelatedHair.name = "Hair Accessory";
                explicitHead.transform.SetParent(player.transform, false);
                unrelatedHair.transform.SetParent(player.transform, false);
                var rig = player.AddComponent<PlayerVisualRig>();
                yield return null;

                rig.Configure(new[] { explicitHead.GetComponent<Renderer>() },
                    System.Array.Empty<Renderer>());
                rig.SetThirdPerson(false);

                Assert.That(explicitHead.GetComponent<Renderer>().enabled, Is.False);
                Assert.That(unrelatedHair.GetComponent<Renderer>().enabled, Is.True);
            }
            finally
            {
                Object.Destroy(player);
            }
            yield return null;
        }
    }
}
using NUnit.Framework;
using UnityEngine;
using UnderGrind.Locomotion;

namespace UnderGrind.Tests
{
    public sealed class HumanLocomotionTuningTests
    {
        [Test]
        public void RunIsExactlyOneThirdFasterThanWalk()
        {
            var run = HumanLocomotionController.DefaultWalkSpeed * HumanLocomotionController.DefaultRunMultiplier;
            Assert.That(run, Is.EqualTo(HumanLocomotionController.DefaultWalkSpeed * 4f / 3f).Within(0.0001f));
            Assert.That(run / HumanLocomotionController.DefaultWalkSpeed, Is.EqualTo(4f / 3f).Within(0.0001f));
        }

        [Test]
        public void JumpVelocityProducesConfiguredApexHeight()
        {
            const float gravity = 9.81f;
            var velocity = Mathf.Sqrt(2f * gravity * HumanLocomotionController.DefaultJumpHeight);
            var height = velocity * velocity / (2f * gravity);
            Assert.That(height, Is.EqualTo(HumanLocomotionController.DefaultJumpHeight).Within(0.0001f));
        }

        [Test]
        public void CharacterHeightMatchesHumanBaseSpecification()
        {
            Assert.That(HumanLocomotionController.DefaultCharacterHeight, Is.EqualTo(1.78f).Within(0.0001f));
        }

        [Test]
        public void PlayerMovementParametersAreExposedAsSerializedInspectorFields()
        {
            var player = new GameObject("Inspector test player");
            try
            {
                var movement = player.AddComponent<HumanLocomotionController>();
                var serialized = new UnityEditor.SerializedObject(movement);
                Assert.That(serialized.FindProperty("characterHeight"), Is.Not.Null);
                Assert.That(serialized.FindProperty("walkSpeed"), Is.Not.Null);
                Assert.That(serialized.FindProperty("runMultiplier"), Is.Not.Null);
                Assert.That(serialized.FindProperty("jumpHeight"), Is.Not.Null);
                Assert.That(serialized.FindProperty("gravity"), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }
    }
}
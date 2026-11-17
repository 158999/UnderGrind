using System.Collections;
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
                var movement = player.AddComponent<HumanLocomotionController>();
                yield return null;

                var controller = player.GetComponent<CharacterController>();
                Assert.That(controller, Is.Not.Null);
                Assert.That(controller.height, Is.EqualTo(1.78f).Within(0.001f));
                Assert.That(movement.WalkSpeed, Is.EqualTo(1.4f).Within(0.001f));
                Assert.That(movement.RunSpeed / movement.WalkSpeed, Is.EqualTo(4f / 3f).Within(0.001f));
                Assert.That(movement.JumpHeight, Is.EqualTo(0.4f).Within(0.001f));
                Assert.That(movement.IsCrouching, Is.False);
                Assert.That(movement.MoveInput, Is.EqualTo(Vector2.zero));
                Assert.That(movement.RunSpeed, Is.EqualTo(1.4f * 4f / 3f).Within(0.001f));
            }
            finally
            {
                Object.Destroy(player);
            }

            yield return null;
        }
    }
}
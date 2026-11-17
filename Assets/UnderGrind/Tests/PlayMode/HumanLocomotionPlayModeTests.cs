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
    }
}
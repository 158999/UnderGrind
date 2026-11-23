using NUnit.Framework;
using UnderGrind.Locomotion;

namespace UnderGrind.Tests
{
    public sealed class PlayerFootSeparationIKTests
    {
        [TestCase(0.20f, 0.12f, 0.14f, 0f)]
        [TestCase(0.12f, 0.12f, 0.14f, 0f)]
        [TestCase(0.02f, 0.12f, 0.14f, 0.05f)]
        [TestCase(-0.10f, 0.12f, 0.14f, 0.11f)]
        [TestCase(-0.50f, 0.12f, 0.14f, 0.14f)]
        public void CorrectionIsBoundedAndOnlyAppliedBelowMinimum(
            float separation, float minimum, float maximum, float expected)
        {
            Assert.That(PlayerFootSeparationIK.CalculatePerFootCorrection(separation, minimum, maximum),
                Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void DisabledOrZeroCorrectionNeverMovesFeet()
        {
            Assert.That(PlayerFootSeparationIK.CalculatePerFootCorrection(-0.2f, 0f, 0.14f), Is.Zero);
            Assert.That(PlayerFootSeparationIK.CalculatePerFootCorrection(-0.2f, 0.12f, 0f), Is.Zero);
        }
    }
}

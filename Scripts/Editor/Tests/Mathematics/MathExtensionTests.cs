using Anvil.Unity.Core;
using Anvil.Unity.DOTS.Mathematics;
using NUnit.Framework;
using Unity.Mathematics;
using MathExtension = Anvil.Unity.DOTS.Mathematics.MathExtension;

namespace Anvil.Unity.DOTS.Tests.Mathematics
{
    public static class MathExtensionTests
    {
        private static int EqualityWithTolerance(float3 a, float3 b)
        {
            return math.all(a.IsApproximately(b)) ? 0 : 1;
        }

        private static int EqualityWithTolerance(float3x3 a, float3x3 b)
        {
            return a.IsApproximately(b).Equals(new bool3x3(true)) ? 0 : 1;
        }

        /// <remarks>
        /// Tests <see cref="DOTS.Mathematics.MathExtension.GetScale()"/> and
        /// <see cref="DOTS.Mathematics.MathExtension.GetRotation"/> need to be used in tandem to produce valid results
        /// so they share she same tests.
        /// </remarks>
        [Test]
        public static void GetScaleAndGetRotationTest()
        {
            Assert.That(nameof(GetScaleAndGetRotationTest), Does.Contain(nameof(MathExtension.GetRotation)).And.Contain(nameof(MathExtension.GetScale)));

            float4x4 transform_identity = float4x4.TRS(float3.zero, quaternion.identity, new float3(1f));

            float4x4 transform_translate = float4x4.TRS(new float3(7f), quaternion.identity, new float3(1f));
            float4x4 transform_negativeTranslate = float4x4.TRS(new float3(-7f), quaternion.identity, new float3(1f));

            quaternion transform_rotate_rotation = quaternion.Euler(math.radians(45f));
            float4x4 transform_rotate = float4x4.TRS(float3.zero, transform_rotate_rotation, new float3(1f));
            quaternion transform_Zrotate_rotation = quaternion.Euler(math.radians(0), math.radians(0), math.radians(45));
            float4x4 transform_Zrotate = float4x4.TRS(
                float3.zero,
                transform_Zrotate_rotation,
                new float3(1f));

            float4x4 transform_scale = float4x4.TRS(float3.zero, quaternion.identity, new float3(7f));
            float4x4 transform_negativeScale = float4x4.TRS(float3.zero, quaternion.identity, new float3(-7f));
            float4x4 transform_Zscale = float4x4.TRS(float3.zero, quaternion.identity, new float3(1f, 1f, 7f));
            float4x4 transform_negativeZscale = float4x4.TRS(float3.zero, quaternion.identity, new float3(1f, 1f, -7f));

            float4x4 transform_scale_zero = float4x4.TRS(float3.zero, quaternion.identity, float3.zero);

            quaternion transform_compound_rotate = quaternion.Euler(math.radians(0), math.radians(0), math.radians(45));
            float4x4 transform_compound = float4x4.TRS(
                new float3(7f),
                transform_compound_rotate,
                new float3(7f));
            quaternion transform_negativeCompound_rotate = quaternion.Euler(math.radians(-45));
            float4x4 transform_negativeCompound = float4x4.TRS(
                new float3(-7f),
                transform_negativeCompound_rotate,
                new float3(-7f));

            Assert.That(
                math.mul(new float3x3(transform_identity.GetRotation()), float3x3.Scale(transform_identity.GetScale())),
                Is.EqualTo((float3x3)transform_identity).Using<float3x3>(EqualityWithTolerance));

            Assert.That(
                math.mul(new float3x3(transform_translate.GetRotation()), float3x3.Scale(transform_translate.GetScale())),
                Is.EqualTo((float3x3)transform_translate).Using<float3x3>(EqualityWithTolerance));
            Assert.That(
                math.mul(new float3x3(transform_negativeTranslate.GetRotation()), float3x3.Scale(transform_negativeTranslate.GetScale())),
                Is.EqualTo((float3x3)transform_negativeTranslate).Using<float3x3>(EqualityWithTolerance));

            Assert.That(
                math.mul(new float3x3(transform_rotate.GetRotation()), float3x3.Scale(transform_rotate.GetScale())),
                Is.EqualTo((float3x3)transform_rotate).Using<float3x3>(EqualityWithTolerance));
            Assert.That(
                math.mul(new float3x3(transform_Zrotate.GetRotation()), float3x3.Scale(transform_Zrotate.GetScale())),
                Is.EqualTo((float3x3)transform_Zrotate).Using<float3x3>(EqualityWithTolerance));

            Assert.That(
                math.mul(new float3x3(transform_scale.GetRotation()), float3x3.Scale(transform_scale.GetScale())),
                Is.EqualTo((float3x3)transform_scale).Using<float3x3>(EqualityWithTolerance));
            Assert.That(
                math.mul(new float3x3(transform_negativeScale.GetRotation()), float3x3.Scale(transform_negativeScale.GetScale())),
                Is.EqualTo((float3x3)transform_negativeScale).Using<float3x3>(EqualityWithTolerance));
            Assert.That(
                math.mul(new float3x3(transform_Zscale.GetRotation()), float3x3.Scale(transform_Zscale.GetScale())),
                Is.EqualTo((float3x3)transform_Zscale).Using<float3x3>(EqualityWithTolerance));
            Assert.That(
                math.mul(new float3x3(transform_negativeZscale.GetRotation()), float3x3.Scale(transform_negativeZscale.GetScale())),
                Is.EqualTo((float3x3)transform_negativeZscale).Using<float3x3>(EqualityWithTolerance));

            Assert.That(
                math.mul(new float3x3(transform_scale_zero.GetRotation()), float3x3.Scale(transform_scale_zero.GetScale())),
                Is.EqualTo((float3x3)transform_scale_zero).Using<float3x3>(EqualityWithTolerance));

            Assert.That(
                math.mul(new float3x3(transform_compound.GetRotation()), float3x3.Scale(transform_compound.GetScale())),
                Is.EqualTo((float3x3)transform_compound).Using<float3x3>(EqualityWithTolerance));
            Assert.That(
                math.mul(new float3x3(transform_negativeCompound.GetRotation()), float3x3.Scale(transform_negativeCompound.GetScale())),
                Is.EqualTo((float3x3)transform_negativeCompound).Using<float3x3>(EqualityWithTolerance));
        }

        [Test]
        public static void GetTranslationTest()
        {
            Assert.That(nameof(GetTranslationTest), Does.StartWith(nameof(MathExtension.GetTranslation)));

            float3 point_zero = float3.zero;
            float3 point_seven = new float3(7f);
            float3 point_negativeSeven = new float3(-7f);

            float4x4 transform_identity = float4x4.TRS(point_zero, quaternion.identity, new float3(1f));

            float4x4 transform_translate = float4x4.TRS(point_seven, quaternion.identity, new float3(1f));
            float4x4 transform_negativeTranslate = float4x4.TRS(point_negativeSeven, quaternion.identity, new float3(1f));

            float4x4 transform_compound = float4x4.TRS(
                point_seven,
                quaternion.Euler(math.radians(0), math.radians(0), math.radians(45)),
                new float3(7f));
            float4x4 transform_negativeCompound = float4x4.TRS(
                point_negativeSeven,
                quaternion.Euler(math.radians(-45)),
                new float3(-7f));

            Assert.That(transform_identity.GetTranslation(), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));

            Assert.That(transform_translate.GetTranslation(), Is.EqualTo(point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(transform_negativeTranslate.GetTranslation(), Is.EqualTo(point_negativeSeven).Using<float3>(EqualityWithTolerance));

            Assert.That(transform_compound.GetTranslation(), Is.EqualTo(point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(transform_negativeCompound.GetTranslation(), Is.EqualTo(point_negativeSeven).Using<float3>(EqualityWithTolerance));
        }

        [Test]
        public static void IsValidTransformTest()
        {
            Assert.That(nameof(IsValidTransformTest), Does.StartWith(nameof(MathExtension.IsValidTransform)));

            float3 point_zero = float3.zero;
            float3 point_seven = new float3(7f);
            float3 point_negativeSeven = new float3(-7f);

            float4x4 transform_identity = float4x4.TRS(point_zero, quaternion.identity, new float3(1f));

            float4x4 transform_translate = float4x4.TRS(point_seven, quaternion.identity, new float3(1f));
            float4x4 transform_negativeTranslate = float4x4.TRS(point_negativeSeven, quaternion.identity, new float3(1f));

            float4x4 transform_nan = new float4x4(float.NaN);
            float4x4 transform_scale_zero = float4x4.TRS(float3.zero, quaternion.identity, point_zero);

            float4x4 transform_scale = float4x4.TRS(float3.zero, quaternion.identity, new float3(7f));
            float4x4 transform_negativeScale = float4x4.TRS(float3.zero, quaternion.identity, new float3(-7f));
            float4x4 transform_Zscale = float4x4.TRS(float3.zero, quaternion.identity, new float3(1f, 1f, 7f));
            float4x4 transform_negativeZscale = float4x4.TRS(float3.zero, quaternion.identity, new float3(1f, 1f, -7f));

            float4x4 transform_compound = float4x4.TRS(
                point_seven,
                quaternion.Euler(math.radians(0), math.radians(0), math.radians(45)),
                new float3(7f));
            float4x4 transform_negativeCompound = float4x4.TRS(
                point_negativeSeven,
                quaternion.Euler(math.radians(-45)),
                new float3(-7f));

            Assert.That(transform_identity.IsValidTransform(), Is.True);

            Assert.That(transform_translate.IsValidTransform(), Is.True);
            Assert.That(transform_negativeTranslate.IsValidTransform(), Is.True);

            Assert.That(transform_nan.IsValidTransform(), Is.False);
            Assert.That(transform_scale_zero.IsValidTransform(), Is.False);

            Assert.That(transform_scale.IsValidTransform(), Is.True);
            Assert.That(transform_negativeScale.IsValidTransform(), Is.True);
            Assert.That(transform_Zscale.IsValidTransform(), Is.True);
            Assert.That(transform_negativeZscale.IsValidTransform(), Is.True);

            Assert.That(transform_compound.IsValidTransform(), Is.True);
            Assert.That(transform_negativeCompound.IsValidTransform(), Is.True);
        }
    }
}

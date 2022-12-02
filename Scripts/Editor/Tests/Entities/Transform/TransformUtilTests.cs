using System.Text.RegularExpressions;
using Anvil.CSharp.Logging;
using Anvil.Unity.Core;
using Anvil.Unity.DOTS.Entities.Transform;
using Anvil.Unity.DOTS.Mathematics;
using NUnit.Framework;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.TestTools;
using Logger = Anvil.CSharp.Logging.Logger;

namespace Anvil.Unity.DOTS.Tests.Entities.Transform
{
    public static class TransformUtilTests
    {
        private static Logger Logger
        {
            get => Log.GetStaticLogger(typeof(TransformUtil));
        }

        //TODO: Update to use [DefaultFloatingPointTolerance] when Unity uses NUnit >=3.7 and remove all .Using<float3>(EqualityWithTolerance) uses
        // https://github.com/nunit/nunit/blob/master/src/NUnitFramework/framework/Attributes/DefaultFloatingPointToleranceAttribute.cs
        private const float FLOATING_POINT_TOLERANCE = 0.00001f;
        private static readonly Regex s_NonUniformScaleError = new Regex(@"This conversion does not support transforms with non-uniform scaling\.");

        private static int EqualityWithTolerance(float3 a, float3 b)
        {
            return math.all(a.IsApproximately(b)) ? 0 : 1;
        }
        private static int EqualityWithTolerance(float4 a, float4 b)
        {
            return math.all(a.IsApproximately(b)) ? 0 : 1;
        }
        private static int EqualityWithTolerance(float3x3 a, float3x3 b)
        {
            return a.IsApproximately(b).Equals(new bool3x3(true)) ? 0 : 1;
        }
        private static int EqualityWithTolerance(quaternion a, quaternion b)
        {
            return EqualityWithTolerance(a.value, b.value);
        }

        private static int EquivalencyWithTolerance(Rect a, Rect b)
        {
            return RectUtil.AreEquivalent(a, b) ? 0 : 1;
        }

        // ----- ConvertWorldToLocalPointTest ----- //
        [Test]
        public static void ConvertWorldToLocalPointTest_Identity()
        {
            float3 point_zero = float3.zero;
            float3 point_one = new float3(1, 1, 1);
            float3 point_seven = point_one * 7f;
            float3 point_sevenXY = new float3(point_seven.xy, 0);

            LocalToWorld localToWorld_Identity = new LocalToWorld() { Value = float4x4.identity };
            float4x4 worldToLocal_TranslatedOne = math.inverse(localToWorld_Identity.Value);
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_Identity, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_Identity, point_one), Is.EqualTo(point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_Identity, point_seven), Is.EqualTo(point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_Identity, point_sevenXY), Is.EqualTo(point_sevenXY).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_Identity, -point_one), Is.EqualTo(-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_Identity, -point_seven), Is.EqualTo(-point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_Identity, -point_sevenXY), Is.EqualTo(-point_sevenXY).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedOne, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedOne, point_one), Is.EqualTo(point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedOne, point_seven), Is.EqualTo(point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedOne, point_sevenXY), Is.EqualTo(point_sevenXY).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedOne, -point_one), Is.EqualTo(-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedOne, -point_seven), Is.EqualTo(-point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedOne, -point_sevenXY), Is.EqualTo(-point_sevenXY).Using<float3>(EqualityWithTolerance));
        }

        [Test]
        public static void ConvertWorldToLocalPointTest_Translate()
        {
            float3 point_zero = float3.zero;
            float3 point_one = new float3(1, 1, 1);
            float3 point_seven = point_one * 7f;
            float3 point_oneXY = new float3(1, 1, 0);
            float3 point_sevenXY = point_oneXY * 7f;

            LocalToWorld localToWorld_TranslatedOne = new LocalToWorld()
            {
                Value = float4x4.TRS(point_one, quaternion.identity, point_one)
            };
            float4x4 worldToLocal_TranslatedOne = math.inverse(localToWorld_TranslatedOne.Value);
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedOne, point_zero), Is.EqualTo(-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedOne, point_one), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedOne, point_seven), Is.EqualTo(point_seven-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedOne, point_sevenXY), Is.EqualTo(point_sevenXY-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedOne, -point_one), Is.EqualTo(-point_one-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedOne, -point_seven), Is.EqualTo(-point_seven-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedOne, -point_sevenXY), Is.EqualTo(-point_sevenXY-point_one).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedOne, point_zero), Is.EqualTo(-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedOne, point_one), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedOne, point_seven), Is.EqualTo(point_seven-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedOne, point_sevenXY), Is.EqualTo(point_sevenXY-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedOne, -point_one), Is.EqualTo(-point_one-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedOne, -point_seven), Is.EqualTo(-point_seven-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedOne, -point_sevenXY), Is.EqualTo(-point_sevenXY-point_one).Using<float3>(EqualityWithTolerance));


            LocalToWorld localToWorld_TranslatedNegativeOne = new LocalToWorld()
            {
                Value = float4x4.TRS(-point_one, quaternion.identity, point_one)
            };
            float4x4 worldToLocal_TranslatedNegativeOne = math.inverse(localToWorld_TranslatedNegativeOne.Value);
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedNegativeOne, point_zero), Is.EqualTo(point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedNegativeOne, point_one), Is.EqualTo(point_one+point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedNegativeOne, point_seven), Is.EqualTo(point_seven+point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedNegativeOne, point_sevenXY), Is.EqualTo(point_sevenXY+point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedNegativeOne, -point_one), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedNegativeOne, -point_seven), Is.EqualTo(-point_seven+point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedNegativeOne, -point_sevenXY), Is.EqualTo(-point_sevenXY+point_one).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedNegativeOne, point_zero), Is.EqualTo(point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedNegativeOne, point_one), Is.EqualTo(point_one+point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedNegativeOne, point_seven), Is.EqualTo(point_seven+point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedNegativeOne, point_sevenXY), Is.EqualTo(point_sevenXY+point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedNegativeOne, -point_one), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedNegativeOne, -point_seven), Is.EqualTo(-point_seven+point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedNegativeOne, -point_sevenXY), Is.EqualTo(-point_sevenXY+point_one).Using<float3>(EqualityWithTolerance));


            LocalToWorld localToWorld_TranslatedSeven = new LocalToWorld()
            {
                Value = float4x4.TRS(point_seven, quaternion.identity, point_one)
            };
            float4x4 worldToLocal_TranslatedSeven = math.inverse(localToWorld_TranslatedSeven.Value);
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedSeven, point_zero), Is.EqualTo(-point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedSeven, point_one), Is.EqualTo(-point_seven+point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedSeven, point_seven), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedSeven, point_sevenXY), Is.EqualTo(point_sevenXY - point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedSeven, -point_one), Is.EqualTo(-point_seven-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedSeven, -point_seven), Is.EqualTo(-point_seven-point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedSeven, -point_sevenXY), Is.EqualTo(-point_sevenXY-point_seven).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedSeven, point_zero), Is.EqualTo(-point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedSeven, point_one), Is.EqualTo(-point_seven+point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedSeven, point_seven), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedSeven, point_sevenXY), Is.EqualTo(point_sevenXY - point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedSeven, -point_one), Is.EqualTo(-point_seven-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedSeven, -point_seven), Is.EqualTo(-point_seven-point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedSeven, -point_sevenXY), Is.EqualTo(-point_sevenXY-point_seven).Using<float3>(EqualityWithTolerance));


            LocalToWorld localToWorld_TranslatedNegativeSeven = new LocalToWorld()
            {
                Value = float4x4.TRS(-point_seven, quaternion.identity, point_one)
            };
            float4x4 worldToLocal_TranslatedNegativeSeven = math.inverse(localToWorld_TranslatedNegativeSeven.Value);
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedNegativeSeven, point_zero), Is.EqualTo(point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedNegativeSeven, point_one), Is.EqualTo(point_seven+point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedNegativeSeven, point_seven), Is.EqualTo(point_seven+point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedNegativeSeven, point_sevenXY), Is.EqualTo(point_sevenXY+point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedNegativeSeven, -point_one), Is.EqualTo(point_seven-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedNegativeSeven, -point_seven), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedNegativeSeven, -point_sevenXY), Is.EqualTo(point_seven-point_sevenXY).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedNegativeSeven, point_zero), Is.EqualTo(point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedNegativeSeven, point_one), Is.EqualTo(point_seven+point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedNegativeSeven, point_seven), Is.EqualTo(point_seven+point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedNegativeSeven, point_sevenXY), Is.EqualTo(point_sevenXY+point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedNegativeSeven, -point_one), Is.EqualTo(point_seven-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedNegativeSeven, -point_seven), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedNegativeSeven, -point_sevenXY), Is.EqualTo(point_seven-point_sevenXY).Using<float3>(EqualityWithTolerance));


            LocalToWorld localToWorld_TranslatedSevenXY = new LocalToWorld()
            {
                Value = float4x4.TRS(point_sevenXY, quaternion.identity, point_one)
            };
            float4x4 worldToLocal_TranslatedSevenXY = math.inverse(localToWorld_TranslatedSevenXY.Value);
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedSevenXY, point_zero), Is.EqualTo(-point_sevenXY).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedSevenXY, point_one), Is.EqualTo(-point_sevenXY+point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedSevenXY, point_seven), Is.EqualTo(point_seven-point_sevenXY).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedSevenXY, point_sevenXY), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedSevenXY, -point_one), Is.EqualTo(-point_sevenXY-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedSevenXY, -point_seven), Is.EqualTo(-point_seven-point_sevenXY).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedSevenXY, -point_sevenXY), Is.EqualTo(-point_sevenXY-point_sevenXY).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedSevenXY, point_zero), Is.EqualTo(-point_sevenXY).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedSevenXY, point_one), Is.EqualTo(-point_sevenXY+point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedSevenXY, point_seven), Is.EqualTo(point_seven-point_sevenXY).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedSevenXY, point_sevenXY), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedSevenXY, -point_one), Is.EqualTo(-point_sevenXY-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedSevenXY, -point_seven), Is.EqualTo(-point_seven-point_sevenXY).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_TranslatedSevenXY, -point_sevenXY), Is.EqualTo(-point_sevenXY-point_sevenXY).Using<float3>(EqualityWithTolerance));
        }

        [Test]
        public static void ConvertWorldToLocalPointTest_Rotate()
        {
            float3 point_zero = float3.zero;
            float3 point_one = new float3(1, 1, 1);
            float3 point_seven = point_one * 7f;

            LocalToWorld localToWorld_RotatedZ90 = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.Euler(0, 0, math.radians(90)), point_one)
            };
            float4x4 worldToLocal_RotatedZ90 = math.inverse(localToWorld_RotatedZ90.Value);
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_RotatedZ90, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_RotatedZ90, point_one), Is.EqualTo(new float3(1f, -1f, 1f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_RotatedZ90, point_seven), Is.EqualTo(new float3(7f, -7f, 7f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_RotatedZ90, -point_one), Is.EqualTo(new float3(-1f, 1f, -1f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_RotatedZ90, -point_seven), Is.EqualTo(new float3(-7f, 7f, -7f)).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_RotatedZ90, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_RotatedZ90, point_one), Is.EqualTo(new float3(1f, -1f, 1f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_RotatedZ90, point_seven), Is.EqualTo(new float3(7f, -7f, 7f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_RotatedZ90, -point_one), Is.EqualTo(new float3(-1f, 1f, -1f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_RotatedZ90, -point_seven), Is.EqualTo(new float3(-7f, 7f, -7f)).Using<float3>(EqualityWithTolerance));


            LocalToWorld localToWorld_RotatedZNegative90 = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.Euler(0, 0, math.radians(-90)), point_one)
            };
            float4x4 worldToLocal_RotatedNegative90 = math.inverse(localToWorld_RotatedZNegative90.Value);
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_RotatedZNegative90, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_RotatedZNegative90, point_one), Is.EqualTo(new float3(-1f, 1f, 1f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_RotatedZNegative90, point_seven), Is.EqualTo(new float3(-7f, 7f, 7f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_RotatedZNegative90, -point_one), Is.EqualTo(new float3(1f, -1f, -1f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_RotatedZNegative90, -point_seven), Is.EqualTo(new float3(7f, -7f, -7f)).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_RotatedNegative90, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_RotatedNegative90, point_one), Is.EqualTo(new float3(-1f, 1f, 1f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_RotatedNegative90, point_seven), Is.EqualTo(new float3(-7f, 7f, 7f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_RotatedNegative90, -point_one), Is.EqualTo(new float3(1f, -1f, -1f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_RotatedNegative90, -point_seven), Is.EqualTo(new float3(7f, -7f, -7f)).Using<float3>(EqualityWithTolerance));


            LocalToWorld localToWorld_RotatedZX90 = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.Euler(math.radians(90), 0, math.radians(90)), point_one)
            };
            float4x4 worldToLocal_RotatedZX90 = math.inverse(localToWorld_RotatedZX90.Value);
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_RotatedZX90, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_RotatedZX90, point_one), Is.EqualTo(new float3(1f, -1f, -1f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_RotatedZX90, point_seven), Is.EqualTo(new float3(7f, -7f, -7f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_RotatedZX90, -point_one), Is.EqualTo(new float3(-1f, 1f, 1f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_RotatedZX90, -point_seven), Is.EqualTo(new float3(-7f, 7f, 7f)).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_RotatedZX90, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_RotatedZX90, point_one), Is.EqualTo(new float3(1f, -1f, -1f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_RotatedZX90, point_seven), Is.EqualTo(new float3(7f, -7f, -7f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_RotatedZX90, -point_one), Is.EqualTo(new float3(-1f, 1f, 1f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_RotatedZX90, -point_seven), Is.EqualTo(new float3(-7f, 7f, 7f)).Using<float3>(EqualityWithTolerance));


            LocalToWorld localToWorld_RotatedZXNegative90 = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.Euler(math.radians(-90), 0, math.radians(-90)), point_one)
            };
            float4x4 worldToLocal_RotatedZXNegative90 = math.inverse(localToWorld_RotatedZXNegative90.Value);
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_RotatedZXNegative90, point_zero), Is.EqualTo(point_zero));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_RotatedZXNegative90, point_one), Is.EqualTo(point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_RotatedZXNegative90, point_seven), Is.EqualTo(point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_RotatedZXNegative90, -point_one), Is.EqualTo(-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_RotatedZXNegative90, -point_seven), Is.EqualTo(-point_seven).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_RotatedZXNegative90, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_RotatedZXNegative90, point_one), Is.EqualTo(point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_RotatedZXNegative90, point_seven), Is.EqualTo(point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_RotatedZXNegative90, -point_one), Is.EqualTo(-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_RotatedZXNegative90, -point_seven), Is.EqualTo(-point_seven).Using<float3>(EqualityWithTolerance));


            LocalToWorld localToWorld_RotatedZX45 = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.Euler(math.radians(45), 0, math.radians(45)), point_one)
            };
            float4x4 worldToLocal_RotatedZX45 = math.inverse(localToWorld_RotatedZX45.Value);
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_RotatedZX45, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_RotatedZX45, point_one), Is.EqualTo(new float3(1.70710683f,0.292893201f,0f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_RotatedZX45, point_seven), Is.EqualTo(new float3(11.9497471f,2.0502522f,0f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_RotatedZX45, -point_one), Is.EqualTo(new float3(-1.70710683f,-0.292893201f,0f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_RotatedZX45, -point_seven), Is.EqualTo(new float3(-11.9497471f,-2.0502522f,0f)).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_RotatedZX45, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_RotatedZX45, point_one), Is.EqualTo(new float3(1.70710683f,0.292893201f,0f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_RotatedZX45, point_seven), Is.EqualTo(new float3(11.9497471f,2.0502522f,0f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_RotatedZX45, -point_one), Is.EqualTo(new float3(-1.70710683f,-0.292893201f,0f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_RotatedZX45, -point_seven), Is.EqualTo(new float3(-11.9497471f,-2.0502522f,0f)).Using<float3>(EqualityWithTolerance));
        }

        [Test]
        public static void ConvertWorldToLocalPointTest_Scale()
        {
            Regex invalidMatrixError_point = new Regex(@"This transform is invalid\. Returning a signed infinite position\.");

            float3 point_infinity = new float3(float.PositiveInfinity);
            float3 point_zero = float3.zero;
            float3 point_one = new float3(1, 1, 1);
            float3 point_seven = point_one * 7f;

            LocalToWorld localToWorld_Scaled2 = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.identity, point_one*2f)
            };
            float4x4 worldToLocal_Scaled2 = math.inverse(localToWorld_Scaled2.Value);
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_Scaled2, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_Scaled2, point_one), Is.EqualTo(point_one/2f).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_Scaled2, point_seven), Is.EqualTo(point_seven/2f).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_Scaled2, -point_one), Is.EqualTo(-point_one/2f).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_Scaled2, -point_seven), Is.EqualTo(-point_seven/2f).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_Scaled2, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_Scaled2, point_one), Is.EqualTo(point_one/2f).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_Scaled2, point_seven), Is.EqualTo(point_seven/2f).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_Scaled2, -point_one), Is.EqualTo(-point_one/2f).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_Scaled2, -point_seven), Is.EqualTo(-point_seven/2f).Using<float3>(EqualityWithTolerance));


            LocalToWorld localToWorld_ScaledNegative2 = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.identity, point_one*-2f)
            };
            float4x4 worldToLocal_ScaledNegative2 = math.inverse(localToWorld_ScaledNegative2.Value);
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_ScaledNegative2, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_ScaledNegative2, point_one), Is.EqualTo(-point_one/2f).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_ScaledNegative2, point_seven), Is.EqualTo(-point_seven/2f).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_ScaledNegative2, -point_one), Is.EqualTo(point_one/2f).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_ScaledNegative2, -point_seven), Is.EqualTo(point_seven/2f).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_ScaledNegative2, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_ScaledNegative2, point_one), Is.EqualTo(-point_one/2f).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_ScaledNegative2, point_seven), Is.EqualTo(-point_seven/2f).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_ScaledNegative2, -point_one), Is.EqualTo(point_one/2f).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_ScaledNegative2, -point_seven), Is.EqualTo(point_seven/2f).Using<float3>(EqualityWithTolerance));


            float3 scaleZ2 = new float3(point_one.xy, 2f);
            LocalToWorld localToWorld_ScaledZ2 = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.identity, scaleZ2)
            };
            float4x4 worldToLocal_ScaledZ2 = math.inverse(localToWorld_ScaledZ2.Value);
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_ScaledZ2, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_ScaledZ2, point_one), Is.EqualTo(point_one/scaleZ2).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_ScaledZ2, point_seven), Is.EqualTo(point_seven/scaleZ2).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_ScaledZ2, -point_one), Is.EqualTo(-point_one/scaleZ2).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_ScaledZ2, -point_seven), Is.EqualTo(-point_seven/scaleZ2).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_ScaledZ2, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_ScaledZ2, point_one), Is.EqualTo(point_one/scaleZ2).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_ScaledZ2, point_seven), Is.EqualTo(point_seven/scaleZ2).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_ScaledZ2, -point_one), Is.EqualTo(-point_one/scaleZ2).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_ScaledZ2, -point_seven), Is.EqualTo(-point_seven/scaleZ2).Using<float3>(EqualityWithTolerance));


            float3 scaleZNegative2 = new float3(point_one.xy, -2f);
            LocalToWorld localToWorld_ScaledZNegative2 = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.identity, scaleZNegative2)
            };
            float4x4 worldToLocal_ScaledZNegative2 = math.inverse(localToWorld_ScaledZNegative2.Value);
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_ScaledZNegative2, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_ScaledZNegative2, point_one), Is.EqualTo(point_one/scaleZNegative2).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_ScaledZNegative2, point_seven), Is.EqualTo(point_seven/scaleZNegative2).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_ScaledZNegative2, -point_one), Is.EqualTo(-point_one/scaleZNegative2).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_ScaledZNegative2, -point_seven), Is.EqualTo(-point_seven/scaleZNegative2).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_ScaledZNegative2, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_ScaledZNegative2, point_one), Is.EqualTo(point_one/scaleZNegative2).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_ScaledZNegative2, point_seven), Is.EqualTo(point_seven/scaleZNegative2).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_ScaledZNegative2, -point_one), Is.EqualTo(-point_one/scaleZNegative2).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_ScaledZNegative2, -point_seven), Is.EqualTo(-point_seven/scaleZNegative2).Using<float3>(EqualityWithTolerance));


            float3 scaleZXOnePointFive = new float3(1.5f, 1f, 1.5f);
            LocalToWorld localToWorld_ScaledZXOnePointFive = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.identity, scaleZXOnePointFive)
            };
            float4x4 worldToLocal_ScaledZXOnePointFive = math.inverse(localToWorld_ScaledZXOnePointFive.Value);
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_ScaledZXOnePointFive, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_ScaledZXOnePointFive, point_one), Is.EqualTo(point_one/scaleZXOnePointFive).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_ScaledZXOnePointFive, point_seven), Is.EqualTo(point_seven/scaleZXOnePointFive).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_ScaledZXOnePointFive, -point_one), Is.EqualTo(-point_one/scaleZXOnePointFive).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_ScaledZXOnePointFive, -point_seven), Is.EqualTo(-point_seven/scaleZXOnePointFive).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_ScaledZXOnePointFive, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_ScaledZXOnePointFive, point_one), Is.EqualTo(point_one/scaleZXOnePointFive).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_ScaledZXOnePointFive, point_seven), Is.EqualTo(point_seven/scaleZXOnePointFive).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_ScaledZXOnePointFive, -point_one), Is.EqualTo(-point_one/scaleZXOnePointFive).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_ScaledZXOnePointFive, -point_seven), Is.EqualTo(-point_seven/scaleZXOnePointFive).Using<float3>(EqualityWithTolerance));


            LocalToWorld localToWorld_zero = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.identity, point_zero)
            };
            float4x4 worldToLocal_zero = float4x4.TRS(point_zero, quaternion.identity, point_zero);

            Logger.Debug("BEGIN: Expected error messages.");
            LogAssert.Expect(LogType.Error, invalidMatrixError_point);
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_zero, point_zero), Is.EqualTo(point_zero));
            LogAssert.Expect(LogType.Error, invalidMatrixError_point);
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_zero, point_one), Is.EqualTo(point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_point);
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_zero, point_seven), Is.EqualTo(point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_point);
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_zero, -point_one), Is.EqualTo(-point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_point);
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_zero, -point_seven), Is.EqualTo(-point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_point);
            Assert.That(
                TransformUtil.ConvertWorldToLocalPoint(localToWorld_zero, new float3(2, -2, 0)),
                Is.EqualTo(new float3(float.PositiveInfinity, float.NegativeInfinity, 0))
                );

            LogAssert.Expect(LogType.Error, invalidMatrixError_point);
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_zero, point_zero), Is.EqualTo(point_zero));
            LogAssert.Expect(LogType.Error, invalidMatrixError_point);
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_zero, point_one), Is.EqualTo(point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_point);
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_zero, point_seven), Is.EqualTo(point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_point);
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_zero, -point_one), Is.EqualTo(-point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_point);
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_zero, -point_seven), Is.EqualTo(-point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_point);
            Assert.That(
                TransformUtil.ConvertWorldToLocalPoint(worldToLocal_zero, new float3(2, -2, 0)),
                Is.EqualTo(new float3(float.PositiveInfinity, float.NegativeInfinity, 0))
            );
            Logger.Debug("END: Expected error messages.");
        }

        [Test]
        public static void ConvertWorldToLocalPointTest_Compound()
        {
            float3 point_zero = float3.zero;
            float3 point_one = new float3(1f, 1f, 1f);
            float3 point_seven = point_one * 7f;
            float3 point_sevenXY = new float3(point_seven.xy, 0f);

            LocalToWorld localToWorld_Compound = new LocalToWorld()
            {
                Value = float4x4.TRS(point_sevenXY, quaternion.Euler(math.radians(45), 0, math.radians(45)), point_one*2f)
            };
            float4x4 worldToLocal_Compound = math.inverse(localToWorld_Compound.Value);
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_Compound, point_zero), Is.EqualTo(new float3(-4.22487354f, 0.7248739f, 2.47487402f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_Compound, point_one), Is.EqualTo(new float3(-3.37132025f, 0.871320367f, 2.47487402f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_Compound, point_seven), Is.EqualTo(new float3(1.75f, 1.75f, 2.47487378f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_Compound, -point_one), Is.EqualTo(new float3(-5.07842731f, 0.578427196f, 2.47487402f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_Compound, -point_seven), Is.EqualTo(new float3(-10.1997471f, -0.300252199f, 2.4748745f)).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_Compound, point_zero), Is.EqualTo(new float3(-4.22487354f, 0.7248739f, 2.47487402f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_Compound, point_one), Is.EqualTo(new float3(-3.37132025f, 0.871320367f, 2.47487402f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_Compound, point_seven), Is.EqualTo(new float3(1.75f, 1.75f, 2.47487378f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_Compound, -point_one), Is.EqualTo(new float3(-5.07842731f, 0.578427196f, 2.47487402f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_Compound, -point_seven), Is.EqualTo(new float3(-10.1997471f, -0.300252199f, 2.4748745f)).Using<float3>(EqualityWithTolerance));


            LocalToWorld localToWorld_CompoundNegative = new LocalToWorld()
            {
                Value = float4x4.TRS(-point_sevenXY, quaternion.Euler(math.radians(-45), 0, math.radians(-45)), point_one*-2f)
            };
            float4x4 worldToLocal_CompoundNegative = math.inverse(localToWorld_CompoundNegative.Value);
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_CompoundNegative, point_zero), Is.EqualTo(new float3(-0.724873781f, -4.22487402f, -2.47487402f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_CompoundNegative, point_one), Is.EqualTo(new float3(-1.07842731f, -4.57842731f, -3.18198061f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_CompoundNegative, point_seven), Is.EqualTo(new float3(-3.19974756f,-6.69974804f,-7.42462158f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_CompoundNegative, -point_one), Is.EqualTo(new float3(-0.371320248f, -3.87132072f, -1.76776719f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_CompoundNegative, -point_seven), Is.EqualTo(new float3(1.75f, -1.75f, 2.47487378f)).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_CompoundNegative, point_zero), Is.EqualTo(new float3(-0.724873781f, -4.22487402f, -2.47487402f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_CompoundNegative, point_one), Is.EqualTo(new float3(-1.07842731f, -4.57842731f, -3.18198061f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_CompoundNegative, point_seven), Is.EqualTo(new float3(-3.19974756f,-6.69974804f,-7.42462158f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_CompoundNegative, -point_one), Is.EqualTo(new float3(-0.371320248f, -3.87132072f, -1.76776719f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(worldToLocal_CompoundNegative, -point_seven), Is.EqualTo(new float3(1.75f, -1.75f, 2.47487378f)).Using<float3>(EqualityWithTolerance));
        }

        // ----- ConvertLocalToWorldPointTest ----- //
        [Test]
        public static void ConvertLocalToWorldPointTest_Identity()
        {
            float3 point_zero = float3.zero;
            float3 point_one = new float3(1, 1, 1);
            float3 point_seven = point_one * 7f;
            float3 point_oneXY = new float3(1, 1, 0);
            float3 point_sevenXY = point_oneXY * 7f;

            LocalToWorld localToWorld_Identity = new LocalToWorld() { Value = float4x4.identity };
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Identity, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Identity, point_one), Is.EqualTo(point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Identity, point_seven), Is.EqualTo(point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_Identity, point_sevenXY), Is.EqualTo(point_sevenXY).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Identity, -point_one), Is.EqualTo(-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Identity, -point_seven), Is.EqualTo(-point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_Identity, -point_sevenXY), Is.EqualTo(-point_sevenXY).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Identity.Value, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Identity.Value, point_one), Is.EqualTo(point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Identity.Value, point_seven), Is.EqualTo(point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_Identity.Value, point_sevenXY), Is.EqualTo(point_sevenXY).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Identity.Value, -point_one), Is.EqualTo(-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Identity.Value, -point_seven), Is.EqualTo(-point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_Identity.Value, -point_sevenXY), Is.EqualTo(-point_sevenXY).Using<float3>(EqualityWithTolerance));
        }

        [Test]
        public static void ConvertLocalToWorldPointTest_Translate()
        {
            float3 point_zero = float3.zero;
            float3 point_one = new float3(1, 1, 1);
            float3 point_seven = point_one * 7f;
            float3 point_oneXY = new float3(1, 1, 0);
            float3 point_sevenXY = point_oneXY * 7f;


            LocalToWorld localToWorld_TranslatedOne = new LocalToWorld()
            {
                Value = float4x4.TRS(point_one, quaternion.identity, point_one)
            };
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedOne, point_zero), Is.EqualTo(point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedOne, point_one), Is.EqualTo(point_one+point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedOne, point_seven), Is.EqualTo(point_seven+point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedOne, point_sevenXY), Is.EqualTo(point_sevenXY+point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedOne, -point_one), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedOne, -point_seven), Is.EqualTo(-point_seven+point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedOne, -point_sevenXY), Is.EqualTo(-point_sevenXY+point_one).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedOne.Value, point_zero), Is.EqualTo(point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedOne.Value, point_one), Is.EqualTo(point_one+point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedOne.Value, point_seven), Is.EqualTo(point_seven+point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedOne.Value, point_sevenXY), Is.EqualTo(point_sevenXY+point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedOne.Value, -point_one), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedOne.Value, -point_seven), Is.EqualTo(-point_seven+point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedOne.Value, -point_sevenXY), Is.EqualTo(-point_sevenXY+point_one).Using<float3>(EqualityWithTolerance));


            LocalToWorld localToWorld_TranslatedNegativeOne = new LocalToWorld()
            {
                Value = float4x4.TRS(-point_one, quaternion.identity, point_one)
            };
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedNegativeOne, point_zero), Is.EqualTo(-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedNegativeOne, point_one), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedNegativeOne, point_seven), Is.EqualTo(point_seven-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedNegativeOne, point_sevenXY), Is.EqualTo(point_sevenXY-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedNegativeOne, -point_one), Is.EqualTo(-point_one-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedNegativeOne, -point_seven), Is.EqualTo(-point_seven-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedNegativeOne, -point_sevenXY), Is.EqualTo(-point_sevenXY-point_one).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedNegativeOne.Value, point_zero), Is.EqualTo(-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedNegativeOne.Value, point_one), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedNegativeOne.Value, point_seven), Is.EqualTo(point_seven-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedNegativeOne.Value, point_sevenXY), Is.EqualTo(point_sevenXY-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedNegativeOne.Value, -point_one), Is.EqualTo(-point_one-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedNegativeOne.Value, -point_seven), Is.EqualTo(-point_seven-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedNegativeOne.Value, -point_sevenXY), Is.EqualTo(-point_sevenXY-point_one).Using<float3>(EqualityWithTolerance));


            LocalToWorld localToWorld_TranslatedSeven = new LocalToWorld()
            {
                Value = float4x4.TRS(point_seven, quaternion.identity, point_one)
            };
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedSeven, point_zero), Is.EqualTo(point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedSeven, point_one), Is.EqualTo(point_seven+point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedSeven, point_seven), Is.EqualTo(point_seven+point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedSeven, point_sevenXY), Is.EqualTo(point_sevenXY+point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedSeven, -point_one), Is.EqualTo(point_seven-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedSeven, -point_seven), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedSeven, -point_sevenXY), Is.EqualTo(-point_sevenXY+point_seven).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedSeven.Value, point_zero), Is.EqualTo(point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedSeven.Value, point_one), Is.EqualTo(point_seven+point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedSeven.Value, point_seven), Is.EqualTo(point_seven+point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedSeven.Value, point_sevenXY), Is.EqualTo(point_sevenXY+point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedSeven.Value, -point_one), Is.EqualTo(point_seven-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedSeven.Value, -point_seven), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalPoint(localToWorld_TranslatedSeven.Value, -point_sevenXY), Is.EqualTo(-point_sevenXY+point_seven).Using<float3>(EqualityWithTolerance));


            LocalToWorld localToWorld_TranslatedNegativeSeven = new LocalToWorld()
            {
                Value = float4x4.TRS(-point_seven, quaternion.identity, point_one)
            };
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedNegativeSeven, point_zero), Is.EqualTo(-point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedNegativeSeven, point_one), Is.EqualTo(-point_seven+point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedNegativeSeven, point_seven), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedNegativeSeven, point_sevenXY), Is.EqualTo(point_sevenXY-point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedNegativeSeven, -point_one), Is.EqualTo(-point_seven-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedNegativeSeven, -point_seven), Is.EqualTo(-point_seven-point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedNegativeSeven, -point_sevenXY), Is.EqualTo(-point_sevenXY-point_seven).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedNegativeSeven.Value, point_zero), Is.EqualTo(-point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedNegativeSeven.Value, point_one), Is.EqualTo(-point_seven+point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedNegativeSeven.Value, point_seven), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedNegativeSeven.Value, point_sevenXY), Is.EqualTo(point_sevenXY-point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedNegativeSeven.Value, -point_one), Is.EqualTo(-point_seven-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedNegativeSeven.Value, -point_seven), Is.EqualTo(-point_seven-point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedNegativeSeven.Value, -point_sevenXY), Is.EqualTo(-point_sevenXY-point_seven).Using<float3>(EqualityWithTolerance));


            LocalToWorld localToWorld_TranslatedSevenXY = new LocalToWorld()
            {
                Value = float4x4.TRS(point_sevenXY, quaternion.identity, point_one)
            };
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedSevenXY, point_zero), Is.EqualTo(point_sevenXY).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedSevenXY, point_one), Is.EqualTo(point_one+point_sevenXY).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedSevenXY, point_seven), Is.EqualTo(point_seven+point_sevenXY).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedSevenXY, point_sevenXY), Is.EqualTo(point_sevenXY+point_sevenXY).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedSevenXY, -point_one), Is.EqualTo(point_sevenXY-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedSevenXY, -point_seven), Is.EqualTo(point_sevenXY-point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedSevenXY, -point_sevenXY), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedSevenXY.Value, point_zero), Is.EqualTo(point_sevenXY).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedSevenXY.Value, point_one), Is.EqualTo(point_one+point_sevenXY).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedSevenXY.Value, point_seven), Is.EqualTo(point_seven+point_sevenXY).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedSevenXY.Value, point_sevenXY), Is.EqualTo(point_sevenXY+point_sevenXY).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedSevenXY.Value, -point_one), Is.EqualTo(point_sevenXY-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedSevenXY.Value, -point_seven), Is.EqualTo(point_sevenXY-point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_TranslatedSevenXY.Value, -point_sevenXY), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
        }

        [Test]
        public static void ConvertLocalToWorldPointTest_Rotate()
        {
            float3 point_zero = float3.zero;
            float3 point_one = new float3(1, 1, 1);
            float3 point_seven = point_one * 7f;

            LocalToWorld localToWorld_RotatedZ90 = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.Euler(0, 0, math.radians(90)), point_one)
            };
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZ90, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZ90, point_one), Is.EqualTo(new float3(-1f, 1f, 1f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZ90, point_seven), Is.EqualTo(new float3(-7f, 7f, 7f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZ90, -point_one), Is.EqualTo(new float3(1f, -1f, -1f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZ90, -point_seven), Is.EqualTo(new float3(7f, -7f, -7f)).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZ90.Value, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZ90.Value, point_one), Is.EqualTo(new float3(-1f, 1f, 1f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZ90.Value, point_seven), Is.EqualTo(new float3(-7f, 7f, 7f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZ90.Value, -point_one), Is.EqualTo(new float3(1f, -1f, -1f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZ90.Value, -point_seven), Is.EqualTo(new float3(7f, -7f, -7f)).Using<float3>(EqualityWithTolerance));


            LocalToWorld localToWorld_RotatedZNegative90 = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.Euler(0, 0, math.radians(-90)), point_one)
            };
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZNegative90, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZNegative90, point_one), Is.EqualTo(new float3(1f, -1f, 1f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZNegative90, point_seven), Is.EqualTo(new float3(7f, -7f, 7f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZNegative90, -point_one), Is.EqualTo(new float3(-1f, 1f, -1f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZNegative90, -point_seven), Is.EqualTo(new float3(-7f, 7f, -7f)).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZNegative90.Value, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZNegative90.Value, point_one), Is.EqualTo(new float3(1f, -1f, 1f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZNegative90.Value, point_seven), Is.EqualTo(new float3(7f, -7f, 7f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZNegative90.Value, -point_one), Is.EqualTo(new float3(-1f, 1f, -1f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZNegative90.Value, -point_seven), Is.EqualTo(new float3(-7f, 7f, -7f)).Using<float3>(EqualityWithTolerance));


            LocalToWorld localToWorld_RotatedZX90 = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.Euler(math.radians(90), 0, math.radians(90)), point_one)
            };
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZX90, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZX90, point_one), Is.EqualTo(new float3(-1f, -1f, 1f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZX90, point_seven), Is.EqualTo(new float3(-7f, -7f, 7f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZX90, -point_one), Is.EqualTo(new float3(1f, 1f, -1f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZX90, -point_seven), Is.EqualTo(new float3(7f, 7f, -7f)).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZX90.Value, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZX90.Value, point_one), Is.EqualTo(new float3(-1f, -1f, 1f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZX90.Value, point_seven), Is.EqualTo(new float3(-7f, -7f, 7f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZX90.Value, -point_one), Is.EqualTo(new float3(1f, 1f, -1f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZX90.Value, -point_seven), Is.EqualTo(new float3(7f, 7f, -7f)).Using<float3>(EqualityWithTolerance));


            LocalToWorld localToWorld_RotatedZXNegative90 = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.Euler(math.radians(-90), 0, math.radians(-90)), point_one)
            };
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZXNegative90, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZXNegative90, point_one), Is.EqualTo(point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZXNegative90, point_seven), Is.EqualTo(point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZXNegative90, -point_one), Is.EqualTo(-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZXNegative90, -point_seven), Is.EqualTo(-point_seven).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZXNegative90.Value, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZXNegative90.Value, point_one), Is.EqualTo(point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZXNegative90.Value, point_seven), Is.EqualTo(point_seven).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZXNegative90.Value, -point_one), Is.EqualTo(-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZXNegative90.Value, -point_seven), Is.EqualTo(-point_seven).Using<float3>(EqualityWithTolerance));


            LocalToWorld localToWorld_RotatedZX45 = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.Euler(math.radians(45), 0, math.radians(45)), point_one)
            };
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZX45, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZX45, point_one), Is.EqualTo(new float3(0f,0.292893201f,1.70710683f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZX45, point_seven), Is.EqualTo(new float3(0f,2.0502522f,11.9497471f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZX45, -point_one), Is.EqualTo(new float3(0f,-0.292893201f,-1.70710683f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZX45, -point_seven), Is.EqualTo(new float3(0f,-2.0502522f,-11.9497471f)).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZX45.Value, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZX45.Value, point_one), Is.EqualTo(new float3(0f,0.292893201f,1.70710683f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZX45.Value, point_seven), Is.EqualTo(new float3(0f,2.0502522f,11.9497471f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZX45.Value, -point_one), Is.EqualTo(new float3(0f,-0.292893201f,-1.70710683f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_RotatedZX45.Value, -point_seven), Is.EqualTo(new float3(0f,-2.0502522f,-11.9497471f)).Using<float3>(EqualityWithTolerance));
        }

        [Test]
        public static void ConvertLocalToWorldPointTest_Scale()
        {
            Regex invalidMatrixError_point = new Regex(@"This transform is invalid\. Returning a signed infinite position\.");

            float3 point_infinity = new float3(float.PositiveInfinity);
            float3 point_zero = float3.zero;
            float3 point_one = new float3(1, 1, 1);
            float3 point_seven = point_one * 7f;

            LocalToWorld localToWorld_Scaled2 = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.identity, point_one*2f)
            };
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Scaled2, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Scaled2, point_one), Is.EqualTo(point_one*2f).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Scaled2, point_seven), Is.EqualTo(point_seven*2f).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Scaled2, -point_one), Is.EqualTo(-point_one*2f).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Scaled2, -point_seven), Is.EqualTo(-point_seven*2f).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Scaled2.Value, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Scaled2.Value, point_one), Is.EqualTo(point_one*2f).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Scaled2.Value, point_seven), Is.EqualTo(point_seven*2f).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Scaled2.Value, -point_one), Is.EqualTo(-point_one*2f).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Scaled2.Value, -point_seven), Is.EqualTo(-point_seven*2f).Using<float3>(EqualityWithTolerance));


            LocalToWorld localToWorld_ScaledNegative2 = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.identity, point_one*-2f)
            };
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledNegative2, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledNegative2, point_one), Is.EqualTo(-point_one*2f).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledNegative2, point_seven), Is.EqualTo(-point_seven*2f).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledNegative2, -point_one), Is.EqualTo(point_one*2f).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledNegative2, -point_seven), Is.EqualTo(point_seven*2f).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledNegative2.Value, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledNegative2.Value, point_one), Is.EqualTo(-point_one*2f).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledNegative2.Value, point_seven), Is.EqualTo(-point_seven*2f).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledNegative2.Value, -point_one), Is.EqualTo(point_one*2f).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledNegative2.Value, -point_seven), Is.EqualTo(point_seven*2f).Using<float3>(EqualityWithTolerance));


            float3 scaleZ2 = new float3(point_one.xy, 2f);
            LocalToWorld localToWorld_ScaledZ2 = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.identity, scaleZ2)
            };
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZ2, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZ2, point_one), Is.EqualTo(point_one*scaleZ2).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZ2, point_seven), Is.EqualTo(point_seven*scaleZ2).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZ2, -point_one), Is.EqualTo(-point_one*scaleZ2).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZ2, -point_seven), Is.EqualTo(-point_seven*scaleZ2).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZ2.Value, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZ2.Value, point_one), Is.EqualTo(point_one*scaleZ2).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZ2.Value, point_seven), Is.EqualTo(point_seven*scaleZ2).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZ2.Value, -point_one), Is.EqualTo(-point_one*scaleZ2).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZ2.Value, -point_seven), Is.EqualTo(-point_seven*scaleZ2).Using<float3>(EqualityWithTolerance));


            float3 scaleZNegative2 = new float3(point_one.xy, -2f);
            LocalToWorld localToWorld_ScaledZNegative2 = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.identity, scaleZNegative2)
            };
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZNegative2, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZNegative2, point_one), Is.EqualTo(point_one*scaleZNegative2).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZNegative2, point_seven), Is.EqualTo(point_seven*scaleZNegative2).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZNegative2, -point_one), Is.EqualTo(-point_one*scaleZNegative2).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZNegative2, -point_seven), Is.EqualTo(-point_seven*scaleZNegative2).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZNegative2.Value, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZNegative2.Value, point_one), Is.EqualTo(point_one*scaleZNegative2).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZNegative2.Value, point_seven), Is.EqualTo(point_seven*scaleZNegative2).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZNegative2.Value, -point_one), Is.EqualTo(-point_one*scaleZNegative2).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZNegative2.Value, -point_seven), Is.EqualTo(-point_seven*scaleZNegative2).Using<float3>(EqualityWithTolerance));


            float3 scaleZXOnePointFive = new float3(1.5f, 1f, 1.5f);
            LocalToWorld localToWorld_ScaledZXOnePointFive = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.identity, scaleZXOnePointFive)
            };
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZXOnePointFive, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZXOnePointFive, point_one), Is.EqualTo(point_one*scaleZXOnePointFive).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZXOnePointFive, point_seven), Is.EqualTo(point_seven*scaleZXOnePointFive).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZXOnePointFive, -point_one), Is.EqualTo(-point_one*scaleZXOnePointFive).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZXOnePointFive, -point_seven), Is.EqualTo(-point_seven*scaleZXOnePointFive).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZXOnePointFive.Value, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZXOnePointFive.Value, point_one), Is.EqualTo(point_one*scaleZXOnePointFive).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZXOnePointFive.Value, point_seven), Is.EqualTo(point_seven*scaleZXOnePointFive).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZXOnePointFive.Value, -point_one), Is.EqualTo(-point_one*scaleZXOnePointFive).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_ScaledZXOnePointFive.Value, -point_seven), Is.EqualTo(-point_seven*scaleZXOnePointFive).Using<float3>(EqualityWithTolerance));


            LocalToWorld localToWorld_zero = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.identity, point_zero)
            };

            Logger.Debug("BEGIN: Expected error messages.");
            LogAssert.Expect(LogType.Error, invalidMatrixError_point);
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_zero, point_zero), Is.EqualTo(point_zero));
            LogAssert.Expect(LogType.Error, invalidMatrixError_point);
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_zero, point_one), Is.EqualTo(point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_point);
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_zero, point_seven), Is.EqualTo(point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_point);
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_zero, -point_one), Is.EqualTo(-point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_point);
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_zero, -point_seven), Is.EqualTo(-point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_point);
            Assert.That(
                TransformUtil.ConvertLocalToWorldPoint(localToWorld_zero, new float3(2, -2, 0)),
                Is.EqualTo(new float3(float.PositiveInfinity, float.NegativeInfinity, 0))
                );

            LogAssert.Expect(LogType.Error, invalidMatrixError_point);
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_zero.Value, point_zero), Is.EqualTo(point_zero));
            LogAssert.Expect(LogType.Error, invalidMatrixError_point);
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_zero.Value, point_one), Is.EqualTo(point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_point);
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_zero.Value, point_seven), Is.EqualTo(point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_point);
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_zero.Value, -point_one), Is.EqualTo(-point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_point);
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_zero.Value, -point_seven), Is.EqualTo(-point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_point);
            Assert.That(
                TransformUtil.ConvertLocalToWorldPoint(localToWorld_zero.Value, new float3(2, -2, 0)),
                Is.EqualTo(new float3(float.PositiveInfinity, float.NegativeInfinity, 0))
            );
            Logger.Debug("END: Expected error messages.");
        }

        [Test]
        public static void ConvertLocalToWorldPointTest_Compound()
        {
            float3 point_zero = float3.zero;
            float3 point_one = new float3(1f, 1f, 1f);
            float3 point_seven = point_one * 7f;
            float3 point_sevenXY = new float3(point_seven.xy, 0f);

            LocalToWorld localToWorld_Compound = new LocalToWorld()
            {
                Value = float4x4.TRS(point_sevenXY, quaternion.Euler(math.radians(45), 0, math.radians(45)), point_one*2f)
            };
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Compound, point_zero), Is.EqualTo(point_sevenXY).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Compound, point_one), Is.EqualTo(new float3(7f, 7.58578634f, 3.41421366f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Compound, point_seven), Is.EqualTo(new float3(7f, 11.1005039f, 23.8994942f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Compound, -point_one), Is.EqualTo(new float3(7f,6.41421366f, -3.41421366f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Compound, -point_seven), Is.EqualTo(new float3(7f, 2.89949608f, -23.8994942f)).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Compound.Value, point_zero), Is.EqualTo(point_sevenXY).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Compound.Value, point_one), Is.EqualTo(new float3(7 ,7.58578634f, 3.41421366f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Compound.Value, point_seven), Is.EqualTo(new float3(7 ,11.1005039f, 23.8994942f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Compound.Value, -point_one), Is.EqualTo(new float3(7 ,6.41421366f, -3.41421366f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_Compound.Value, -point_seven), Is.EqualTo(new float3(7 ,2.89949608f, -23.8994942f)).Using<float3>(EqualityWithTolerance));


            LocalToWorld localToWorld_CompoundNegative = new LocalToWorld()
            {
                Value = float4x4.TRS(-point_sevenXY, quaternion.Euler(math.radians(-45), 0, math.radians(-45)), point_one*-2f)
            };
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_CompoundNegative, point_zero), Is.EqualTo(-point_sevenXY).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_CompoundNegative, point_one), Is.EqualTo(new float3(-9.82842731f, -8.41421318f, -1.41421366f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_CompoundNegative, point_seven), Is.EqualTo(new float3(-26.7989922f, -16.8994961f, -9.89949512f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_CompoundNegative, -point_one), Is.EqualTo(new float3(-4.17157269f, -5.58578634f, 1.41421366f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_CompoundNegative, -point_seven), Is.EqualTo(new float3(12.7989922f, 2.89949608f, 9.89949512f)).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_CompoundNegative.Value, point_zero), Is.EqualTo(-point_sevenXY).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_CompoundNegative.Value, point_one), Is.EqualTo(new float3(-9.82842731f, -8.41421318f, -1.41421366f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_CompoundNegative.Value, point_seven), Is.EqualTo(new float3(-26.7989922f, -16.8994961f, -9.89949512f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_CompoundNegative.Value, -point_one), Is.EqualTo(new float3(-4.17157269f, -5.58578634f, 1.41421366f)).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldPoint(localToWorld_CompoundNegative.Value, -point_seven), Is.EqualTo(new float3(12.7989922f, 2.89949608f, 9.89949512f)).Using<float3>(EqualityWithTolerance));
        }

        // ----- ConvertWorldToLocalRotation ----- //
        [Test]
        public static void ConvertWorldToLocalRotationTest_Rotate()
        {
            float3 point_zero = float3.zero;
            float3 point_one = new float3(1f, 1f, 1f);

            quaternion rotation_fortyFive = quaternion.Euler(math.radians(45f), math.radians(45f), math.radians(45f));
            quaternion rotation_fortyFive_inverse = math.inverse(rotation_fortyFive);

            quaternion rotation_ninety = quaternion.Euler(math.radians(90f), math.radians(90f), math.radians(90f));
            quaternion rotation_ninety_inverse = math.inverse(rotation_ninety);

            quaternion rotation_ZfortyFive = quaternion.Euler(0f, 0f, math.radians(45f));
            quaternion rotation_ZfortyFive_inverse = math.inverse(rotation_ZfortyFive);

            quaternion rotation_Zninety = quaternion.Euler(0f, 0f, math.radians(90f));
            quaternion rotation_Zninety_inverse = math.inverse(rotation_Zninety);


            LocalToWorld localToWorld_ninety = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, rotation_ninety, point_one)
            };
            float4x4 worldToLocal_ninety = math.inverse(localToWorld_ninety.Value);
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(localToWorld_ninety, quaternion.identity), Is.EqualTo(math.mul(rotation_ninety_inverse, quaternion.identity)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(localToWorld_ninety, rotation_ninety), Is.EqualTo(math.mul(rotation_ninety_inverse, rotation_ninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(localToWorld_ninety, rotation_Zninety), Is.EqualTo(math.mul(rotation_ninety_inverse, rotation_Zninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(localToWorld_ninety, rotation_fortyFive), Is.EqualTo(math.mul(rotation_ninety_inverse, rotation_fortyFive)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(localToWorld_ninety, rotation_ZfortyFive), Is.EqualTo(math.mul(rotation_ninety_inverse, rotation_ZfortyFive)).Using<quaternion>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_ninety, quaternion.identity), Is.EqualTo(math.mul(rotation_ninety_inverse, quaternion.identity)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_ninety, rotation_ninety), Is.EqualTo(math.mul(rotation_ninety_inverse, rotation_ninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_ninety, rotation_Zninety), Is.EqualTo(math.mul(rotation_ninety_inverse, rotation_Zninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_ninety, rotation_fortyFive), Is.EqualTo(math.mul(rotation_ninety_inverse, rotation_fortyFive)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_ninety, rotation_ZfortyFive), Is.EqualTo(math.mul(rotation_ninety_inverse, rotation_ZfortyFive)).Using<quaternion>(EqualityWithTolerance));


            LocalToWorld localToWorld_fortyFive = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, rotation_fortyFive, point_one)
            };
            float4x4 worldToLocal_fortyFive = math.inverse(localToWorld_fortyFive.Value);
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(localToWorld_fortyFive, quaternion.identity), Is.EqualTo(math.mul(rotation_fortyFive_inverse, quaternion.identity)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(localToWorld_fortyFive, rotation_ninety), Is.EqualTo(math.mul(rotation_fortyFive_inverse, rotation_ninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(localToWorld_fortyFive, rotation_Zninety), Is.EqualTo(math.mul(rotation_fortyFive_inverse, rotation_Zninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(localToWorld_fortyFive, rotation_fortyFive), Is.EqualTo(math.mul(rotation_fortyFive_inverse, rotation_fortyFive)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(localToWorld_fortyFive, rotation_ZfortyFive), Is.EqualTo(math.mul(rotation_fortyFive_inverse, rotation_ZfortyFive)).Using<quaternion>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_fortyFive, quaternion.identity), Is.EqualTo(math.mul(rotation_fortyFive_inverse, quaternion.identity)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_fortyFive, rotation_ninety), Is.EqualTo(math.mul(rotation_fortyFive_inverse, rotation_ninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_fortyFive, rotation_Zninety), Is.EqualTo(math.mul(rotation_fortyFive_inverse, rotation_Zninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_fortyFive, rotation_fortyFive), Is.EqualTo(math.mul(rotation_fortyFive_inverse, rotation_fortyFive)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_fortyFive, rotation_ZfortyFive), Is.EqualTo(math.mul(rotation_fortyFive_inverse, rotation_ZfortyFive)).Using<quaternion>(EqualityWithTolerance));


            LocalToWorld localToWorld_Zninety = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, rotation_Zninety, point_one)
            };
            float4x4 worldToLocal_Zninety = math.inverse(localToWorld_Zninety.Value);
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(localToWorld_Zninety, quaternion.identity), Is.EqualTo(math.mul(rotation_Zninety_inverse, quaternion.identity)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(localToWorld_Zninety, rotation_ninety), Is.EqualTo(math.mul(rotation_Zninety_inverse, rotation_ninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(localToWorld_Zninety, rotation_Zninety), Is.EqualTo(math.mul(rotation_Zninety_inverse, rotation_Zninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(localToWorld_Zninety, rotation_fortyFive), Is.EqualTo(math.mul(rotation_Zninety_inverse, rotation_fortyFive)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(localToWorld_Zninety, rotation_ZfortyFive), Is.EqualTo(math.mul(rotation_Zninety_inverse, rotation_ZfortyFive)).Using<quaternion>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_Zninety, quaternion.identity), Is.EqualTo(math.mul(rotation_Zninety_inverse, quaternion.identity)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_Zninety, rotation_ninety), Is.EqualTo(math.mul(rotation_Zninety_inverse, rotation_ninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_Zninety, rotation_Zninety), Is.EqualTo(math.mul(rotation_Zninety_inverse, rotation_Zninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_Zninety, rotation_fortyFive), Is.EqualTo(math.mul(rotation_Zninety_inverse, rotation_fortyFive)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_Zninety, rotation_ZfortyFive), Is.EqualTo(math.mul(rotation_Zninety_inverse, rotation_ZfortyFive)).Using<quaternion>(EqualityWithTolerance));


            LocalToWorld localToWorld_ZfortyFive = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, rotation_ZfortyFive, point_one)
            };
            float4x4 worldToLocal_ZfortyFive = math.inverse(localToWorld_ZfortyFive.Value);
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(localToWorld_ZfortyFive, quaternion.identity), Is.EqualTo(math.mul(rotation_ZfortyFive_inverse, quaternion.identity)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(localToWorld_ZfortyFive, rotation_ninety), Is.EqualTo(math.mul(rotation_ZfortyFive_inverse, rotation_ninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(localToWorld_ZfortyFive, rotation_Zninety), Is.EqualTo(math.mul(rotation_ZfortyFive_inverse, rotation_Zninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(localToWorld_ZfortyFive, rotation_fortyFive), Is.EqualTo(math.mul(rotation_ZfortyFive_inverse, rotation_fortyFive)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(localToWorld_ZfortyFive, rotation_ZfortyFive), Is.EqualTo(math.mul(rotation_ZfortyFive_inverse, rotation_ZfortyFive)).Using<quaternion>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_ZfortyFive, quaternion.identity), Is.EqualTo(math.mul(rotation_ZfortyFive_inverse, quaternion.identity)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_ZfortyFive, rotation_ninety), Is.EqualTo(math.mul(rotation_ZfortyFive_inverse, rotation_ninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_ZfortyFive, rotation_Zninety), Is.EqualTo(math.mul(rotation_ZfortyFive_inverse, rotation_Zninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_ZfortyFive, rotation_fortyFive), Is.EqualTo(math.mul(rotation_ZfortyFive_inverse, rotation_fortyFive)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_ZfortyFive, rotation_ZfortyFive), Is.EqualTo(math.mul(rotation_ZfortyFive_inverse, rotation_ZfortyFive)).Using<quaternion>(EqualityWithTolerance));
        }

        [Test]
        public static void ConvertWorldToLocalRotationTest_Compound()
        {
            Regex invalidMatrixError_rotate = new Regex(@"Transform is not valid\. Returning identity rotation\.");

            float3 point_zero = float3.zero;
            float3 point_one = new float3(1f, 1f, 1f);
            float3 point_sevenXY = new float3(7f, 7f, 0f);
            quaternion rotation_fortyFive = quaternion.Euler(math.radians(45f), math.radians(45f), math.radians(45f));
            quaternion rotation_ninety = quaternion.Euler(math.radians(90f), math.radians(90f), math.radians(90f));
            quaternion rotation_ZfortyFive = quaternion.Euler(0f, 0f, math.radians(45f));
            quaternion rotation_Zninety = quaternion.Euler(0f, 0f, math.radians(90f));

            quaternion rotation_XZ_fortyFive = quaternion.Euler(math.radians(45), 0, math.radians(45));
            quaternion rotation_XZ_fortyFive_inverse = math.inverse(rotation_XZ_fortyFive);
            quaternion rotation_XZ_negativeFortyFive = quaternion.Euler(math.radians(-45), 0, math.radians(-45));
            quaternion rotation_XZ_negativeFortyFive_inverse = math.inverse(rotation_XZ_negativeFortyFive);


            LocalToWorld localToWorld_compound = new LocalToWorld()
            {
                Value = float4x4.TRS(point_sevenXY, rotation_XZ_fortyFive, point_one*2f)
            };
            float4x4 worldToLocal_compound = math.inverse(localToWorld_compound.Value);

            Assert.That(
                TransformUtil.ConvertWorldToLocalRotation(localToWorld_compound, quaternion.identity),
                Is.EqualTo(math.mul(rotation_XZ_fortyFive_inverse, quaternion.identity)).Using<quaternion>(EqualityWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRotation(localToWorld_compound, rotation_ninety),
                Is.EqualTo(math.mul(rotation_XZ_fortyFive_inverse, rotation_ninety)).Using<quaternion>(EqualityWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRotation(localToWorld_compound, rotation_Zninety),
                Is.EqualTo(math.mul(rotation_XZ_fortyFive_inverse, rotation_Zninety)).Using<quaternion>(EqualityWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRotation(localToWorld_compound, rotation_fortyFive),
                Is.EqualTo(math.mul(rotation_XZ_fortyFive_inverse, rotation_fortyFive)).Using<quaternion>(EqualityWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRotation(localToWorld_compound, rotation_ZfortyFive),
                Is.EqualTo(math.mul(rotation_XZ_fortyFive_inverse, rotation_ZfortyFive)).Using<quaternion>(EqualityWithTolerance)
                );

            Assert.That(
                TransformUtil.ConvertWorldToLocalRotation(worldToLocal_compound, quaternion.identity),
                Is.EqualTo(math.mul(rotation_XZ_fortyFive_inverse, quaternion.identity)).Using<quaternion>(EqualityWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRotation(worldToLocal_compound, rotation_ninety),
                Is.EqualTo(math.mul(rotation_XZ_fortyFive_inverse, rotation_ninety)).Using<quaternion>(EqualityWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRotation(worldToLocal_compound, rotation_Zninety),
                Is.EqualTo(math.mul(rotation_XZ_fortyFive_inverse, rotation_Zninety)).Using<quaternion>(EqualityWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRotation(worldToLocal_compound, rotation_fortyFive),
                Is.EqualTo(math.mul(rotation_XZ_fortyFive_inverse, rotation_fortyFive)).Using<quaternion>(EqualityWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRotation(worldToLocal_compound, rotation_ZfortyFive),
                Is.EqualTo(math.mul(rotation_XZ_fortyFive_inverse, rotation_ZfortyFive)).Using<quaternion>(EqualityWithTolerance)
                );


            LocalToWorld localToWorld_compound_negativeZ = new LocalToWorld()
            {
                Value = float4x4.TRS(-point_sevenXY, rotation_XZ_negativeFortyFive, new float3(2f, 2f, -2f))
            };
            float4x4 worldToLocal_compound_negativeZ = math.inverse(localToWorld_compound_negativeZ.Value);
            float3x3 worldToLocal_compound_negativeZScale = float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(worldToLocal_compound_negativeZ, new float3(1f)));

            Assert.That(
                math.mul(new float3x3(TransformUtil.ConvertWorldToLocalRotation(localToWorld_compound_negativeZ, quaternion.identity)), worldToLocal_compound_negativeZScale),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeZ, new float3x3(quaternion.identity))).Using<float3x3>(EqualityWithTolerance)
                );
            Assert.That(
                math.mul(new float3x3(TransformUtil.ConvertWorldToLocalRotation(localToWorld_compound_negativeZ, rotation_ninety)), worldToLocal_compound_negativeZScale),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeZ, new float3x3(rotation_ninety))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(new float3x3(TransformUtil.ConvertWorldToLocalRotation(localToWorld_compound_negativeZ, rotation_Zninety)), worldToLocal_compound_negativeZScale),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeZ, new float3x3(rotation_Zninety))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(new float3x3(TransformUtil.ConvertWorldToLocalRotation(localToWorld_compound_negativeZ, rotation_fortyFive)), worldToLocal_compound_negativeZScale),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeZ, new float3x3(rotation_fortyFive))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(new float3x3(TransformUtil.ConvertWorldToLocalRotation(localToWorld_compound_negativeZ, rotation_ZfortyFive)), worldToLocal_compound_negativeZScale),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeZ, new float3x3(rotation_ZfortyFive))).Using<float3x3>(EqualityWithTolerance)
            );

            Assert.That(
                math.mul(new float3x3(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_compound_negativeZ, quaternion.identity)), worldToLocal_compound_negativeZScale),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeZ, new float3x3(quaternion.identity))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(new float3x3(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_compound_negativeZ, rotation_ninety)), worldToLocal_compound_negativeZScale),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeZ, new float3x3(rotation_ninety))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(new float3x3(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_compound_negativeZ, rotation_Zninety)), worldToLocal_compound_negativeZScale),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeZ, new float3x3(rotation_Zninety))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(new float3x3(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_compound_negativeZ, rotation_fortyFive)), worldToLocal_compound_negativeZScale),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeZ, new float3x3(rotation_fortyFive))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(new float3x3(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_compound_negativeZ, rotation_ZfortyFive)), worldToLocal_compound_negativeZScale),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeZ, new float3x3(rotation_ZfortyFive))).Using<float3x3>(EqualityWithTolerance)
            );


            LocalToWorld localToWorld_compound_zeroScale = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, rotation_ZfortyFive, point_zero)
            };
            float4x4 worldToLocal_compound_zeroScale =
                float4x4.TRS(point_zero, Quaternion.Inverse(rotation_ZfortyFive), point_zero);

            Logger.Debug("BEGIN: Expected error messages.");
            LogAssert.Expect(LogType.Error, invalidMatrixError_rotate);
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(localToWorld_compound_zeroScale, quaternion.identity), Is.EqualTo(quaternion.identity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_rotate);
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(localToWorld_compound_zeroScale, rotation_ninety), Is.EqualTo(quaternion.identity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_rotate);
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(localToWorld_compound_zeroScale, rotation_Zninety), Is.EqualTo(quaternion.identity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_rotate);
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(localToWorld_compound_zeroScale, rotation_fortyFive), Is.EqualTo(quaternion.identity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_rotate);
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(localToWorld_compound_zeroScale, rotation_ZfortyFive), Is.EqualTo(quaternion.identity));

            LogAssert.Expect(LogType.Error, invalidMatrixError_rotate);
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_compound_zeroScale, quaternion.identity), Is.EqualTo(quaternion.identity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_rotate);
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_compound_zeroScale, rotation_ninety), Is.EqualTo(quaternion.identity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_rotate);
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_compound_zeroScale, rotation_Zninety), Is.EqualTo(quaternion.identity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_rotate);
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_compound_zeroScale, rotation_fortyFive), Is.EqualTo(quaternion.identity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_rotate);
            Assert.That(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_compound_zeroScale, rotation_ZfortyFive), Is.EqualTo(quaternion.identity));
            Logger.Debug("END: Expected error messages.");


            //TODO: #116 - Transforms with non-uniform scale operations are not currently supported.
            // Tests with assertions commented out below would replace these tests.
            LocalToWorld localToWorld_compound_negativeYZ = new LocalToWorld()
            {
                Value = float4x4.TRS(-point_sevenXY, quaternion.identity, new float3(1f, -2f, -1.5f))
            };
            float4x4 worldToLocal_compound_negativeYZ = math.inverse(localToWorld_compound_negativeYZ.Value);

#if DEBUG
            Logger.Debug("BEGIN: Expected error messages.");
            // The assert ensures the utility method emits a log message when a non-uniform transform is provided.
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertWorldToLocalRotation(localToWorld_compound_negativeYZ, quaternion.identity);

            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertWorldToLocalRotation(localToWorld_compound_negativeYZ, rotation_ninety);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertWorldToLocalRotation(localToWorld_compound_negativeYZ, rotation_Zninety);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertWorldToLocalRotation(localToWorld_compound_negativeYZ, rotation_fortyFive);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertWorldToLocalRotation(localToWorld_compound_negativeYZ, rotation_ZfortyFive);

            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertWorldToLocalRotation(worldToLocal_compound_negativeYZ, quaternion.identity);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertWorldToLocalRotation(worldToLocal_compound_negativeYZ, rotation_ninety);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertWorldToLocalRotation(worldToLocal_compound_negativeYZ, rotation_Zninety);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertWorldToLocalRotation(worldToLocal_compound_negativeYZ, rotation_fortyFive);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertWorldToLocalRotation(worldToLocal_compound_negativeYZ, rotation_ZfortyFive);
            Logger.Debug("END: Expected error messages.");
#endif

            // // These follow the template of the asserts for uniform scale transforms. They may or may not result in correct values for
            // // non-uniform scaling.
            // float3x3 worldToLocal_compound_negativeYZScale = float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(worldToLocal_compound_negativeYZ, new float3(1f)));
            // Assert.That(
            //     math.mul(new float3x3(TransformUtil.ConvertWorldToLocalRotation(localToWorld_compound_negativeYZ, quaternion.identity)), worldToLocal_compound_negativeYZScale),
            //     Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeYZ, new float3x3(quaternion.identity))).Using<float3x3>(EqualityWithTolerance)
            //     );
            // Assert.That(
            //     math.mul(new float3x3(TransformUtil.ConvertWorldToLocalRotation(localToWorld_compound_negativeYZ, rotation_ninety)), worldToLocal_compound_negativeYZScale),
            //     Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeYZ, new float3x3(rotation_ninety))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(new float3x3(TransformUtil.ConvertWorldToLocalRotation(localToWorld_compound_negativeYZ, rotation_Zninety)), worldToLocal_compound_negativeYZScale),
            //     Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeYZ, new float3x3(rotation_Zninety))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(new float3x3(TransformUtil.ConvertWorldToLocalRotation(localToWorld_compound_negativeYZ, rotation_fortyFive)), worldToLocal_compound_negativeYZScale),
            //     Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeYZ, new float3x3(rotation_fortyFive))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(new float3x3(TransformUtil.ConvertWorldToLocalRotation(localToWorld_compound_negativeYZ, rotation_ZfortyFive)), worldToLocal_compound_negativeYZScale),
            //     Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeYZ, new float3x3(rotation_ZfortyFive))).Using<float3x3>(EqualityWithTolerance)
            // );
            //
            // Assert.That(
            //     math.mul(new float3x3(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_compound_negativeYZ, quaternion.identity)), worldToLocal_compound_negativeYZScale),
            //     Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeYZ, new float3x3(quaternion.identity))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(new float3x3(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_compound_negativeYZ, rotation_ninety)), worldToLocal_compound_negativeYZScale),
            //     Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeYZ, new float3x3(rotation_ninety))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(new float3x3(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_compound_negativeYZ, rotation_Zninety)), worldToLocal_compound_negativeYZScale),
            //     Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeYZ, new float3x3(rotation_Zninety))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(new float3x3(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_compound_negativeYZ, rotation_fortyFive)), worldToLocal_compound_negativeYZScale),
            //     Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeYZ, new float3x3(rotation_fortyFive))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(new float3x3(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_compound_negativeYZ, rotation_ZfortyFive)), worldToLocal_compound_negativeYZScale),
            //     Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeYZ, new float3x3(rotation_ZfortyFive))).Using<float3x3>(EqualityWithTolerance)
            // );
        }

        // ----- ConvertLocalToWorldRotation ----- //
        [Test]
        public static void ConvertLocalToWorldRotationTest_Rotate()
        {
            float3 point_zero = float3.zero;
            float3 point_one = new float3(1f, 1f, 1f);
            quaternion rotation_fortyFive = quaternion.Euler(math.radians(45f), math.radians(45f), math.radians(45f));
            quaternion rotation_ninety = quaternion.Euler(math.radians(90f), math.radians(90f), math.radians(90f));
            quaternion rotation_ZfortyFive = quaternion.Euler(0f, 0f, math.radians(45f));
            quaternion rotation_Zninety = quaternion.Euler(0f, 0f, math.radians(90f));


            LocalToWorld localToWorld_ninety = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, rotation_ninety, point_one)
            };
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_ninety, quaternion.identity), Is.EqualTo(math.mul(rotation_ninety, quaternion.identity)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_ninety, rotation_ninety), Is.EqualTo(math.mul(rotation_ninety, rotation_ninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_ninety, rotation_Zninety), Is.EqualTo(math.mul(rotation_ninety, rotation_Zninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_ninety, rotation_fortyFive), Is.EqualTo(math.mul(rotation_ninety, rotation_fortyFive)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_ninety, rotation_ZfortyFive), Is.EqualTo(math.mul(rotation_ninety, rotation_ZfortyFive)).Using<quaternion>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_ninety.Value, quaternion.identity), Is.EqualTo(math.mul(rotation_ninety, quaternion.identity)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_ninety.Value, rotation_ninety), Is.EqualTo(math.mul(rotation_ninety, rotation_ninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_ninety.Value, rotation_Zninety), Is.EqualTo(math.mul(rotation_ninety, rotation_Zninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_ninety.Value, rotation_fortyFive), Is.EqualTo(math.mul(rotation_ninety, rotation_fortyFive)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_ninety.Value, rotation_ZfortyFive), Is.EqualTo(math.mul(rotation_ninety, rotation_ZfortyFive)).Using<quaternion>(EqualityWithTolerance));


            LocalToWorld localToWorld_fortyFive = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, rotation_fortyFive, point_one)
            };
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_fortyFive, quaternion.identity), Is.EqualTo(math.mul(rotation_fortyFive, quaternion.identity)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_fortyFive, rotation_ninety), Is.EqualTo(math.mul(rotation_fortyFive, rotation_ninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_fortyFive, rotation_Zninety), Is.EqualTo(math.mul(rotation_fortyFive, rotation_Zninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_fortyFive, rotation_fortyFive), Is.EqualTo(math.mul(rotation_fortyFive, rotation_fortyFive)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_fortyFive, rotation_ZfortyFive), Is.EqualTo(math.mul(rotation_fortyFive, rotation_ZfortyFive)).Using<quaternion>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_fortyFive.Value, quaternion.identity), Is.EqualTo(math.mul(rotation_fortyFive, quaternion.identity)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_fortyFive.Value, rotation_ninety), Is.EqualTo(math.mul(rotation_fortyFive, rotation_ninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_fortyFive.Value, rotation_Zninety), Is.EqualTo(math.mul(rotation_fortyFive, rotation_Zninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_fortyFive.Value, rotation_fortyFive), Is.EqualTo(math.mul(rotation_fortyFive, rotation_fortyFive)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_fortyFive.Value, rotation_ZfortyFive), Is.EqualTo(math.mul(rotation_fortyFive, rotation_ZfortyFive)).Using<quaternion>(EqualityWithTolerance));


            LocalToWorld localToWorld_Zninety = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, rotation_Zninety, point_one)
            };
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_Zninety, quaternion.identity), Is.EqualTo(math.mul(rotation_Zninety, quaternion.identity)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_Zninety, rotation_ninety), Is.EqualTo(math.mul(rotation_Zninety, rotation_ninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_Zninety, rotation_Zninety), Is.EqualTo(math.mul(rotation_Zninety, rotation_Zninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_Zninety, rotation_fortyFive), Is.EqualTo(math.mul(rotation_Zninety, rotation_fortyFive)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_Zninety, rotation_ZfortyFive), Is.EqualTo(math.mul(rotation_Zninety, rotation_ZfortyFive)).Using<quaternion>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_Zninety.Value, quaternion.identity), Is.EqualTo(math.mul(rotation_Zninety, quaternion.identity)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_Zninety.Value, rotation_ninety), Is.EqualTo(math.mul(rotation_Zninety, rotation_ninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_Zninety.Value, rotation_Zninety), Is.EqualTo(math.mul(rotation_Zninety, rotation_Zninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_Zninety.Value, rotation_fortyFive), Is.EqualTo(math.mul(rotation_Zninety, rotation_fortyFive)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_Zninety.Value, rotation_ZfortyFive), Is.EqualTo(math.mul(rotation_Zninety, rotation_ZfortyFive)).Using<quaternion>(EqualityWithTolerance));


            LocalToWorld localToWorld_ZfortyFive = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, rotation_ZfortyFive, point_one)
            };
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_ZfortyFive, quaternion.identity), Is.EqualTo(math.mul(rotation_ZfortyFive, quaternion.identity)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_ZfortyFive, rotation_ninety), Is.EqualTo(math.mul(rotation_ZfortyFive, rotation_ninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_ZfortyFive, rotation_Zninety), Is.EqualTo(math.mul(rotation_ZfortyFive, rotation_Zninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_ZfortyFive, rotation_fortyFive), Is.EqualTo(math.mul(rotation_ZfortyFive, rotation_fortyFive)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_ZfortyFive, rotation_ZfortyFive), Is.EqualTo(math.mul(rotation_ZfortyFive, rotation_ZfortyFive)).Using<quaternion>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_ZfortyFive.Value, quaternion.identity), Is.EqualTo(math.mul(rotation_ZfortyFive, quaternion.identity)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_ZfortyFive.Value, rotation_ninety), Is.EqualTo(math.mul(rotation_ZfortyFive, rotation_ninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_ZfortyFive.Value, rotation_Zninety), Is.EqualTo(math.mul(rotation_ZfortyFive, rotation_Zninety)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_ZfortyFive.Value, rotation_fortyFive), Is.EqualTo(math.mul(rotation_ZfortyFive, rotation_fortyFive)).Using<quaternion>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_ZfortyFive.Value, rotation_ZfortyFive), Is.EqualTo(math.mul(rotation_ZfortyFive, rotation_ZfortyFive)).Using<quaternion>(EqualityWithTolerance));
        }

        [Test]
        public static void ConvertLocalToWorldRotationTest_Compound()
        {
            Regex invalidMatrixError_rotate = new Regex(@"Transform is not valid\. Returning identity rotation\.");

            float3 point_zero = float3.zero;
            float3 point_one = new float3(1f, 1f, 1f);
            float3 point_sevenXY = new float3(7f, 7f, 0f);
            quaternion rotation_fortyFive = quaternion.Euler(math.radians(45f), math.radians(45f), math.radians(45f));
            quaternion rotation_ninety = quaternion.Euler(math.radians(90f), math.radians(90f), math.radians(90f));
            quaternion rotation_ZfortyFive = quaternion.Euler(0f, 0f, math.radians(45f));
            quaternion rotation_Zninety = quaternion.Euler(0f, 0f, math.radians(90f));

            quaternion rotation_XZ_fortyFive = quaternion.Euler(math.radians(45), 0, math.radians(45));
            quaternion rotation_XZ_fortyFive_inverse = math.inverse(rotation_XZ_fortyFive);
            quaternion rotation_XZ_negativeFortyFive = quaternion.Euler(math.radians(-45), 0, math.radians(-45));
            quaternion rotation_XZ_negativeFortyFive_inverse = math.inverse(rotation_XZ_negativeFortyFive);


            LocalToWorld localToWorld_compound = new LocalToWorld()
            {
                Value = float4x4.TRS(point_sevenXY, rotation_XZ_fortyFive, point_one*2f)
            };
            Assert.That(
                TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound, quaternion.identity),
                Is.EqualTo(math.mul(rotation_XZ_fortyFive, quaternion.identity)).Using<quaternion>(EqualityWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound, rotation_ninety),
                Is.EqualTo(math.mul(rotation_XZ_fortyFive, rotation_ninety)).Using<quaternion>(EqualityWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound, rotation_Zninety),
                Is.EqualTo(math.mul(rotation_XZ_fortyFive, rotation_Zninety)).Using<quaternion>(EqualityWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound, rotation_fortyFive),
                Is.EqualTo(math.mul(rotation_XZ_fortyFive, rotation_fortyFive)).Using<quaternion>(EqualityWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound, rotation_ZfortyFive),
                Is.EqualTo(math.mul(rotation_XZ_fortyFive, rotation_ZfortyFive)).Using<quaternion>(EqualityWithTolerance)
                );

            Assert.That(
                TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound.Value, quaternion.identity),
                Is.EqualTo(math.mul(rotation_XZ_fortyFive, quaternion.identity)).Using<quaternion>(EqualityWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound.Value, rotation_ninety),
                Is.EqualTo(math.mul(rotation_XZ_fortyFive, rotation_ninety)).Using<quaternion>(EqualityWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound.Value, rotation_Zninety),
                Is.EqualTo(math.mul(rotation_XZ_fortyFive, rotation_Zninety)).Using<quaternion>(EqualityWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound.Value, rotation_fortyFive),
                Is.EqualTo(math.mul(rotation_XZ_fortyFive, rotation_fortyFive)).Using<quaternion>(EqualityWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound.Value, rotation_ZfortyFive),
                Is.EqualTo(math.mul(rotation_XZ_fortyFive, rotation_ZfortyFive)).Using<quaternion>(EqualityWithTolerance)
                );


            LocalToWorld localToWorld_compound_negativeZ = new LocalToWorld()
            {
                Value = float4x4.TRS(-point_sevenXY, rotation_XZ_negativeFortyFive, new float3(2f, 2f, -2f))
            };
            float4x4 localToWorldMatrix_compound_negativeZ = localToWorld_compound_negativeZ.Value;
            float3x3 localToWorld_compound_negativeZScale = float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_compound_negativeZ, new float3(1f)));

            Assert.That(
                math.mul(new float3x3(TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound_negativeZ, quaternion.identity)), localToWorld_compound_negativeZScale),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeZ, new float3x3(quaternion.identity))).Using<float3x3>(EqualityWithTolerance)
                );
            Assert.That(
                math.mul(new float3x3(TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound_negativeZ, rotation_ninety)), localToWorld_compound_negativeZScale),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeZ, new float3x3(rotation_ninety))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(new float3x3(TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound_negativeZ, rotation_Zninety)), localToWorld_compound_negativeZScale),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeZ, new float3x3(rotation_Zninety))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(new float3x3(TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound_negativeZ, rotation_fortyFive)), localToWorld_compound_negativeZScale),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeZ, new float3x3(rotation_fortyFive))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(new float3x3(TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound_negativeZ, rotation_ZfortyFive)), localToWorld_compound_negativeZScale),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeZ, new float3x3(rotation_ZfortyFive))).Using<float3x3>(EqualityWithTolerance)
            );

            Assert.That(
                math.mul(new float3x3(TransformUtil.ConvertLocalToWorldRotation(localToWorldMatrix_compound_negativeZ, quaternion.identity)), localToWorld_compound_negativeZScale),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeZ, new float3x3(quaternion.identity))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(new float3x3(TransformUtil.ConvertLocalToWorldRotation(localToWorldMatrix_compound_negativeZ, rotation_ninety)), localToWorld_compound_negativeZScale),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeZ, new float3x3(rotation_ninety))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(new float3x3(TransformUtil.ConvertLocalToWorldRotation(localToWorldMatrix_compound_negativeZ, rotation_Zninety)), localToWorld_compound_negativeZScale),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeZ, new float3x3(rotation_Zninety))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(new float3x3(TransformUtil.ConvertLocalToWorldRotation(localToWorldMatrix_compound_negativeZ, rotation_fortyFive)), localToWorld_compound_negativeZScale),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeZ, new float3x3(rotation_fortyFive))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(new float3x3(TransformUtil.ConvertLocalToWorldRotation(localToWorldMatrix_compound_negativeZ, rotation_ZfortyFive)), localToWorld_compound_negativeZScale),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeZ, new float3x3(rotation_ZfortyFive))).Using<float3x3>(EqualityWithTolerance)
            );


            LocalToWorld localToWorld_compound_zeroScale = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, rotation_ZfortyFive, point_zero)
            };

            Logger.Debug("BEGIN: Expected error messages.");
            LogAssert.Expect(LogType.Error, invalidMatrixError_rotate);
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound_zeroScale, quaternion.identity), Is.EqualTo(quaternion.identity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_rotate);
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound_zeroScale, rotation_ninety), Is.EqualTo(quaternion.identity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_rotate);
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound_zeroScale, rotation_Zninety), Is.EqualTo(quaternion.identity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_rotate);
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound_zeroScale, rotation_fortyFive), Is.EqualTo(quaternion.identity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_rotate);
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound_zeroScale, rotation_ZfortyFive), Is.EqualTo(quaternion.identity));

            LogAssert.Expect(LogType.Error, invalidMatrixError_rotate);
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound_zeroScale.Value, quaternion.identity), Is.EqualTo(quaternion.identity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_rotate);
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound_zeroScale.Value, rotation_ninety), Is.EqualTo(quaternion.identity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_rotate);
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound_zeroScale.Value, rotation_Zninety), Is.EqualTo(quaternion.identity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_rotate);
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound_zeroScale.Value, rotation_fortyFive), Is.EqualTo(quaternion.identity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_rotate);
            Assert.That(TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound_zeroScale.Value, rotation_ZfortyFive), Is.EqualTo(quaternion.identity));
            Logger.Debug("END: Expected error messages.");


            //TODO: #116 - Transforms with non-uniform scale operations are not currently supported.
            // Once supported, actual tests are commented out below.
            LocalToWorld localToWorld_compound_negativeYZ = new LocalToWorld()
            {
                Value = float4x4.TRS(-point_sevenXY, quaternion.identity, new float3(1f, -2f, -1.5f))
            };
            float4x4 localToWorldMatrix_compound_negativeYZ = localToWorld_compound_negativeYZ.Value;

#if DEBUG
            Logger.Debug("BEGIN: Expected error messages.");
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound_negativeYZ, quaternion.identity);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound_negativeYZ, rotation_ninety);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound_negativeYZ, rotation_Zninety);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound_negativeYZ, rotation_fortyFive);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound_negativeYZ, rotation_ZfortyFive);

            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertLocalToWorldRotation(localToWorldMatrix_compound_negativeYZ, quaternion.identity);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertLocalToWorldRotation(localToWorldMatrix_compound_negativeYZ, rotation_ninety);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertLocalToWorldRotation(localToWorldMatrix_compound_negativeYZ, rotation_Zninety);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertLocalToWorldRotation(localToWorldMatrix_compound_negativeYZ, rotation_fortyFive);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertLocalToWorldRotation(localToWorldMatrix_compound_negativeYZ, rotation_ZfortyFive);
            Logger.Debug("END: Expected error messages.");
#endif

            // // These follow the template of the asserts for uniform scale transforms. They may or may not result in correct values for
            // // non-uniform scaling.
            // float3x3 localToWorld_compound_negativeYZScale = float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_compound_negativeYZ, new float3(1f));
            // Assert.That(
            //     math.mul(new float3x3(TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound_negativeYZ, quaternion.identity)), localToWorld_compound_negativeYZScale),
            //     Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeYZ, new float3x3(quaternion.identity))).Using<float3x3>(EqualityWithTolerance)
            //     );
            // Assert.That(
            //     math.mul(new float3x3(TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound_negativeYZ, rotation_ninety)), localToWorld_compound_negativeYZScale),
            //     Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeYZ, new float3x3(rotation_ninety))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(new float3x3(TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound_negativeYZ, rotation_Zninety)), localToWorld_compound_negativeYZScale),
            //     Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeYZ, new float3x3(rotation_Zninety))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(new float3x3(TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound_negativeYZ, rotation_fortyFive)), localToWorld_compound_negativeYZScale),
            //     Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeYZ, new float3x3(rotation_fortyFive))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(new float3x3(TransformUtil.ConvertLocalToWorldRotation(localToWorld_compound_negativeYZ, rotation_ZfortyFive)), localToWorld_compound_negativeYZScale),
            //     Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeYZ, new float3x3(rotation_ZfortyFive))).Using<float3x3>(EqualityWithTolerance)
            // );
            //
            // Assert.That(
            //     math.mul(new float3x3(TransformUtil.ConvertLocalToWorldRotation(localToWorldMatrix_compound_negativeYZ, quaternion.identity)), localToWorld_compound_negativeYZScale),
            //     Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeYZ, new float3x3(quaternion.identity))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(new float3x3(TransformUtil.ConvertLocalToWorldRotation(localToWorldMatrix_compound_negativeYZ, rotation_ninety)), localToWorld_compound_negativeYZScale),
            //     Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeYZ, new float3x3(rotation_ninety))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(new float3x3(TransformUtil.ConvertLocalToWorldRotation(localToWorldMatrix_compound_negativeYZ, rotation_Zninety)), localToWorld_compound_negativeYZScale),
            //     Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeYZ, new float3x3(rotation_Zninety))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(new float3x3(TransformUtil.ConvertLocalToWorldRotation(localToWorldMatrix_compound_negativeYZ, rotation_fortyFive)), localToWorld_compound_negativeYZScale),
            //     Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeYZ, new float3x3(rotation_fortyFive))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(new float3x3(TransformUtil.ConvertLocalToWorldRotation(localToWorldMatrix_compound_negativeYZ, rotation_ZfortyFive)), localToWorld_compound_negativeYZScale),
            //     Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeYZ, new float3x3(rotation_ZfortyFive))).Using<float3x3>(EqualityWithTolerance)
            // );
        }

        // ----- ConvertWorldToLocalScale ----- //
        [Test]
        public static void ConvertWorldToLocalScaleTest_Scale()
        {
            Regex invalidMatrixError_scale = new Regex(@"This transform is invalid\. Returning a signed infinite scale\.");
            float3 point_zero = float3.zero;
            float3 point_one = new float3(1f, 1f, 1f);
            float3 point_two = point_one * 2f;
            float3 point_infinity = new float3(float.PositiveInfinity);


            LocalToWorld localToWorld_one = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.identity, point_one)
            };
            float4x4 worldToLocal_one = math.inverse(localToWorld_one.Value);

            Assert.That(TransformUtil.ConvertWorldToLocalScale(localToWorld_one, point_one), Is.EqualTo(point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalScale(localToWorld_one, point_two), Is.EqualTo(point_two).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalScale(localToWorld_one, -point_one), Is.EqualTo(-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalScale(localToWorld_one, -point_two), Is.EqualTo(-point_two).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalScale(localToWorld_one, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertWorldToLocalScale(worldToLocal_one, point_one), Is.EqualTo(point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalScale(worldToLocal_one, point_two), Is.EqualTo(point_two).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalScale(worldToLocal_one, -point_one), Is.EqualTo(-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalScale(worldToLocal_one, -point_two), Is.EqualTo(-point_two).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalScale(worldToLocal_one, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));


            LocalToWorld localToWorld_two = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.identity, point_two)
            };
            float4x4 worldToLocal_two = math.inverse(localToWorld_two.Value);

            Assert.That(TransformUtil.ConvertWorldToLocalScale(localToWorld_two, point_one), Is.EqualTo(point_one/point_two).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalScale(localToWorld_two, point_two), Is.EqualTo(point_two/point_two).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalScale(localToWorld_two, -point_one), Is.EqualTo(-point_one/point_two).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalScale(localToWorld_two, -point_two), Is.EqualTo(-point_two/point_two).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalScale(localToWorld_two, point_zero), Is.EqualTo(point_zero/point_two).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertWorldToLocalScale(worldToLocal_two, point_one), Is.EqualTo(point_one/point_two).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalScale(worldToLocal_two, point_two), Is.EqualTo(point_two/point_two).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalScale(worldToLocal_two, -point_one), Is.EqualTo(-point_one/point_two).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalScale(worldToLocal_two, -point_two), Is.EqualTo(-point_two/point_two).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalScale(worldToLocal_two, point_zero), Is.EqualTo(point_zero/point_two).Using<float3>(EqualityWithTolerance));


            LocalToWorld localToWorld_negativeOne = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.identity, -point_one)
            };
            float4x4 worldToLocal_negativeOne = math.inverse(localToWorld_negativeOne.Value);
            float3x3 worldToLocal_negativeOneRotation = new float3x3(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_negativeOne, quaternion.identity));

            Assert.That(
                math.mul(worldToLocal_negativeOneRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(localToWorld_negativeOne, point_one))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_negativeOne, float3x3.Scale(point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_negativeOneRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(localToWorld_negativeOne, point_two))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_negativeOne, float3x3.Scale(point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_negativeOneRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(localToWorld_negativeOne, -point_one))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_negativeOne, float3x3.Scale(-point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_negativeOneRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(localToWorld_negativeOne, -point_two))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_negativeOne, float3x3.Scale(-point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_negativeOneRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(localToWorld_negativeOne, point_zero))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_negativeOne, float3x3.Scale(point_zero))).Using<float3x3>(EqualityWithTolerance)
            );

            Assert.That(
                math.mul(worldToLocal_negativeOneRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(worldToLocal_negativeOne, point_one))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_negativeOne, float3x3.Scale(point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_negativeOneRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(worldToLocal_negativeOne, point_two))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_negativeOne, float3x3.Scale(point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_negativeOneRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(worldToLocal_negativeOne, -point_one))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_negativeOne, float3x3.Scale(-point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_negativeOneRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(worldToLocal_negativeOne, -point_two))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_negativeOne, float3x3.Scale(-point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_negativeOneRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(worldToLocal_negativeOne, point_zero))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_negativeOne, float3x3.Scale(point_zero))).Using<float3x3>(EqualityWithTolerance)
            );


            LocalToWorld localToWorld_negativeTwo = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.identity, -point_two)
            };
            float4x4 worldToLocal_negativeTwo = math.inverse(localToWorld_negativeTwo.Value);
            float3x3 worldToLocal_negativeTwoRotation = new float3x3(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_negativeTwo, quaternion.identity));

            Assert.That(
                math.mul(worldToLocal_negativeTwoRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(localToWorld_negativeTwo, point_one))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_negativeTwo, float3x3.Scale(point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_negativeTwoRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(localToWorld_negativeTwo, point_two))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_negativeTwo, float3x3.Scale(point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_negativeTwoRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(localToWorld_negativeTwo, -point_one))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_negativeTwo, float3x3.Scale(-point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_negativeTwoRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(localToWorld_negativeTwo, -point_two))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_negativeTwo, float3x3.Scale(-point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_negativeTwoRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(localToWorld_negativeTwo, point_zero))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_negativeTwo, float3x3.Scale(point_zero))).Using<float3x3>(EqualityWithTolerance)
            );

            Assert.That(
                math.mul(worldToLocal_negativeTwoRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(worldToLocal_negativeTwo, point_one))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_negativeTwo, float3x3.Scale(point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_negativeTwoRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(worldToLocal_negativeTwo, point_two))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_negativeTwo, float3x3.Scale(point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_negativeTwoRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(worldToLocal_negativeTwo, -point_one))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_negativeTwo, float3x3.Scale(-point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_negativeTwoRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(worldToLocal_negativeTwo, -point_two))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_negativeTwo, float3x3.Scale(-point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_negativeTwoRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(worldToLocal_negativeTwo, point_zero))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_negativeTwo, float3x3.Scale(point_zero))).Using<float3x3>(EqualityWithTolerance)
            );


            LocalToWorld localToWorld_zero = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.identity, point_zero)
            };
            float4x4 worldToLocal_zero = math.inverse(localToWorld_zero.Value);

            Logger.Debug("BEGIN: Expected error messages.");
            LogAssert.Expect(LogType.Error, invalidMatrixError_scale);
            Assert.That(TransformUtil.ConvertWorldToLocalScale(localToWorld_zero, point_one), Is.EqualTo(point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_scale);
            Assert.That(TransformUtil.ConvertWorldToLocalScale(localToWorld_zero, point_two), Is.EqualTo(point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_scale);
            Assert.That(TransformUtil.ConvertWorldToLocalScale(localToWorld_zero, -point_one), Is.EqualTo(-point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_scale);
            Assert.That(TransformUtil.ConvertWorldToLocalScale(localToWorld_zero, -point_two), Is.EqualTo(-point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_scale);
            Assert.That(TransformUtil.ConvertWorldToLocalScale(localToWorld_zero, point_zero), Is.EqualTo(point_zero));
            LogAssert.Expect(LogType.Error, invalidMatrixError_scale);
            Assert.That(
                TransformUtil.ConvertWorldToLocalScale(localToWorld_zero, new float3(2, -2, 0)),
                Is.EqualTo(new float3(float.PositiveInfinity, float.NegativeInfinity, 0))
                );

            LogAssert.Expect(LogType.Error, invalidMatrixError_scale);
            Assert.That(TransformUtil.ConvertWorldToLocalScale(worldToLocal_zero, point_one), Is.EqualTo(point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_scale);
            Assert.That(TransformUtil.ConvertWorldToLocalScale(worldToLocal_zero, point_two), Is.EqualTo(point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_scale);
            Assert.That(TransformUtil.ConvertWorldToLocalScale(worldToLocal_zero, -point_one), Is.EqualTo(-point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_scale);
            Assert.That(TransformUtil.ConvertWorldToLocalScale(worldToLocal_zero, -point_two), Is.EqualTo(-point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_scale);
            Assert.That(TransformUtil.ConvertWorldToLocalScale(worldToLocal_zero, point_zero), Is.EqualTo(point_zero));
            LogAssert.Expect(LogType.Error, invalidMatrixError_scale);
            Assert.That(
                TransformUtil.ConvertWorldToLocalScale(worldToLocal_zero, new float3(2, -2, 0)),
                Is.EqualTo(new float3(float.PositiveInfinity, float.NegativeInfinity, 0))
            );
            Logger.Debug("END: Expected error messages.");


            //TODO: #116 - Transforms with non-uniform scale operations are not currently supported.
            // Tests with assertions commented out below would replace these tests.
            LocalToWorld localToWorld_nonUniform = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.identity, new float3(1.5f, 3, 1))
            };
            float4x4 worldToLocal_nonUniform = math.inverse(localToWorld_nonUniform.Value);

#if DEBUG
            Logger.Debug("BEGIN: Expected error messages.");
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertWorldToLocalScale(localToWorld_nonUniform, point_one);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertWorldToLocalScale(localToWorld_nonUniform, point_two);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertWorldToLocalScale(localToWorld_nonUniform, -point_one);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertWorldToLocalScale(localToWorld_nonUniform, -point_two);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertWorldToLocalScale(localToWorld_nonUniform, point_zero);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertWorldToLocalScale(localToWorld_nonUniform, new float3(2, -2, 0));

            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertWorldToLocalScale(worldToLocal_nonUniform, point_one);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertWorldToLocalScale(worldToLocal_nonUniform, point_two);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertWorldToLocalScale(worldToLocal_nonUniform, -point_one);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertWorldToLocalScale(worldToLocal_nonUniform, -point_two);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertWorldToLocalScale(worldToLocal_nonUniform, point_zero);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertWorldToLocalScale(worldToLocal_nonUniform, new float3(2, -2, 0));
            Logger.Debug("END: Expected error messages.");
#endif

            // float3x3 worldToLocal_nonUniformRotation = new float3x3(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_nonUniform, quaternion.identity));
            // Assert.That(
            //     math.mul(worldToLocal_nonUniformRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(localToWorld_nonUniform, point_one))),
            //     Is.EqualTo(math.mul((float3x3)worldToLocal_nonUniform, float3x3.Scale(point_one))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(worldToLocal_nonUniformRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(localToWorld_nonUniform, point_two))),
            //     Is.EqualTo(math.mul((float3x3)worldToLocal_nonUniform, float3x3.Scale(point_two))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(worldToLocal_nonUniformRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(localToWorld_nonUniform, -point_one))),
            //     Is.EqualTo(math.mul((float3x3)worldToLocal_nonUniform, float3x3.Scale(-point_one))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(worldToLocal_nonUniformRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(localToWorld_nonUniform, -point_two))),
            //     Is.EqualTo(math.mul((float3x3)worldToLocal_nonUniform, float3x3.Scale(-point_two))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(worldToLocal_nonUniformRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(localToWorld_nonUniform, point_zero))),
            //     Is.EqualTo(math.mul((float3x3)worldToLocal_nonUniform, float3x3.Scale(point_zero))).Using<float3x3>(EqualityWithTolerance)
            // );
            //
            // Assert.That(
            //     math.mul(worldToLocal_nonUniformRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(worldToLocal_nonUniform, point_one))),
            //     Is.EqualTo(math.mul((float3x3)worldToLocal_nonUniform, float3x3.Scale(point_one))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(worldToLocal_nonUniformRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(worldToLocal_nonUniform, point_two))),
            //     Is.EqualTo(math.mul((float3x3)worldToLocal_nonUniform, float3x3.Scale(point_two))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(worldToLocal_nonUniformRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(worldToLocal_nonUniform, -point_one))),
            //     Is.EqualTo(math.mul((float3x3)worldToLocal_nonUniform, float3x3.Scale(-point_one))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(worldToLocal_nonUniformRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(worldToLocal_nonUniform, -point_two))),
            //     Is.EqualTo(math.mul((float3x3)worldToLocal_nonUniform, float3x3.Scale(-point_two))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(worldToLocal_nonUniformRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(worldToLocal_nonUniform, point_zero))),
            //     Is.EqualTo(math.mul((float3x3)worldToLocal_nonUniform, float3x3.Scale(point_zero))).Using<float3x3>(EqualityWithTolerance)
            // );

        }

        [Test]
        public static void ConvertWorldToLocalScaleTest_Compound()
        {
            float3 point_zero = float3.zero;
            float3 point_one = new float3(1f, 1f, 1f);
            float3 point_two = point_one * 2f;
            float3 point_sevenXY = new float3(7f, 7f, 0f);

            quaternion rotation_XZ_fortyFive = quaternion.Euler(math.radians(45), 0, math.radians(45));
            quaternion rotation_XZ_negativeFortyFive = quaternion.Euler(math.radians(-45), 0, math.radians(-45));


            LocalToWorld localToWorld_compound = new LocalToWorld()
            {
                Value = float4x4.TRS(point_sevenXY, rotation_XZ_fortyFive, point_one*2f)
            };
            float4x4 worldToLocal_compound = math.inverse(localToWorld_compound.Value);
            float3x3 worldToLocal_compound_rotation = new float3x3(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_compound, quaternion.identity));

            Assert.That(
                math.mul(worldToLocal_compound_rotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(localToWorld_compound, point_one))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound, float3x3.Scale(point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_compound_rotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(localToWorld_compound, point_two))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound, float3x3.Scale(point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_compound_rotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(localToWorld_compound, -point_one))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound, float3x3.Scale(-point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_compound_rotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(localToWorld_compound, -point_two))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound, float3x3.Scale(-point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_compound_rotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(localToWorld_compound, point_zero))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound, float3x3.Scale(point_zero))).Using<float3x3>(EqualityWithTolerance)
            );

            Assert.That(
                math.mul(worldToLocal_compound_rotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(worldToLocal_compound, point_one))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound, float3x3.Scale(point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_compound_rotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(worldToLocal_compound, point_two))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound, float3x3.Scale(point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_compound_rotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(worldToLocal_compound, -point_one))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound, float3x3.Scale(-point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_compound_rotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(worldToLocal_compound, -point_two))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound, float3x3.Scale(-point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_compound_rotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(worldToLocal_compound, point_zero))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound, float3x3.Scale(point_zero))).Using<float3x3>(EqualityWithTolerance)
            );


            LocalToWorld localToWorld_compound_negativeZ = new LocalToWorld()
            {
                Value = float4x4.TRS(-point_sevenXY, rotation_XZ_negativeFortyFive, new float3(2f, 2f, -2f))
            };
            float4x4 worldToLocal_compound_negativeZ = math.inverse(localToWorld_compound_negativeZ.Value);
            float3x3 worldToLocal_compound_negativeZRotation = new float3x3(TransformUtil.ConvertWorldToLocalRotation(worldToLocal_compound_negativeZ, quaternion.identity));

            Assert.That(
                math.mul(worldToLocal_compound_negativeZRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(localToWorld_compound_negativeZ, point_one))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeZ, float3x3.Scale(point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_compound_negativeZRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(localToWorld_compound_negativeZ, point_two))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeZ, float3x3.Scale(point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_compound_negativeZRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(localToWorld_compound_negativeZ, -point_one))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeZ, float3x3.Scale(-point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_compound_negativeZRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(localToWorld_compound_negativeZ, -point_two))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeZ, float3x3.Scale(-point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_compound_negativeZRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(localToWorld_compound_negativeZ, point_zero))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeZ, float3x3.Scale(point_zero))).Using<float3x3>(EqualityWithTolerance)
            );

            Assert.That(
                math.mul(worldToLocal_compound_negativeZRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(worldToLocal_compound_negativeZ, point_one))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeZ, float3x3.Scale(point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_compound_negativeZRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(worldToLocal_compound_negativeZ, point_two))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeZ, float3x3.Scale(point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_compound_negativeZRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(worldToLocal_compound_negativeZ, -point_one))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeZ, float3x3.Scale(-point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_compound_negativeZRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(worldToLocal_compound_negativeZ, -point_two))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeZ, float3x3.Scale(-point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(worldToLocal_compound_negativeZRotation, float3x3.Scale(TransformUtil.ConvertWorldToLocalScale(worldToLocal_compound_negativeZ, point_zero))),
                Is.EqualTo(math.mul((float3x3)worldToLocal_compound_negativeZ, float3x3.Scale(point_zero))).Using<float3x3>(EqualityWithTolerance)
            );
        }

        [Test]
        public static void ConvertLocalToWorldScaleTest_Scale()
        {
            Regex invalidMatrixError_scale = new Regex(@"This transform is invalid\. Returning a signed infinite scale\.");
            float3 point_zero = float3.zero;
            float3 point_one = new float3(1f, 1f, 1f);
            float3 point_two = point_one * 2f;
            float3 point_infinity = new float3(float.PositiveInfinity);


            LocalToWorld localToWorld_one = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.identity, point_one)
            };
            float4x4 localToWorldMatrix_one = localToWorld_one.Value;

            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorld_one, point_one), Is.EqualTo(point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorld_one, point_two), Is.EqualTo(point_two).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorld_one, -point_one), Is.EqualTo(-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorld_one, -point_two), Is.EqualTo(-point_two).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorld_one, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_one, point_one), Is.EqualTo(point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_one, point_two), Is.EqualTo(point_two).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_one, -point_one), Is.EqualTo(-point_one).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_one, -point_two), Is.EqualTo(-point_two).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_one, point_zero), Is.EqualTo(point_zero).Using<float3>(EqualityWithTolerance));


            LocalToWorld localToWorld_two = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.identity, point_two)
            };
            float4x4 localToWorldMatrix_two = localToWorld_two.Value;

            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorld_two, point_one), Is.EqualTo(point_one*point_two).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorld_two, point_two), Is.EqualTo(point_two*point_two).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorld_two, -point_one), Is.EqualTo(-point_one*point_two).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorld_two, -point_two), Is.EqualTo(-point_two*point_two).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorld_two, point_zero), Is.EqualTo(point_zero*point_two).Using<float3>(EqualityWithTolerance));

            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_two, point_one), Is.EqualTo(point_one*point_two).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_two, point_two), Is.EqualTo(point_two*point_two).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_two, -point_one), Is.EqualTo(-point_one*point_two).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_two, -point_two), Is.EqualTo(-point_two*point_two).Using<float3>(EqualityWithTolerance));
            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_two, point_zero), Is.EqualTo(point_zero*point_two).Using<float3>(EqualityWithTolerance));


            LocalToWorld localToWorld_negativeOne = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.identity, -point_one)
            };
            float4x4 localToWorldMatrix_negativeOne = localToWorld_negativeOne.Value;
            float3x3 localToWorld_negativeOneRotation = new float3x3(TransformUtil.ConvertLocalToWorldRotation(localToWorldMatrix_negativeOne, quaternion.identity));

            Assert.That(
                math.mul(localToWorld_negativeOneRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorld_negativeOne, point_one))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_negativeOne, float3x3.Scale(point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_negativeOneRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorld_negativeOne, point_two))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_negativeOne, float3x3.Scale(point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_negativeOneRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorld_negativeOne, -point_one))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_negativeOne, float3x3.Scale(-point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_negativeOneRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorld_negativeOne, -point_two))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_negativeOne, float3x3.Scale(-point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_negativeOneRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorld_negativeOne, point_zero))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_negativeOne, float3x3.Scale(point_zero))).Using<float3x3>(EqualityWithTolerance)
            );

            Assert.That(
                math.mul(localToWorld_negativeOneRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_negativeOne, point_one))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_negativeOne, float3x3.Scale(point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_negativeOneRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_negativeOne, point_two))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_negativeOne, float3x3.Scale(point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_negativeOneRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_negativeOne, -point_one))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_negativeOne, float3x3.Scale(-point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_negativeOneRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_negativeOne, -point_two))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_negativeOne, float3x3.Scale(-point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_negativeOneRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_negativeOne, point_zero))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_negativeOne, float3x3.Scale(point_zero))).Using<float3x3>(EqualityWithTolerance)
            );


            LocalToWorld localToWorld_negativeTwo = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.identity, -point_two)
            };
            float4x4 localToWorldMatrix_negativeTwo = localToWorld_negativeTwo.Value;
            float3x3 localToWorld_negativeTwoRotation = new float3x3(TransformUtil.ConvertLocalToWorldRotation(localToWorldMatrix_negativeTwo, quaternion.identity));

            Assert.That(
                math.mul(localToWorld_negativeTwoRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorld_negativeTwo, point_one))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_negativeTwo, float3x3.Scale(point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_negativeTwoRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorld_negativeTwo, point_two))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_negativeTwo, float3x3.Scale(point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_negativeTwoRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorld_negativeTwo, -point_one))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_negativeTwo, float3x3.Scale(-point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_negativeTwoRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorld_negativeTwo, -point_two))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_negativeTwo, float3x3.Scale(-point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_negativeTwoRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorld_negativeTwo, point_zero))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_negativeTwo, float3x3.Scale(point_zero))).Using<float3x3>(EqualityWithTolerance)
            );

            Assert.That(
                math.mul(localToWorld_negativeTwoRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_negativeTwo, point_one))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_negativeTwo, float3x3.Scale(point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_negativeTwoRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_negativeTwo, point_two))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_negativeTwo, float3x3.Scale(point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_negativeTwoRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_negativeTwo, -point_one))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_negativeTwo, float3x3.Scale(-point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_negativeTwoRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_negativeTwo, -point_two))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_negativeTwo, float3x3.Scale(-point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_negativeTwoRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_negativeTwo, point_zero))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_negativeTwo, float3x3.Scale(point_zero))).Using<float3x3>(EqualityWithTolerance)
            );


            LocalToWorld localToWorld_zero = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.identity, point_zero)
            };
            float4x4 localToWorldMatrix_zero = localToWorld_zero.Value;

            Logger.Debug("BEGIN: Expected error messages.");
            LogAssert.Expect(LogType.Error, invalidMatrixError_scale);
            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorld_zero, point_one), Is.EqualTo(point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_scale);
            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorld_zero, point_two), Is.EqualTo(point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_scale);
            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorld_zero, -point_one), Is.EqualTo(-point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_scale);
            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorld_zero, -point_two), Is.EqualTo(-point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_scale);
            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorld_zero, point_zero), Is.EqualTo(point_zero));
            LogAssert.Expect(LogType.Error, invalidMatrixError_scale);
            Assert.That(
                TransformUtil.ConvertLocalToWorldScale(localToWorld_zero, new float3(2, -2, 0)),
                Is.EqualTo(new float3(float.PositiveInfinity, float.NegativeInfinity, 0))
                );

            LogAssert.Expect(LogType.Error, invalidMatrixError_scale);
            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_zero, point_one), Is.EqualTo(point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_scale);
            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_zero, point_two), Is.EqualTo(point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_scale);
            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_zero, -point_one), Is.EqualTo(-point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_scale);
            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_zero, -point_two), Is.EqualTo(-point_infinity));
            LogAssert.Expect(LogType.Error, invalidMatrixError_scale);
            Assert.That(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_zero, point_zero), Is.EqualTo(point_zero));
            LogAssert.Expect(LogType.Error, invalidMatrixError_scale);
            Assert.That(
                TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_zero, new float3(2, -2, 0)),
                Is.EqualTo(new float3(float.PositiveInfinity, float.NegativeInfinity, 0))
            );
            Logger.Debug("END: Expected error messages.");


            //TODO: #116 - Transforms with non-uniform scale operations are not currently supported.
            // Tests with assertions commented out below would replace these tests.
            LocalToWorld localToWorld_nonUniform = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.identity, new float3(1.5f, 3, 1))
            };
            float4x4 localToWorldMatrix_nonUniform = localToWorld_nonUniform.Value;

#if DEBUG
            Logger.Debug("BEGIN: Expected error messages.");
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertLocalToWorldScale(localToWorld_nonUniform, point_one);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertLocalToWorldScale(localToWorld_nonUniform, point_two);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertLocalToWorldScale(localToWorld_nonUniform, -point_one);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertLocalToWorldScale(localToWorld_nonUniform, -point_two);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertLocalToWorldScale(localToWorld_nonUniform, point_zero);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertLocalToWorldScale(localToWorld_nonUniform, new float3(2, -2, 0));

            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_nonUniform, point_one);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_nonUniform, point_two);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_nonUniform, -point_one);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_nonUniform, -point_two);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_nonUniform, point_zero);
            LogAssert.Expect(LogType.Error, s_NonUniformScaleError);
            TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_nonUniform, new float3(2, -2, 0));
            Logger.Debug("END: Expected error messages.");
#endif

            // float3x3 localToWorld_nonUniformRotation = new float3x3(TransformUtil.ConvertLocalToWorldRotation(localToWorldMatrix_nonUniform, quaternion.identity));
            // Assert.That(
            //     math.mul(localToWorld_nonUniformRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorld_nonUniform, point_one))),
            //     Is.EqualTo(math.mul((float3x3)localToWorldMatrix_nonUniform, float3x3.Scale(point_one))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(localToWorld_nonUniformRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorld_nonUniform, point_two))),
            //     Is.EqualTo(math.mul((float3x3)localToWorldMatrix_nonUniform, float3x3.Scale(point_two))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(localToWorld_nonUniformRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorld_nonUniform, -point_one))),
            //     Is.EqualTo(math.mul((float3x3)localToWorldMatrix_nonUniform, float3x3.Scale(-point_one))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(localToWorld_nonUniformRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorld_nonUniform, -point_two))),
            //     Is.EqualTo(math.mul((float3x3)localToWorldMatrix_nonUniform, float3x3.Scale(-point_two))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(localToWorld_nonUniformRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorld_nonUniform, point_zero))),
            //     Is.EqualTo(math.mul((float3x3)localToWorldMatrix_nonUniform, float3x3.Scale(point_zero))).Using<float3x3>(EqualityWithTolerance)
            // );
            //
            // Assert.That(
            //     math.mul(localToWorld_nonUniformRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_nonUniform, point_one))),
            //     Is.EqualTo(math.mul((float3x3)localToWorldMatrix_nonUniform, float3x3.Scale(point_one))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(localToWorld_nonUniformRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_nonUniform, point_two))),
            //     Is.EqualTo(math.mul((float3x3)localToWorldMatrix_nonUniform, float3x3.Scale(point_two))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(localToWorld_nonUniformRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_nonUniform, -point_one))),
            //     Is.EqualTo(math.mul((float3x3)localToWorldMatrix_nonUniform, float3x3.Scale(-point_one))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(localToWorld_nonUniformRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_nonUniform, -point_two))),
            //     Is.EqualTo(math.mul((float3x3)localToWorldMatrix_nonUniform, float3x3.Scale(-point_two))).Using<float3x3>(EqualityWithTolerance)
            // );
            // Assert.That(
            //     math.mul(localToWorld_nonUniformRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_nonUniform, point_zero))),
            //     Is.EqualTo(math.mul((float3x3)localToWorldMatrix_nonUniform, float3x3.Scale(point_zero))).Using<float3x3>(EqualityWithTolerance)
            // );
        }

        [Test]
        public static void ConvertLocalToWorldScaleTest_Compound()
        {
            float3 point_zero = float3.zero;
            float3 point_one = new float3(1f, 1f, 1f);
            float3 point_two = point_one * 2f;
            float3 point_sevenXY = new float3(7f, 7f, 0f);

            quaternion rotation_XZ_fortyFive = quaternion.Euler(math.radians(45), 0, math.radians(45));
            quaternion rotation_XZ_negativeFortyFive = quaternion.Euler(math.radians(-45), 0, math.radians(-45));


            LocalToWorld localToWorld_compound = new LocalToWorld()
            {
                Value = float4x4.TRS(point_sevenXY, rotation_XZ_fortyFive, point_one*2f)
            };
            float4x4 localToWorldMatrix_compound = localToWorld_compound.Value;
            float3x3 localToWorld_compound_rotation = new float3x3(TransformUtil.ConvertLocalToWorldRotation(localToWorldMatrix_compound, quaternion.identity));

            Assert.That(
                math.mul(localToWorld_compound_rotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorld_compound, point_one))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound, float3x3.Scale(point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_compound_rotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorld_compound, point_two))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound, float3x3.Scale(point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_compound_rotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorld_compound, -point_one))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound, float3x3.Scale(-point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_compound_rotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorld_compound, -point_two))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound, float3x3.Scale(-point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_compound_rotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorld_compound, point_zero))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound, float3x3.Scale(point_zero))).Using<float3x3>(EqualityWithTolerance)
            );

            Assert.That(
                math.mul(localToWorld_compound_rotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_compound, point_one))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound, float3x3.Scale(point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_compound_rotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_compound, point_two))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound, float3x3.Scale(point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_compound_rotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_compound, -point_one))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound, float3x3.Scale(-point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_compound_rotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_compound, -point_two))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound, float3x3.Scale(-point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_compound_rotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_compound, point_zero))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound, float3x3.Scale(point_zero))).Using<float3x3>(EqualityWithTolerance)
            );


            LocalToWorld localToWorld_compound_negativeZ = new LocalToWorld()
            {
                Value = float4x4.TRS(-point_sevenXY, rotation_XZ_negativeFortyFive, new float3(2f, 2f, -2f))
            };
            float4x4 localToWorldMatrix_compound_negativeZ = localToWorld_compound_negativeZ.Value;
            float3x3 localToWorld_compound_negativeZRotation = new float3x3(TransformUtil.ConvertLocalToWorldRotation(localToWorldMatrix_compound_negativeZ, quaternion.identity));

            Assert.That(
                math.mul(localToWorld_compound_negativeZRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorld_compound_negativeZ, point_one))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeZ, float3x3.Scale(point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_compound_negativeZRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorld_compound_negativeZ, point_two))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeZ, float3x3.Scale(point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_compound_negativeZRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorld_compound_negativeZ, -point_one))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeZ, float3x3.Scale(-point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_compound_negativeZRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorld_compound_negativeZ, -point_two))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeZ, float3x3.Scale(-point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_compound_negativeZRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorld_compound_negativeZ, point_zero))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeZ, float3x3.Scale(point_zero))).Using<float3x3>(EqualityWithTolerance)
            );

            Assert.That(
                math.mul(localToWorld_compound_negativeZRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_compound_negativeZ, point_one))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeZ, float3x3.Scale(point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_compound_negativeZRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_compound_negativeZ, point_two))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeZ, float3x3.Scale(point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_compound_negativeZRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_compound_negativeZ, -point_one))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeZ, float3x3.Scale(-point_one))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_compound_negativeZRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_compound_negativeZ, -point_two))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeZ, float3x3.Scale(-point_two))).Using<float3x3>(EqualityWithTolerance)
            );
            Assert.That(
                math.mul(localToWorld_compound_negativeZRotation, float3x3.Scale(TransformUtil.ConvertLocalToWorldScale(localToWorldMatrix_compound_negativeZ, point_zero))),
                Is.EqualTo(math.mul((float3x3)localToWorldMatrix_compound_negativeZ, float3x3.Scale(point_zero))).Using<float3x3>(EqualityWithTolerance)
            );
        }

        // ----- ConvertWorldToLocalRect ----- //
        [Test]
        public static void ConvertWorldToLocalRectTest_Identity()
        {
            float3 point_zero = float3.zero;
            float3 point_one = new float3(1f);
            float3 point_seven = new float3(7f);

            Rect rect_zero_zero = Rect.zero;

            Rect rect_zero_one = new Rect(0, 0, 1, 1);

            Rect rect_seven_seven = new Rect(7, 7, 7, 7);
            Rect rect_seven_negativeSeven = new Rect(7, 7, -7, -7);

            Rect rect_zero_oneSeven = new Rect(0, 0, 1, 7);
            Rect rect_zero_sevenOne = new Rect(0, 0, 7, 1);
            Rect rect_zero_negativeOneSeven = new Rect(0, 0, -1, -7);
            Rect rect_zero_negativeSevenOne = new Rect(0, 0, -7, -1);

            Rect rect_negativeOffset_seven = new Rect(-14, -14, 7, 7);
            Rect rect_negativeOffset_negativeSeven = new Rect(-14, -14, -7, -7);

            Rect rect_centered_seven = new Rect(-3.5f, -3.5f, 7, 7);
            Rect rect_centered_negativeSeven = new Rect(-3.5f, -3.5f, -7, -7);

            Rect rect_centered_negativePositiveSeven = new Rect(-3.5f, -3.5f, -7, 7);


            LocalToWorld localToWorld_identity = new LocalToWorld()
            {
                Value = float4x4.TRS(point_zero, quaternion.identity, point_one)
            };

            Assert.That(TransformUtil.ConvertWorldToLocalRect(localToWorld_identity, rect_zero_zero), Is.EqualTo(rect_zero_zero).Using<Rect>(EquivalencyWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRect(localToWorld_identity, rect_zero_one), Is.EqualTo(rect_zero_one).Using<Rect>(EquivalencyWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRect(localToWorld_identity, rect_seven_seven), Is.EqualTo(rect_seven_seven).Using<Rect>(EquivalencyWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRect(localToWorld_identity, rect_seven_negativeSeven), Is.EqualTo(rect_seven_negativeSeven).Using<Rect>(EquivalencyWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRect(localToWorld_identity, rect_zero_oneSeven), Is.EqualTo(rect_zero_oneSeven).Using<Rect>(EquivalencyWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRect(localToWorld_identity, rect_zero_sevenOne), Is.EqualTo(rect_zero_sevenOne).Using<Rect>(EquivalencyWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRect(localToWorld_identity, rect_zero_negativeOneSeven), Is.EqualTo(rect_zero_negativeOneSeven).Using<Rect>(EquivalencyWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRect(localToWorld_identity, rect_zero_negativeSevenOne), Is.EqualTo(rect_zero_negativeSevenOne).Using<Rect>(EquivalencyWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRect(localToWorld_identity, rect_negativeOffset_seven), Is.EqualTo(rect_negativeOffset_seven).Using<Rect>(EquivalencyWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRect(localToWorld_identity, rect_negativeOffset_negativeSeven), Is.EqualTo(rect_negativeOffset_negativeSeven).Using<Rect>(EquivalencyWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRect(localToWorld_identity, rect_centered_seven), Is.EqualTo(rect_centered_seven).Using<Rect>(EquivalencyWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRect(localToWorld_identity, rect_centered_negativeSeven), Is.EqualTo(rect_centered_negativeSeven).Using<Rect>(EquivalencyWithTolerance));
            Assert.That(TransformUtil.ConvertWorldToLocalRect(localToWorld_identity, rect_centered_negativePositiveSeven), Is.EqualTo(rect_centered_negativePositiveSeven).Using<Rect>(EquivalencyWithTolerance));
        }

        [Test]
        public static void ConvertWorldToLocalRectTest_Translate()
        {
            float3 point_zero = float3.zero;
            float3 point_one = new float3(1f);
            float3 point_seven = new float3(7f);

            Rect rect_zero_zero = Rect.zero;

            Rect rect_zero_one = new Rect(0, 0, 1, 1);

            Rect rect_seven_seven = new Rect(7, 7, 7, 7);
            Rect rect_seven_negativeSeven = new Rect(7, 7, -7, -7);

            Rect rect_zero_oneSeven = new Rect(0, 0, 1, 7);
            Rect rect_zero_sevenOne = new Rect(0, 0, 7, 1);
            Rect rect_zero_negativeOneSeven = new Rect(0, 0, -1, -7);
            Rect rect_zero_negativeSevenOne = new Rect(0, 0, -7, -1);

            Rect rect_negativeOffset_seven = new Rect(-14, -14, 7, 7);
            Rect rect_negativeOffset_negativeSeven = new Rect(-14, -14, -7, -7);

            Rect rect_centered_seven = new Rect(-3.5f, -3.5f, 7, 7);
            Rect rect_centered_negativeSeven = new Rect(-3.5f, -3.5f, -7, -7);

            Rect rect_centered_negativePositiveSeven = new Rect(-3.5f, -3.5f, -7, 7);


            LocalToWorld localToWorld_translate = new LocalToWorld()
            {
                Value = float4x4.TRS(point_seven, quaternion.identity, point_one)
            };
            Vector2 localToWorld_translateTranslation = point_seven.xy;

            Assert.That(
                TransformUtil.ConvertWorldToLocalRect(localToWorld_translate, rect_zero_zero),
                Is.EqualTo(new Rect(rect_zero_zero.position - localToWorld_translateTranslation, rect_zero_zero.size)).Using<Rect>(EquivalencyWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRect(localToWorld_translate, rect_seven_seven),
                Is.EqualTo(new Rect(rect_seven_seven.position - localToWorld_translateTranslation, rect_seven_seven.size)).Using<Rect>(EquivalencyWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRect(localToWorld_translate, rect_zero_one),
                Is.EqualTo(new Rect(rect_zero_one.position - localToWorld_translateTranslation, rect_zero_one.size)).Using<Rect>(EquivalencyWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRect(localToWorld_translate, rect_seven_negativeSeven),
                Is.EqualTo(new Rect(rect_seven_negativeSeven.position - localToWorld_translateTranslation, rect_seven_negativeSeven.size)).Using<Rect>(EquivalencyWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRect(localToWorld_translate, rect_zero_oneSeven),
                Is.EqualTo(new Rect(rect_zero_oneSeven.position - localToWorld_translateTranslation, rect_zero_oneSeven.size)).Using<Rect>(EquivalencyWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRect(localToWorld_translate, rect_zero_sevenOne),
                Is.EqualTo(new Rect(rect_zero_sevenOne.position - localToWorld_translateTranslation, rect_zero_sevenOne.size)).Using<Rect>(EquivalencyWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRect(localToWorld_translate, rect_zero_negativeOneSeven),
                Is.EqualTo(new Rect(rect_zero_negativeOneSeven.position - localToWorld_translateTranslation, rect_zero_negativeOneSeven.size)).Using<Rect>(EquivalencyWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRect(localToWorld_translate, rect_zero_negativeSevenOne),
                Is.EqualTo(new Rect(rect_zero_negativeSevenOne.position - localToWorld_translateTranslation, rect_zero_negativeSevenOne.size)).Using<Rect>(EquivalencyWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRect(localToWorld_translate, rect_negativeOffset_seven),
                Is.EqualTo(new Rect(rect_negativeOffset_seven.position - localToWorld_translateTranslation, rect_negativeOffset_seven.size)).Using<Rect>(EquivalencyWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRect(localToWorld_translate, rect_negativeOffset_negativeSeven),
                Is.EqualTo(new Rect(rect_negativeOffset_negativeSeven.position - localToWorld_translateTranslation, rect_negativeOffset_negativeSeven.size)).Using<Rect>(EquivalencyWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRect(localToWorld_translate, rect_centered_seven),
                Is.EqualTo(new Rect(rect_centered_seven.position - localToWorld_translateTranslation, rect_centered_seven.size)).Using<Rect>(EquivalencyWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRect(localToWorld_translate, rect_centered_negativeSeven),
                Is.EqualTo(new Rect(rect_centered_negativeSeven.position - localToWorld_translateTranslation, rect_centered_negativeSeven.size)).Using<Rect>(EquivalencyWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRect(localToWorld_translate, rect_centered_negativePositiveSeven),
                Is.EqualTo(new Rect(rect_centered_negativePositiveSeven.position - localToWorld_translateTranslation, rect_centered_negativePositiveSeven.size)).Using<Rect>(EquivalencyWithTolerance)
                );


            LocalToWorld localToWorld_negativeTranslate = new LocalToWorld()
            {
                Value = float4x4.TRS(-point_seven, quaternion.identity, point_one)
            };
            Vector2 localToWorld_negativeTranslateTranslation = -point_seven.xy;

            Assert.That(
                TransformUtil.ConvertWorldToLocalRect(localToWorld_negativeTranslate, rect_zero_zero),
                Is.EqualTo(new Rect(rect_zero_zero.position - localToWorld_negativeTranslateTranslation, rect_zero_zero.size)).Using<Rect>(EquivalencyWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRect(localToWorld_negativeTranslate, rect_seven_seven),
                Is.EqualTo(new Rect(rect_seven_seven.position - localToWorld_negativeTranslateTranslation, rect_seven_seven.size)).Using<Rect>(EquivalencyWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRect(localToWorld_negativeTranslate, rect_zero_one),
                Is.EqualTo(new Rect(rect_zero_one.position - localToWorld_negativeTranslateTranslation, rect_zero_one.size)).Using<Rect>(EquivalencyWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRect(localToWorld_negativeTranslate, rect_seven_negativeSeven),
                Is.EqualTo(new Rect(rect_seven_negativeSeven.position - localToWorld_negativeTranslateTranslation, rect_seven_negativeSeven.size)).Using<Rect>(EquivalencyWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRect(localToWorld_negativeTranslate, rect_zero_oneSeven),
                Is.EqualTo(new Rect(rect_zero_oneSeven.position - localToWorld_negativeTranslateTranslation, rect_zero_oneSeven.size)).Using<Rect>(EquivalencyWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRect(localToWorld_negativeTranslate, rect_zero_sevenOne),
                Is.EqualTo(new Rect(rect_zero_sevenOne.position - localToWorld_negativeTranslateTranslation, rect_zero_sevenOne.size)).Using<Rect>(EquivalencyWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRect(localToWorld_negativeTranslate, rect_zero_negativeOneSeven),
                Is.EqualTo(new Rect(rect_zero_negativeOneSeven.position - localToWorld_negativeTranslateTranslation, rect_zero_negativeOneSeven.size)).Using<Rect>(EquivalencyWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRect(localToWorld_negativeTranslate, rect_zero_negativeSevenOne),
                Is.EqualTo(new Rect(rect_zero_negativeSevenOne.position - localToWorld_negativeTranslateTranslation, rect_zero_negativeSevenOne.size)).Using<Rect>(EquivalencyWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRect(localToWorld_negativeTranslate, rect_negativeOffset_seven),
                Is.EqualTo(new Rect(rect_negativeOffset_seven.position - localToWorld_negativeTranslateTranslation, rect_negativeOffset_seven.size)).Using<Rect>(EquivalencyWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRect(localToWorld_negativeTranslate, rect_negativeOffset_negativeSeven),
                Is.EqualTo(new Rect(rect_negativeOffset_negativeSeven.position - localToWorld_negativeTranslateTranslation, rect_negativeOffset_negativeSeven.size)).Using<Rect>(EquivalencyWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRect(localToWorld_negativeTranslate, rect_centered_seven),
                Is.EqualTo(new Rect(rect_centered_seven.position - localToWorld_negativeTranslateTranslation, rect_centered_seven.size)).Using<Rect>(EquivalencyWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRect(localToWorld_negativeTranslate, rect_centered_negativeSeven),
                Is.EqualTo(new Rect(rect_centered_negativeSeven.position - localToWorld_negativeTranslateTranslation, rect_centered_negativeSeven.size)).Using<Rect>(EquivalencyWithTolerance)
                );
            Assert.That(
                TransformUtil.ConvertWorldToLocalRect(localToWorld_negativeTranslate, rect_centered_negativePositiveSeven),
                Is.EqualTo(new Rect(rect_centered_negativePositiveSeven.position - localToWorld_negativeTranslateTranslation, rect_centered_negativePositiveSeven.size)).Using<Rect>(EquivalencyWithTolerance)
                );
        }

        // [Test]
        // public static Rect ConvertLocalToWorldRectTest()
        // {
        //     //TODO: #321 - Optimize...
        //     float4x4 worldToLocalMtx = math.inverse(localToWorld.Value);
        //
        //     float3 point1 = (Vector3)localRect.min;
        //     float3 point2 = (Vector3)localRect.max;
        //     float3 point3 = new float3(point1.x, point2.y, 0);
        //     float3 point4 = new float3(point2.x, point1.y, 0);
        //
        //     return RectUtil.CreateFromPoints(
        //         ConvertLocalToWorldPoint(worldToLocalMtx, point1).xy,
        //         ConvertLocalToWorldPoint(worldToLocalMtx, point2).xy,
        //         ConvertLocalToWorldPoint(worldToLocalMtx, point3).xy,
        //         ConvertLocalToWorldPoint(worldToLocalMtx, point4).xy
        //     );
        // }
    }
}
using Anvil.Unity.Core;
using NUnit.Framework;
using UnityEngine;

namespace Anvil.Unity.DOTS.Tests.Mathematics
{
    public static class MathExtensionTests
    {
        [Test]
        public static void CreateFromPointsTest()
        {
            Rect rect = RectUtil.CreateFromPoints(new Vector2(1, 2), new Vector2(3, 4));

            Assert.That(rect.min.x, Is.EqualTo(1));
            Assert.That(rect.min.y, Is.EqualTo(2));
            Assert.That(rect.max.x, Is.EqualTo(3));
            Assert.That(rect.max.y, Is.EqualTo(4));

            rect = RectUtil.CreateFromPoints(new Vector2(3, 4), new Vector2(1, 2));

            Assert.That(rect.min.x, Is.EqualTo(1));
            Assert.That(rect.min.y, Is.EqualTo(2));
            Assert.That(rect.max.x, Is.EqualTo(3));
            Assert.That(rect.max.y, Is.EqualTo(4));
        }
    }
}
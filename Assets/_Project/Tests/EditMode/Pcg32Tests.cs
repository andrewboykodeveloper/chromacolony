using System;
using Chromacolony.Core;
using NUnit.Framework;

namespace Chromacolony.Tests.EditMode
{
    public sealed class Pcg32Tests
    {
        [Test]
        public void TheSameSeedGivesTheSameNumbers()
        {
            var a = new Pcg32(42);
            var b = new Pcg32(42);

            for (int i = 0; i < 100; i++)
            {
                Assert.That(a.NextUInt(), Is.EqualTo(b.NextUInt()));
            }
        }

        [Test]
        public void ARandomRangeStaysWithinItsBounds()
        {
            var random = new Pcg32(7);

            for (int i = 0; i < 1000; i++)
            {
                Assert.That(random.Range(-3, 4), Is.InRange(-3, 3));
                Assert.That(random.Value(), Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
            }

            Assert.Throws<ArgumentException>(() => random.Range(2, 2));
        }
    }
}

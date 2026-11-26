using System.Collections.Generic;
using Chromacolony.Core;
using NUnit.Framework;

namespace Chromacolony.Tests.EditMode
{
    public sealed class FixedStepTickerTests
    {
        [Test]
        public void TheTickerRunsWholeStepsAndCarriesTheRestOver()
        {
            var ticker = new FixedStepTicker(0.25);
            var steps = new List<float>();

            Assert.That(ticker.Advance(0.625, steps.Add), Is.EqualTo(2));
            Assert.That(ticker.Advance(0.125, steps.Add), Is.EqualTo(1));
            Assert.That(steps, Is.EqualTo(new[] { 0.25f, 0.25f, 0.25f }));
        }

        [Test]
        public void TheTickerDropsTimeItCannotCatchUpWith()
        {
            var ticker = new FixedStepTicker(0.25, maxStepsPerAdvance: 3);

            Assert.That(ticker.Advance(10.0, _ => { }), Is.EqualTo(3));
            Assert.That(ticker.Advance(0.0, _ => { }), Is.EqualTo(0));
        }
    }
}

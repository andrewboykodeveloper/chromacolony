using System;
using System.Collections.Generic;
using Chromacolony.Core;
using Chromacolony.Foundation;
using NUnit.Framework;

namespace Chromacolony.Tests.EditMode
{
    public sealed class FoundationTests
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

        [Test]
        public void HandlersHearEventsInTheOrderTheySubscribedUntilTheyUnsubscribe()
        {
            var events = new EventBus();
            var heard = new List<string>();
            IDisposable first = events.Subscribe<BoardWon>(_ => heard.Add("first"));
            events.Subscribe<BoardWon>(_ => heard.Add("second"));

            events.Publish(new BoardWon());
            first.Dispose();
            events.Publish(new BoardWon());

            Assert.That(heard, Is.EqualTo(new[] { "first", "second", "second" }));
        }

        [Test]
        public void AStateMachineAllowsOnlyTheTransitionsItWasGiven()
        {
            StateMachine<GamePhase> machine = new StateMachine<GamePhase>(GamePhase.Menu).Allow(GamePhase.Menu, GamePhase.Playing);

            machine.Go(GamePhase.Playing);

            Assert.That(machine.Current, Is.EqualTo(GamePhase.Playing));
            Assert.That(machine.CanGo(GamePhase.Won), Is.False);
            Assert.Throws<InvalidOperationException>(() => machine.Go(GamePhase.Won));
        }
    }
}

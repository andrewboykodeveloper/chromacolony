using System;
using Chromacolony.Core;
using NUnit.Framework;

namespace Chromacolony.Tests.EditMode
{
    public sealed class StateMachineTests
    {
        [Test]
        public void AllowsOnlyItsTransitions()
        {
            StateMachine<GamePhase> machine = new StateMachine<GamePhase>(GamePhase.Menu).Allow(GamePhase.Menu, GamePhase.Playing);

            machine.Go(GamePhase.Playing);

            Assert.That(machine.Current, Is.EqualTo(GamePhase.Playing));
            Assert.That(machine.CanGo(GamePhase.Won), Is.False);
            Assert.Throws<InvalidOperationException>(() => machine.Go(GamePhase.Won));
        }
    }
}

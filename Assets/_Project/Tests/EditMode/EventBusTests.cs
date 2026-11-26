using System;
using System.Collections.Generic;
using Chromacolony.Core;
using NUnit.Framework;

namespace Chromacolony.Tests.EditMode
{
    public sealed class EventBusTests
    {
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
    }
}

using System.Collections.Generic;
using System.Linq;
using Chromacolony.Core;
using NUnit.Framework;

namespace Chromacolony.Tests.EditMode
{
    public sealed class HelperTests
    {
        private const float Step = 0.125f;
        private const string Ring = "board ring\nRRR\nRYR\nRRR\n= R8\n= Y1\n";

        private EventBus _events;

        [SetUp]
        public void NewEvents()
        {
            _events = new EventBus();
        }

        [Test]
        public void AnExtraSlotLetsAStuckBoardPlayOn()
        {
            GameModel model = Start(Ring, slots: 1);
            model.TrySend(1);
            model.Tick(Step);
            Assert.That(model.Phase, Is.EqualTo(GamePhase.Stuck));
            Assert.That(model.CanAddSlot, Is.True);

            model.AddSlot();

            Assert.That(model.Phase, Is.EqualTo(GamePhase.Playing));
            Assert.That(model.SlotCount, Is.EqualTo(2));
            Assert.That(model.TrySend(0), Is.True);
            for (int i = 0; i < 100 && model.Phase == GamePhase.Playing; i++)
            {
                model.Tick(Step);
            }

            Assert.That(model.Phase, Is.EqualTo(GamePhase.Won));
        }

        [Test]
        public void AStuckBoardOffersTheHelpersThatFreeASlotButNoShuffle()
        {
            GameModel model = Start("board ring\nRRR\nRYR\nRRR\n= R4 R4\n= Y1\n", slots: 1);
            Assert.That(model.CanShuffle, Is.True);

            model.TrySend(1);
            model.Tick(Step);

            Assert.That(model.Phase, Is.EqualTo(GamePhase.Stuck));
            Assert.That(model.CanShuffle, Is.False);
            Assert.That(model.CanAddSlot, Is.True);
            Assert.That(model.CanBack(0), Is.True);
        }

        [Test]
        public void AStuckBoardStaysStuckWhenAskedToShuffle()
        {
            GameModel model = Start("board ring\nRRR\nRYR\nRRR\n= R4 R4\n= Y1\n", slots: 1);
            model.TrySend(1);
            model.Tick(Step);
            Assert.That(model.Phase, Is.EqualTo(GamePhase.Stuck));
            bool shuffled = false;
            _events.Subscribe<QueueShuffled>(_ => shuffled = true);

            model.Shuffle(new Pcg32(7));

            Assert.That(shuffled, Is.False, "the queue was shuffled");
            Assert.That(model.Phase, Is.EqualTo(GamePhase.Stuck));
        }

        [Test]
        public void ABoardGetsOneExtraSlotAtMost()
        {
            GameModel model = Start(Ring, slots: 5);

            model.AddSlot();
            Assert.That(model.CanAddSlot, Is.False);
            model.AddSlot();

            Assert.That(model.SlotCount, Is.EqualTo(6));
        }

        [Test]
        public void BackFreesTheSlotOfAWaitingCrewAndUnsticksTheBoard()
        {
            GameModel model = Start(Ring, slots: 1);
            model.TrySend(1);
            model.Tick(Step);
            Assert.That(model.Phase, Is.EqualTo(GamePhase.Stuck));
            var backs = new List<CrewBack>();
            _events.Subscribe<CrewBack>(backs.Add);
            Assert.That(model.CanBack(0), Is.True);

            model.Back(0);

            Assert.That(model.Phase, Is.EqualTo(GamePhase.Playing));
            Assert.That(model.SlotAt(0), Is.Null);
            Assert.That(model.Column(1)[0].Waiting, Is.False);
            Assert.That(backs, Has.Count.EqualTo(1));
            Assert.That(model.TrySend(0), Is.True);
        }

        [Test]
        public void BackReturnsACrewToTheFrontOfItsColumnWithTheCrabsItHasLeft()
        {
            GameModel model = Start(Ring, slots: 5);
            model.TrySend(0);
            model.Tick(Step);
            model.Tick(Step);
            Assert.That(model.SlotAt(0).Remaining, Is.EqualTo(6));

            model.Back(0);

            Assert.That(model.SlotAt(0), Is.Null);
            Assert.That(model.Column(0)[0].Id, Is.EqualTo(0));
            Assert.That(model.Column(0)[0].Remaining, Is.EqualTo(6));
        }

        [Test]
        public void BackAfterAShuffleReturnsACrewToTheColumnItWasSentFrom()
        {
            GameModel model = Start("board s\nRRYY\n= R1 Y1\n= R1 Y1\n", slots: 5);
            // Crews 0 and 1 start in column 0, crews 2 and 3 in column 1; these draws swap the two front crews.
            model.Shuffle(new ScriptedRandom(3, 0, 1));
            Assert.That(model.Column(1)[0].Id, Is.EqualTo(0));

            model.TrySend(1);
            model.Back(0);

            Assert.That(model.Column(1)[0].Id, Is.EqualTo(0), "the crew is back at the front of column 1");
            Assert.That(model.Column(0), Has.Count.EqualTo(2), "column 0 keeps the two crews the shuffle left there");
        }

        [Test]
        public void BackCutsTheRopeOfACrewWhosePartnerHasGone()
        {
            GameModel model = Start("board rope\nRRYY\n= R2~1\n= Y2~1\n", slots: 5);

            model.TrySend(0);
            model.Back(0);

            Assert.That(model.Column(0)[0].Link, Is.EqualTo(-1));
            Assert.That(model.TrySend(0), Is.True);
        }

        [Test]
        public void AShuffleMovesTheCrewsButNotTheRopes()
        {
            GameModel model = Start("board s\nRRRRYYYYOOOOKK\n= R2 Y2 O2 K1~1\n= R2 Y2 O2 K1~1\n", slots: 5);
            string before = Order(model);

            model.Shuffle(new Pcg32(7));

            Assert.That(Order(model), Is.Not.EqualTo(before));
            Assert.That(model.Column(0)[3].Link, Is.EqualTo(1));
            Assert.That(model.Column(1)[3].Link, Is.EqualTo(1));
            Assert.That(model.Column(0).Count + model.Column(1).Count, Is.EqualTo(8));
        }

        [Test]
        public void AShuffleUncoversACrewThatComesToTheFront()
        {
            GameModel model = Start("board s\nRRRRRRYYYY\n= R2 ?Y2 ?Y2\n= R2 ?R2\n", slots: 5);
            List<int> covered = Enumerable.Range(0, model.ColumnCount).SelectMany(c => model.Column(c)).Where(crew => crew.Hidden).Select(crew => crew.Id).ToList();
            int uncovered = 0;

            for (uint seed = 1; seed < 40; seed++)
            {
                model.Start(model.Board);
                model.Shuffle(new Pcg32(seed));
                for (int c = 0; c < model.ColumnCount; c++)
                {
                    Crew front = model.Column(c)[0];
                    Assert.That(front.Hidden, Is.False, $"seed {seed}, column {c}");
                    uncovered += covered.Contains(front.Id) ? 1 : 0;
                }
            }

            Assert.That(uncovered, Is.GreaterThan(0), "some shuffles bring a covered crew to the front");
        }

        private GameModel Start(string board, int slots)
        {
            var model = new GameModel(_events, new Rules(slots, 0.25f));
            model.Start(BoardText.Parse(board)[0]);
            return model;
        }

        private static string Order(GameModel model)
        {
            return string.Join(",", Enumerable.Range(0, model.ColumnCount).SelectMany(column => model.Column(column)).Select(crew => crew.Id));
        }
    }
}

using System.Collections.Generic;
using Chromacolony.Core;
using NUnit.Framework;

namespace Chromacolony.Tests.EditMode
{
    public sealed class GameModelTests
    {
        private const float Interval = 0.25f;
        private const float Step = 0.125f;
        private const string Ring = "board ring\nRRR\nRYR\nRRR\n= R8\n= Y1\n";

        private List<string> _log;

        [SetUp]
        public void NewLog()
        {
            _log = new List<string>();
        }

        [Test]
        public void SentCrewTakesASlotAndLeavesItsColumn()
        {
            GameModel model = Start(Ring);

            Assert.That(model.TrySend(0), Is.True);

            Assert.That(_log, Is.EqualTo(new[] { "sent 0 col 0 slot 0" }));
            Assert.That(model.SlotAt(0).Id, Is.EqualTo(0));
            Assert.That(model.Column(0), Is.Empty);
            Assert.That(model.FreeSlots(), Is.EqualTo(4));
        }

        [Test]
        public void CrabsSetOffOneByOneAndTheCountFalls()
        {
            GameModel model = Start(Ring);
            model.TrySend(0);

            model.Tick(Step);
            Assert.That(model.SlotAt(0).Remaining, Is.EqualTo(7));
            model.Tick(Step);
            Assert.That(model.SlotAt(0).Remaining, Is.EqualTo(6));
            model.Tick(Step);
            Assert.That(model.SlotAt(0).Remaining, Is.EqualTo(6));
            Assert.That(_log, Is.EqualTo(new[] { "sent 0 col 0 slot 0", "crab 0 cell 7", "crab 0 cell 6" }));
        }

        [Test]
        public void CrewWaitsUntilAWayOpens()
        {
            GameModel model = Start(Ring);

            model.TrySend(1);
            model.Tick(Step);
            Assert.That(model.SlotAt(0).Waiting, Is.True);
            model.TrySend(0);
            model.Tick(Step);
            model.Tick(Step);

            Assert.That(_log, Is.EqualTo(new[]
            {
                "sent 1 col 1 slot 0", "waiting 1 True", "sent 0 col 0 slot 1", "crab 0 cell 7", "waiting 1 False", "crab 1 cell 4",
                "done 1 slot 0", "crab 0 cell 6",
            }));
            Assert.That(model.SlotAt(0), Is.Null);
        }

        [Test]
        public void EmptyMosaicIsAWin()
        {
            GameModel model = Start(Ring);

            model.TrySend(0);
            model.TrySend(1);
            Run(model, 10f);

            Assert.That(model.Phase, Is.EqualTo(GamePhase.Won));
            Assert.That(model.Mosaic.Left, Is.EqualTo(0));
            Assert.That(_log[_log.Count - 1], Is.EqualTo("won"));
            Assert.That(_log, Does.Contain("done 0 slot 0").And.Contain("done 1 slot 1"));
        }

        [Test]
        public void FullSlotsWithNoWayInAreStuck()
        {
            GameModel model = Start(Ring, slots: 1);

            model.TrySend(1);
            model.Tick(Step);

            Assert.That(model.Phase, Is.EqualTo(GamePhase.Stuck));
            Assert.That(_log, Is.EqualTo(new[] { "sent 1 col 1 slot 0", "waiting 1 True", "stuck" }));
            model.Tick(Step);
            Assert.That(model.Phase, Is.EqualTo(GamePhase.Stuck));
            Assert.That(model.TrySend(0), Is.False);
        }

        [Test]
        public void FreeSlotKeepsTheBoardGoing()
        {
            GameModel model = Start(Ring, slots: 2);

            model.TrySend(1);
            model.Tick(Step);

            Assert.That(model.Phase, Is.EqualTo(GamePhase.Playing));
        }

        [Test]
        public void SendWithoutAFreeSlotIsRefused()
        {
            GameModel model = Start(Ring, slots: 1);
            model.TrySend(1);

            Assert.That(model.TrySend(0), Is.False);

            Assert.That(_log[_log.Count - 1], Is.EqualTo("refused 0 NoRoom"));
            Assert.That(model.Column(0), Has.Count.EqualTo(1));
        }

        [Test]
        public void SendFromAnEmptyColumnIsRefused()
        {
            GameModel model = Start(Ring);
            model.TrySend(0);

            Assert.That(model.TrySend(0), Is.False);

            Assert.That(_log[_log.Count - 1], Is.EqualTo("refused 0 Empty"));
        }

        [Test]
        public void CrewsOnARopeGoTogetherIntoTwoSlots()
        {
            GameModel model = Start("board rope\nRRYY\n= R1 R1~1\n= Y2~1\n");

            Assert.That(model.TrySend(1), Is.False);
            Assert.That(_log[_log.Count - 1], Is.EqualTo("refused 1 PartnerNotReady"));
            Assert.That(model.TrySend(0), Is.True);
            Assert.That(model.TrySend(1), Is.True);

            Assert.That(_log, Is.EqualTo(new[] { "refused 1 PartnerNotReady", "sent 0 col 0 slot 0", "sent 2 col 1 slot 1", "sent 1 col 0 slot 2" }));
        }

        [Test]
        public void CrewsOnARopeNeedTwoFreeSlots()
        {
            GameModel model = Start("board rope\nRRYY\n= R2~1\n= Y2~1\n", slots: 1);

            Assert.That(model.TrySend(0), Is.False);

            Assert.That(_log[_log.Count - 1], Is.EqualTo("refused 0 NoRoom"));
        }

        [Test]
        public void CoveredCrewShowsItsColourAtTheFront()
        {
            GameModel model = Start("board cover\nRRRY\n= R3 ?Y1\n");
            Assert.That(model.Column(0)[1].Hidden, Is.True);

            model.TrySend(0);

            Assert.That(model.Column(0)[0].Hidden, Is.False);
            Assert.That(_log, Is.EqualTo(new[] { "sent 0 col 0 slot 0", "revealed 1" }));
        }

        [Test]
        public void FastPlaysAtTwiceTheSpeed()
        {
            GameModel slow = Start(Ring);
            slow.TrySend(0);
            Run(slow, 1f);
            Assert.That(slow.SlotAt(0), Is.Not.Null, "the slow crew is still at work");

            GameModel fast = Start(Ring);
            fast.Fast = true;
            fast.TrySend(0);
            Run(fast, 0.5f);

            Assert.That(fast.SlotAt(0).Remaining, Is.EqualTo(slow.SlotAt(0).Remaining));
        }

        [Test]
        public void StartingABoardAgainDealsItAnew()
        {
            GameModel model = Start(Ring);
            model.TrySend(0);
            Run(model, 1f);

            model.Start(model.Board);

            Assert.That(model.Mosaic.Left, Is.EqualTo(9));
            Assert.That(model.FreeSlots(), Is.EqualTo(5));
            Assert.That(model.Column(0), Has.Count.EqualTo(1));
        }

        [Test]
        public void CopyPlaysOnWithoutChangingItsOriginal()
        {
            GameModel model = Start(Ring);
            model.TrySend(0);

            GameModel copy = model.Copy();
            copy.Tick(Step);
            copy.TrySend(1);

            Assert.That(model.Mosaic.Left, Is.EqualTo(9));
            Assert.That(copy.Mosaic.Left, Is.EqualTo(8));
            Assert.That(model.Column(1), Has.Count.EqualTo(1));
            Assert.That(copy.Column(1), Is.Empty);
            Assert.That(model.SlotAt(0).Remaining, Is.EqualTo(8));
            Assert.That(copy.SlotAt(0).Remaining, Is.EqualTo(7));
        }

        private GameModel Start(string board, int slots = 5)
        {
            var model = new GameModel(new Rules(slots, Interval));
            model.CrewSent += e => _log.Add($"sent {e.Crew.Id} col {e.Column} slot {e.Slot}");
            model.CrabSent += e => _log.Add($"crab {e.Crew.Id} cell {e.Cell}");
            model.CrewWaiting += e => _log.Add($"waiting {e.Crew.Id} {e.Waiting}");
            model.CrewDone += e => _log.Add($"done {e.Crew.Id} slot {e.Slot}");
            model.CrewRevealed += e => _log.Add($"revealed {e.Crew.Id}");
            model.SendRefused += e => _log.Add($"refused {e.Column} {e.Reason}");
            model.BoardWon += () => _log.Add("won");
            model.BoardStuck += () => _log.Add("stuck");
            model.Start(BoardText.Parse(board)[0]);
            _log.Clear();
            return model;
        }

        private static void Run(GameModel model, float seconds)
        {
            for (float t = 0f; t < seconds && model.Phase == GamePhase.Playing; t += Step)
            {
                model.Tick(Step);
            }
        }
    }
}

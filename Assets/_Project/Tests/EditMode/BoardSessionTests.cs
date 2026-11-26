using System.Collections.Generic;
using Chromacolony.Core;
using NUnit.Framework;

namespace Chromacolony.Tests.EditMode
{
    public sealed class BoardSessionTests
    {
        private const float Step = 0.125f;
        private const string Ring = "board ring\nRRR\nRYR\nRRR\n= R8\n= Y1\n";

        // The fourth board is the first with a covered crew, so it teaches covered crews, and the crew opens its column.
        private const string Covered = "board covered\nRY\n= ?R1\n= Y1\n";

        private readonly Prices _prices = new Prices(coinsPerWin: 20, startStock: 1, slot: 30, shuffle: 20, back: 25);
        private readonly List<BoardEnded> _ends = new List<BoardEnded>();
        private GameModel _model;
        private Progress _progress;
        private BoardSession _session;

        [SetUp]
        public void MakeTwoBoards()
        {
            BoardSpec ring = BoardText.Parse(Ring)[0];
            _ends.Clear();
            _model = new GameModel(new Rules(5, 0.25f));
            _progress = Progress.New(_prices);
            _session = new BoardSession(_model, new[] { ring, ring }, _progress, _prices);
            _session.Ended += _ends.Add;
        }

        [Test]
        public void ABoardIsTakenByItsIndexRoundTheList()
        {
            _session.Start(3);

            Assert.That(_session.Board, Is.EqualTo(1));
            Assert.That(_model.Phase, Is.EqualTo(GamePhase.Playing));
        }

        [Test]
        public void EachBoardStartsWithItsLesson()
        {
            _session.Start(0);
            Assert.That(_session.Lesson, Is.EqualTo(Lesson.Tap));

            _session.Start(1);
            Assert.That(_session.Lesson, Is.EqualTo(Lesson.Slots));
        }

        [Test]
        public void AWinPaysItsCoinsAndSaysSo()
        {
            _session.Start(0);
            Win();

            Assert.That(_ends, Has.Count.EqualTo(1));
            Assert.That(_ends[0].Won, Is.True);
            Assert.That(_ends[0].Last, Is.False);
            Assert.That(_ends[0].CoinsBefore, Is.Zero);
            Assert.That(_ends[0].CoinsAfter, Is.EqualTo(20));
            Assert.That(_progress.Coins, Is.EqualTo(20));
            Assert.That(_progress.Board, Is.EqualTo(1), "the next board is up");
        }

        [Test]
        public void TheLastBoardsWinFinishesTheGame()
        {
            _session.Start(1);
            Win();

            Assert.That(_ends[0].Last, Is.True);
            Assert.That(_progress.Finished, Is.True);
            Assert.That(_progress.Board, Is.Zero);
        }

        [Test]
        public void AStuckBoardPaysNothing()
        {
            var model = new GameModel(new Rules(1, 0.25f));
            var session = new BoardSession(model, BoardText.Parse(Ring), _progress, _prices);
            session.Ended += _ends.Add;
            session.Start(0);

            model.TrySend(1);
            model.Tick(Step);

            Assert.That(_ends, Has.Count.EqualTo(1));
            Assert.That(_ends[0].Won, Is.False);
            Assert.That(_ends[0].CoinsAfter, Is.EqualTo(_ends[0].CoinsBefore));
            Assert.That(_progress.Coins, Is.Zero);
        }

        [Test]
        public void LeavingABoardSaysSo()
        {
            bool left = false;
            _session.Left += () => left = true;
            _session.Start(0);

            _model.BackToMenu();

            Assert.That(left, Is.True);
        }

        [Test]
        public void TheFirstTapEndsTheFirstLesson()
        {
            int changes = 0;
            _session.TutorialChanged += () => changes++;
            _session.Start(0);

            _model.TrySend(0);

            Assert.That(_session.Lesson, Is.EqualTo(Lesson.None));
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void AnExtraSlotChangesWhatTheLessonShows()
        {
            int changes = 0;
            _session.TutorialChanged += () => changes++;
            _session.Start(1);

            _model.AddSlot();

            Assert.That(_session.Lesson, Is.EqualTo(Lesson.Slots));
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void TheOpeningRevealsTeachNothing()
        {
            BoardSpec ring = BoardText.Parse(Ring)[0];
            var model = new GameModel(new Rules(5, 0.25f));
            var session = new BoardSession(model, new[] { ring, ring, ring, BoardText.Parse(Covered)[0] }, _progress, _prices);
            int changes = 0;
            session.TutorialChanged += () => changes++;

            session.Start(3);

            Assert.That(session.Lesson, Is.EqualTo(Lesson.Cover));
            Assert.That(changes, Is.Zero);
        }

        private void Win()
        {
            _model.TrySend(0);
            _model.TrySend(1);
            for (float t = 0f; t < 10f && _model.Phase == GamePhase.Playing; t += Step)
            {
                _model.Tick(Step);
            }

            Assert.That(_model.Phase, Is.EqualTo(GamePhase.Won));
        }
    }
}

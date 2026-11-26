using System.Collections.Generic;
using System.Linq;
using Chromacolony.Core;
using Chromacolony.Game;
using NUnit.Framework;

namespace Chromacolony.Tests.EditMode
{
    public sealed class ShippedBoardsTests
    {
        private const int BoardCount = 20;
        private const int SearchBudget = 20000;
        private const float SmallestPebble = 0.26f;

        private IReadOnlyList<BoardSpec> _boards;
        private Rules _rules;

        [OneTimeSetUp]
        public void LoadTheShippedData()
        {
            _boards = ShippedData.Boards();
            _rules = ShippedData.Rules();
        }

        [Test]
        public void TheGameShipsTwentyBoardsWithDistinctNames()
        {
            Assert.That(_boards, Has.Count.EqualTo(BoardCount));
            Assert.That(_boards.Select(board => board.Name), Is.Unique);
        }

        [Test]
        public void EveryBoardCanBeWonWithoutHelpers([Range(0, BoardCount - 1)] int index)
        {
            BoardSpec board = _boards[index];
            var solver = new Solver(_rules, SearchBudget);

            Solver.Result result = solver.Solve(board);

            Assert.That(result, Is.EqualTo(Solver.Result.Won), $"board {index + 1}, {board.Name}, after {solver.Positions} positions");
        }

        [Test]
        public void EveryBoardLaysOutWithPebblesLargeEnoughAndBucketsApart([Range(0, BoardCount - 1)] int index)
        {
            BoardSpec board = _boards[index];
            var layout = new BoardLayout(board.Width, board.Height, _rules.Slots, board.Columns.Count);

            // 0.26 world units is 26 pixels on the 1080-pixel-wide screen the layout is drawn for.
            Assert.That(layout.Cell, Is.GreaterThanOrEqualTo(SmallestPebble), $"board {index + 1}, {board.Name}");
            float gap = layout.QueueAt(1, 0).x - layout.QueueAt(0, 0).x;
            Assert.That(gap, Is.GreaterThanOrEqualTo(layout.BucketSize), $"board {index + 1}, {board.Name}");
        }

        [Test]
        public void EveryBoardOpensWithAFrontCrewThatCanStartAtOnce([Range(0, BoardCount - 1)] int index)
        {
            var model = new GameModel(new EventBus(), _rules);
            model.Start(_boards[index]);

            int ready = Enumerable.Range(0, model.ColumnCount).Count(c => model.Mosaic.AnyInReach(model.Column(c)[0].Color));

            // The first two boards teach the basics, so none of their first taps may have to wait.
            int expected = index < 2 ? model.ColumnCount : 1;
            Assert.That(ready, Is.GreaterThanOrEqualTo(expected), $"board {index + 1}, {_boards[index].Name}");
        }

        [Test]
        public void TheFirstRopeAndTheFirstCoverOfABoardAreInSightFromTheStart([Range(0, BoardCount - 1)] int index)
        {
            List<(CrewSpec Crew, int Depth)> crews = _boards[index].Columns.SelectMany(column => column.Select((crew, depth) => (Crew: crew, Depth: depth))).ToList();

            if (crews.Any(c => c.Crew.Link >= 0))
            {
                Assert.That(crews.Any(c => c.Crew.Link >= 0 && c.Depth < BoardLayout.VisibleDepth), "a rope in sight");
            }

            if (crews.Any(c => c.Crew.Hidden))
            {
                Assert.That(crews.Any(c => c.Crew.Hidden && c.Depth < BoardLayout.VisibleDepth), "a cover in sight");
            }
        }

        [Test]
        public void EachLessonComesOnTheBoardThatIntroducesItsIdea()
        {
            Lesson[] lessons = Enumerable.Range(0, _boards.Count).Select(index => Lessons.For(index, _boards)).ToArray();

            Assert.That(lessons[0], Is.EqualTo(Lesson.Tap));
            Assert.That(lessons[1], Is.EqualTo(Lesson.Slots));
            Assert.That(lessons[2], Is.EqualTo(Lesson.Inside));
            Assert.That(lessons[8], Is.EqualTo(Lesson.Rope));
            Assert.That(lessons[9], Is.EqualTo(Lesson.Cover));
            Assert.That(lessons.Count(lesson => lesson != Lesson.None), Is.EqualTo(5));
        }
    }
}

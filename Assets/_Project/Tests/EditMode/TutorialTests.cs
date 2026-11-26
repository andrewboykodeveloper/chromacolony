using Chromacolony.Core;
using NUnit.Framework;

namespace Chromacolony.Tests.EditMode
{
    public sealed class TutorialTests
    {
        // Column 0's colour is out of reach, columns 1 and 2 are tied by a rope, and columns 3 and 4 hold free crews in reach.
        private const string Board = "board fronts\nRRR\nRYR\nRRR\n= Y1\n= R1~1\n= R1~1\n= R5\n= R1\n";
        private const string Blocked = "board blocked\nRRR\nRYR\nRRR\n= Y1 R8\n";

        [TestCase(Lesson.Tap, true)]
        [TestCase(Lesson.Slots, true)]
        [TestCase(Lesson.Inside, true)]
        [TestCase(Lesson.Rope, false)]
        [TestCase(Lesson.Cover, false)]
        [TestCase(Lesson.None, false)]
        public void TheFirstThreeLessonsShowTheWayUntilTheFirstSend(Lesson lesson, bool pointing)
        {
            var tutorial = new Tutorial(lesson);
            Assert.That(tutorial.Pointing, Is.EqualTo(pointing));

            tutorial.OnCrewSent(Front(Start(Board), 4));

            Assert.That(tutorial.Pointing, Is.False);
        }

        [TestCase(Lesson.Tap, false, true)]
        [TestCase(Lesson.Tap, true, true)]
        [TestCase(Lesson.Rope, false, false)]
        [TestCase(Lesson.Rope, true, true)]
        [TestCase(Lesson.Slots, true, false)]
        [TestCase(Lesson.Inside, true, false)]
        [TestCase(Lesson.Cover, true, false)]
        public void ASendTeachesTapAndARopedSendTeachesRope(Lesson lesson, bool roped, bool learned)
        {
            var tutorial = new Tutorial(lesson);

            tutorial.OnCrewSent(Front(Start(Board), roped ? 1 : 4));

            Assert.That(tutorial.Lesson, Is.EqualTo(learned ? Lesson.None : lesson));
        }

        [TestCase(Lesson.Slots, true)]
        [TestCase(Lesson.Inside, true)]
        [TestCase(Lesson.Tap, false)]
        [TestCase(Lesson.Rope, false)]
        [TestCase(Lesson.Cover, false)]
        public void AFinishedCrewTeachesSlotsAndInside(Lesson lesson, bool learned)
        {
            var tutorial = new Tutorial(lesson);

            tutorial.OnCrewDone();

            Assert.That(tutorial.Lesson, Is.EqualTo(learned ? Lesson.None : lesson));
        }

        [TestCase(Lesson.Cover, true)]
        [TestCase(Lesson.Tap, false)]
        [TestCase(Lesson.Slots, false)]
        [TestCase(Lesson.Inside, false)]
        [TestCase(Lesson.Rope, false)]
        public void ARevealedCrewTeachesCover(Lesson lesson, bool learned)
        {
            var tutorial = new Tutorial(lesson);

            tutorial.OnCrewRevealed();

            Assert.That(tutorial.Lesson, Is.EqualTo(learned ? Lesson.None : lesson));
        }

        [Test]
        public void ARevealedCrewLeavesTheClawPointing()
        {
            var tutorial = new Tutorial(Lesson.Tap);

            tutorial.OnCrewRevealed();

            Assert.That(tutorial.Pointing, Is.True);
        }

        [Test]
        public void ALearnedLessonStaysLearned()
        {
            var tutorial = new Tutorial(Lesson.Slots);
            tutorial.OnCrewDone();

            tutorial.OnCrewDone();
            tutorial.OnCrewSent(Front(Start(Board), 4));

            Assert.That(tutorial.Lesson, Is.EqualTo(Lesson.None));
        }

        [Test]
        public void TheClawPointsAtTheFirstFreeCrewThatCanReachItsColour()
        {
            GameModel model = Start(Board);
            var tutorial = new Tutorial(Lesson.Tap);

            Assert.That(tutorial.PointedColumn(model), Is.EqualTo(3));

            model.TrySend(3);

            Assert.That(tutorial.PointedColumn(model), Is.EqualTo(4), "column 3 is empty now");
            Assert.That(tutorial.PointedColumn(Start(Blocked)), Is.EqualTo(-1), "no colour at the front is in reach");
            Assert.That(new Tutorial(Lesson.Rope).PointedColumn(model), Is.EqualTo(-1), "the rope lesson shows no claw");
        }

        private static GameModel Start(string board)
        {
            var model = new GameModel(new Rules(5, 0.25f));
            model.Start(BoardText.Parse(board)[0]);
            return model;
        }

        private static Crew Front(GameModel model, int column)
        {
            return model.Column(column)[0];
        }
    }
}

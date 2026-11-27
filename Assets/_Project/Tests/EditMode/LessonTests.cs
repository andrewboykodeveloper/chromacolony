using Chromacolony.Core;
using NUnit.Framework;

namespace Chromacolony.Tests.EditMode
{
    public sealed class LessonTests
    {
        private const string Boards =
            "board a\nR\n= R1\n" +
            "board b\nR\n= R1\n" +
            "board c\nR\n= R1\n" +
            "board d\nR\n= R1\n" +
            "board e\nRY\n= R1~1\n= Y1~1\n" +
            "board f\nRY\n= R1 ?Y1\n" +
            "board g\nRYY\n= R1~1 ?Y1\n= Y1~1\n";

        [TestCase(0, Lesson.Tap)]
        [TestCase(1, Lesson.Slots)]
        [TestCase(2, Lesson.Inside)]
        [TestCase(3, Lesson.None)]
        [TestCase(4, Lesson.Rope)]
        [TestCase(5, Lesson.Cover)]
        [TestCase(6, Lesson.None)]
        public void EachIdeaIsTaughtOnItsFirstBoard(int board, Lesson lesson)
        {
            Assert.That(Lessons.For(board, BoardText.Parse(Boards)), Is.EqualTo(lesson));
        }
    }
}

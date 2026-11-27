using System;
using System.Collections.Generic;
using Chromacolony.Core;
using NUnit.Framework;

namespace Chromacolony.Tests.EditMode
{
    public sealed class BoardTextTests
    {
        [Test]
        public void BoardIsReadWithItsMosaicAndItsQueue()
        {
            IReadOnlyList<BoardSpec> boards = BoardText.Parse("# a comment\nboard ring\nRRR\nRYR\nRRR\n= R8\n= ?Y1\n");

            Assert.That(boards, Has.Count.EqualTo(1));
            BoardSpec board = boards[0];
            Assert.That(board.Name, Is.EqualTo("ring"));
            Assert.That(board.Width, Is.EqualTo(3));
            Assert.That(board.Height, Is.EqualTo(3));
            Assert.That(board.Cells[4], Is.EqualTo(Palette.Of('Y')));
            Assert.That(board.Columns, Has.Count.EqualTo(2));
            Assert.That(board.Columns[0][0].Crabs, Is.EqualTo(8));
            Assert.That(board.Columns[1][0].Hidden, Is.True);
        }

        [Test]
        public void SeveralBoardsAndARopeAreRead()
        {
            IReadOnlyList<BoardSpec> boards = BoardText.Parse("board a\nRY\n= R1~3\n= Y1~3\nboard b\nR.\n= R1\n");

            Assert.That(boards, Has.Count.EqualTo(2));
            Assert.That(boards[0].Columns[0][0].Link, Is.EqualTo(3));
            Assert.That(boards[0].Columns[1][0].Link, Is.EqualTo(3));
            Assert.That(boards[1].Cells[1], Is.EqualTo(-1));
        }

        [Test]
        public void WindowsLineEndingsAreRead()
        {
            IReadOnlyList<BoardSpec> boards = BoardText.Parse("board a\r\nRY\r\n= R1 Y1\r\n");

            Assert.That(boards[0].Width, Is.EqualTo(2));
            Assert.That(boards[0].Columns[0], Has.Count.EqualTo(2));
        }

        [TestCase("board x\nRR\n= R1\n", TestName = "{m}(more pebbles than crabs)")]
        [TestCase("board x\nRY\n= R1 Y1~1\n", TestName = "{m}(a rope with one end)")]
        [TestCase("board x\nRY\n= R1~1 Y1~1\n", TestName = "{m}(a rope within one column)")]
        [TestCase("board x\nRZ\n= R1\n", TestName = "{m}(an unknown colour)")]
        [TestCase("board x\nRY\nR\n= R2 Y1\n", TestName = "{m}(rows of different widths)")]
        [TestCase("board x\nR\n= R0\n", TestName = "{m}(a crew without crabs)")]
        [TestCase("RR\nboard x\n= R2\n", TestName = "{m}(a row before the board line)")]
        [TestCase("board x\n= R1\nR\n", TestName = "{m}(a row after the queue)")]
        [TestCase("board x\n...\n=\n", TestName = "{m}(no pebbles, so an empty column)")]
        [TestCase("board x\nRY\n= R1 Y1\n=\n", TestName = "{m}(a column without crews)")]
        public void BoardThatDoesNotAddUpIsRejected(string text)
        {
            Assert.Throws<FormatException>(() => BoardText.Parse(text));
        }
    }
}

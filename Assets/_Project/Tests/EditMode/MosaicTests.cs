using System;
using System.Linq;
using Chromacolony.Core;
using NUnit.Framework;

namespace Chromacolony.Tests.EditMode
{
    public sealed class MosaicTests
    {
        [Test]
        public void TheEdgeIsInReachAndTheCoreIsNot()
        {
            Mosaic mosaic = MosaicOf("RRR", "RYR", "RRR");

            Assert.That(mosaic.InReach(0), Is.True);
            Assert.That(mosaic.InReach(7), Is.True);
            Assert.That(mosaic.InReach(4), Is.False);
            Assert.That(mosaic.Nearest(Palette.Of('Y')), Is.EqualTo(-1));
        }

        [Test]
        public void TakingAnOuterPebbleOpensTheWayIn()
        {
            Mosaic mosaic = MosaicOf("RRR", "RYR", "RRR");

            mosaic.Take(7);

            Assert.That(mosaic.InReach(4), Is.True);
            Assert.That(mosaic.Nearest(Palette.Of('Y')), Is.EqualTo(4));
            Assert.That(mosaic.Left, Is.EqualTo(8));
        }

        [Test]
        public void ThePebbleNearestTheCastleIsTakenFirst()
        {
            Mosaic mosaic = MosaicOf("RRR", "RRR", "RRR");

            Assert.That(mosaic.Nearest(Palette.Of('R')), Is.EqualTo(7));
            mosaic.Take(7);
            Assert.That(mosaic.Nearest(Palette.Of('R')), Is.EqualTo(6));
        }

        [Test]
        public void AnInnerGapOpensOnceAWayToItIsCleared()
        {
            Mosaic mosaic = MosaicOf("RRRRR", "R.Y.R", "R...R", "RRRRR");

            Assert.That(mosaic.InReach(7), Is.False);
            mosaic.Take(17);
            Assert.That(mosaic.InReach(7), Is.True);
        }

        [Test]
        public void APebbleOutOfReachCannotBeTaken()
        {
            Mosaic mosaic = MosaicOf("RRR", "RYR", "RRR");

            Assert.Throws<InvalidOperationException>(() => mosaic.Take(4));
        }

        [Test]
        public void ACopyChangesIndependentlyOfItsOriginal()
        {
            Mosaic mosaic = MosaicOf("RRR", "RYR", "RRR");
            Mosaic copy = mosaic.Copy();

            copy.Take(7);

            Assert.That(mosaic.Left, Is.EqualTo(9));
            Assert.That(mosaic.InReach(4), Is.False);
            Assert.That(copy.Left, Is.EqualTo(8));
            Assert.That(copy.InReach(4), Is.True);
        }

        private static Mosaic MosaicOf(params string[] rows)
        {
            string crews = string.Join(" ", string.Concat(rows).Where(c => c != '.').GroupBy(c => c).Select(g => $"{g.Key}{g.Count()}"));
            BoardSpec board = BoardText.Parse("board m\n" + string.Join("\n", rows) + "\n= " + crews + "\n")[0];
            return new Mosaic(board.Width, board.Height, board.Cells);
        }
    }
}

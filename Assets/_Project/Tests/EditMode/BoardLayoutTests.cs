using Chromacolony.Game;
using NUnit.Framework;
using UnityEngine;

namespace Chromacolony.Tests.EditMode
{
    public sealed class BoardLayoutTests
    {
        private const int Columns = 3;

        private readonly BoardLayout _layout = new BoardLayout(width: 13, height: 12, slots: 5, columns: Columns);

        [Test]
        public void ATapOnAnyVisibleCrewFindsItsColumn()
        {
            for (int column = 0; column < Columns; column++)
            {
                for (int depth = 0; depth < BoardLayout.VisibleDepth; depth++)
                {
                    Assert.That(_layout.ColumnUnder(_layout.QueueAt(column, depth)), Is.EqualTo(column), $"column {column}, depth {depth}");
                }
            }
        }

        [Test]
        public void TheQueuesReachThreeQuartersOfABucketAboveTheFrontCrewsAndBelowTheLastVisibleOnes()
        {
            Vector2 front = _layout.QueueAt(0, 0);
            Vector2 last = _layout.QueueAt(0, BoardLayout.VisibleDepth - 1);
            float bucket = _layout.BucketSize;

            Assert.That(_layout.ColumnUnder(front + new Vector2(0f, bucket * 0.7f)), Is.EqualTo(0), "just inside the top");
            Assert.That(_layout.ColumnUnder(front + new Vector2(0f, bucket * 0.8f)), Is.EqualTo(-1), "just above the top");
            Assert.That(_layout.ColumnUnder(last - new Vector2(0f, bucket * 0.7f)), Is.EqualTo(0), "just inside the bottom");
            Assert.That(_layout.ColumnUnder(last - new Vector2(0f, bucket * 0.8f)), Is.EqualTo(-1), "just below the bottom");
        }

        [Test]
        public void ATapBesideTheOuterColumnsFindsNoColumn()
        {
            float bucket = _layout.BucketSize;

            Assert.That(_layout.ColumnUnder(_layout.QueueAt(0, 0) - new Vector2(bucket * 1.1f, 0f)), Is.EqualTo(-1), "left of the first column");
            Assert.That(_layout.ColumnUnder(_layout.QueueAt(Columns - 1, 0) + new Vector2(bucket * 1.1f, 0f)), Is.EqualTo(-1),
                "right of the last column");
        }

        [Test]
        public void ATapThatTwoColumnsReachFindsTheNearerOne()
        {
            Vector2 first = _layout.QueueAt(0, 0);
            Vector2 second = _layout.QueueAt(1, 0);
            Assert.That(Vector2.Distance(Vector2.Lerp(first, second, 0.45f), second), Is.LessThan(_layout.BucketSize), "both columns reach");

            Assert.That(_layout.ColumnUnder(Vector2.Lerp(first, second, 0.45f)), Is.EqualTo(0));
            Assert.That(_layout.ColumnUnder(Vector2.Lerp(first, second, 0.55f)), Is.EqualTo(1));
        }

        [Test]
        public void ASlotCoversTheTapsNearItsCentreOnly()
        {
            Vector2 centre = _layout.SlotAt(2);

            Assert.That(_layout.SlotCovers(2, centre), Is.True);
            Assert.That(_layout.SlotCovers(2, centre + new Vector2(0f, _layout.SlotSize * 0.5f)), Is.True);
            Assert.That(_layout.SlotCovers(2, centre + new Vector2(0f, _layout.SlotSize * 0.7f)), Is.False);
            Assert.That(_layout.SlotCovers(1, centre), Is.False, "the next slot");
        }
    }
}

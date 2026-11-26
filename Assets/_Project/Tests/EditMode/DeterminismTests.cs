using System.Collections.Generic;
using System.Text;
using Chromacolony.Core;
using NUnit.Framework;

namespace Chromacolony.Tests.EditMode
{
    public sealed class DeterminismTests
    {
        private const float TickSeconds = 1f / 60f;
        private const int MaxTicks = 60 * 60 * 5;
        private const int TapEveryTicks = 11;
        private const int ShuffleAtTick = 45;
        private const ulong ShuffleSeed = 2026;

        [TestCase(2, GamePhase.Won, 303, TestName = "{m}(board 3)")]
        [TestCase(11, GamePhase.Stuck, 397, TestName = "{m}(board 12)")]
        [TestCase(19, GamePhase.Won, 1241, TestName = "{m}(board 20)")]
        public void TheSameTapsAtTheSameMomentsGiveTheSameBoard(int boardIndex, GamePhase outcome, int lastTick)
        {
            BoardSpec board = ShippedData.Boards()[boardIndex];
            Rules rules = ShippedData.Rules();

            List<string> first = Replay(board, rules);
            List<string> second = Replay(board, rules);

            Assert.That(second, Is.EqualTo(first));
            Assert.That(first, Does.Contain($"{ShuffleAtTick} shuffled"), "the seeded shuffle took place");
            Assert.That(first[first.Count - 1], Does.StartWith($"{lastTick} {outcome} "), "the board ends as it always has");
        }

        private static List<string> Replay(BoardSpec board, Rules rules)
        {
            var model = new GameModel(rules);
            var random = new Pcg32(ShuffleSeed);
            var log = new List<string>();
            int tick = 0;
            model.CrewSent += e => log.Add($"{tick} sent {e.Crew.Id} {e.Column} {e.Slot}");
            model.CrabSent += e => log.Add($"{tick} crab {e.Crew.Id} {e.Cell}");
            model.CrewDone += e => log.Add($"{tick} done {e.Crew.Id}");
            model.CrewRevealed += e => log.Add($"{tick} revealed {e.Crew.Id}");
            model.QueueShuffled += () => log.Add($"{tick} shuffled");
            model.BoardWon += () => log.Add($"{tick} won");
            model.BoardStuck += () => log.Add($"{tick} stuck");

            model.Start(board);
            for (; tick < MaxTicks && model.Phase == GamePhase.Playing; tick++)
            {
                if (tick % TapEveryTicks == 0)
                {
                    model.TrySend(tick / TapEveryTicks % model.ColumnCount);
                }

                if (tick == ShuffleAtTick)
                {
                    model.Shuffle(random);
                }

                model.Tick(TickSeconds);
            }

            log.Add($"{tick} {model.Phase} {model.Mosaic.Left} {Cells(model.Mosaic)}");
            return log;
        }

        private static string Cells(Mosaic mosaic)
        {
            var cells = new StringBuilder();
            for (int cell = 0; cell < mosaic.Width * mosaic.Height; cell++)
            {
                cells.Append(mosaic.ColorAt(cell) + 1);
            }

            return cells.ToString();
        }
    }
}

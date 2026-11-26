using System.Collections.Generic;
using System.Text;
using Chromacolony.Core;

namespace Chromacolony.Tests.EditMode
{
    internal sealed class Solver
    {
        public enum Result
        {
            Won,
            Lost,
            OutOfBudget,
        }

        private const float GameStep = 1f / 60f;
        private const int MaxWaitTicks = 100000;

        private readonly Rules _rules;
        private readonly int _budget;
        private readonly HashSet<string> _seen = new HashSet<string>();

        public int Positions { get; private set; }

        public Solver(Rules rules, int budget)
        {
            _rules = rules;
            _budget = budget;
        }

        private static int Rank(GameModel model, int column)
        {
            IReadOnlyList<Crew> crews = model.Column(column);
            return crews.Count > 0 && model.Mosaic.AnyInReach(crews[0].Color) ? 0 : 1;
        }

        private static bool AnyAtWork(GameModel model)
        {
            for (int s = 0; s < model.SlotCount; s++)
            {
                Crew crew = model.SlotAt(s);
                if (crew != null && crew.Remaining > 0 && model.Mosaic.AnyInReach(crew.Color))
                {
                    return true;
                }
            }

            return false;
        }

        private static string Key(GameModel model)
        {
            var key = new StringBuilder();
            for (int cell = 0; cell < model.Mosaic.Width * model.Mosaic.Height; cell++)
            {
                key.Append((char)('a' + model.Mosaic.ColorAt(cell) + 1));
            }

            key.Append('|');
            for (int s = 0; s < model.SlotCount; s++)
            {
                Crew crew = model.SlotAt(s);
                key.Append(crew == null ? "-" : crew.Id + ":" + crew.Remaining).Append(',');
            }

            key.Append('|');
            for (int c = 0; c < model.ColumnCount; c++)
            {
                key.Append(model.Column(c).Count).Append(',');
            }

            return key.ToString();
        }

        public Result Solve(BoardSpec board)
        {
            _seen.Clear();
            Positions = 0;
            var model = new GameModel(new EventBus(), _rules);
            model.Start(board);
            return Search(model);
        }

        private Result Search(GameModel model)
        {
            if (model.Phase == GamePhase.Won)
            {
                return Result.Won;
            }

            if (model.Phase != GamePhase.Playing)
            {
                return Result.Lost;
            }

            if (++Positions > _budget)
            {
                return Result.OutOfBudget;
            }

            if (!_seen.Add(Key(model)))
            {
                return Result.Lost;
            }

            var columns = new List<int>();
            for (int c = 0; c < model.ColumnCount; c++)
            {
                columns.Add(c);
            }

            // Crews that can start working at once go first, which keeps the search short.
            columns.Sort((a, b) => Rank(model, a).CompareTo(Rank(model, b)));
            foreach (int column in columns)
            {
                GameModel next = model.Copy();
                if (!next.TrySend(column))
                {
                    continue;
                }

                Result result = Search(next);
                if (result != Result.Lost)
                {
                    return result;
                }
            }

            if (!AnyAtWork(model))
            {
                return Result.Lost;
            }

            GameModel waited = model.Copy();
            Wait(waited);
            return Search(waited);
        }

        // Waiting can only matter until a slot frees up or no crew in a slot can take a pebble, so the wait stops there.
        private void Wait(GameModel model)
        {
            int free = model.FreeSlots();
            for (int i = 0; i < MaxWaitTicks && model.Phase == GamePhase.Playing; i++)
            {
                model.Tick(GameStep);
                if (model.FreeSlots() > free || !AnyAtWork(model))
                {
                    return;
                }
            }
        }
    }
}

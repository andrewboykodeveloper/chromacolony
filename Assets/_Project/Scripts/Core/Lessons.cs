using System.Collections.Generic;

namespace Chromacolony.Core
{
    public enum Lesson
    {
        None,

        Tap,

        Slots,

        Inside,

        Rope,

        Cover,
    }

    public static class Lessons
    {
        public static Lesson For(int board, IReadOnlyList<BoardSpec> boards)
        {
            switch (board)
            {
                case 0:
                    return Lesson.Tap;
                case 1:
                    return Lesson.Slots;
                case 2:
                    return Lesson.Inside;
            }

            if (Has(boards[board], rope: true) && FirstWith(boards, rope: true) == board)
            {
                return Lesson.Rope;
            }

            if (Has(boards[board], rope: false) && FirstWith(boards, rope: false) == board)
            {
                return Lesson.Cover;
            }

            return Lesson.None;
        }

        private static int FirstWith(IReadOnlyList<BoardSpec> boards, bool rope)
        {
            for (int i = 0; i < boards.Count; i++)
            {
                if (Has(boards[i], rope))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool Has(BoardSpec board, bool rope)
        {
            foreach (IReadOnlyList<CrewSpec> column in board.Columns)
            {
                foreach (CrewSpec crew in column)
                {
                    if (rope ? crew.Link >= 0 : crew.Hidden)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}

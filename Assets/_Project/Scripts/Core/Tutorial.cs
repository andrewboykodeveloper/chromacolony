using System.Collections.Generic;

namespace Chromacolony.Core
{
    public sealed class Tutorial
    {
        public Lesson Lesson { get; private set; }

        public bool Pointing { get; private set; }

        public Tutorial(Lesson lesson)
        {
            Lesson = lesson;
            Pointing = lesson is Lesson.Tap or Lesson.Slots or Lesson.Inside;
        }

        /// <summary>
        /// The column the claw points at: the first whose front crew could go on its own and find a pebble of its colour in
        /// reach, or -1 while the claw is hidden or no column is ready.
        /// </summary>
        public int PointedColumn(GameModel model)
        {
            if (!Pointing)
            {
                return -1;
            }

            for (int c = 0; c < model.ColumnCount; c++)
            {
                IReadOnlyList<Crew> column = model.Column(c);
                if (column.Count > 0 && column[0].Link < 0 && model.Mosaic.AnyInReach(column[0].Color))
                {
                    return c;
                }
            }

            return -1;
        }

        public void OnCrewSent(Crew crew)
        {
            Pointing = false;
            if (Lesson is Lesson.Tap || (Lesson is Lesson.Rope && crew.Link >= 0))
            {
                Learn();
            }
        }

        public void OnCrewDone()
        {
            if (Lesson is Lesson.Slots or Lesson.Inside)
            {
                Learn();
            }
        }

        public void OnCrewRevealed()
        {
            if (Lesson is Lesson.Cover)
            {
                Learn();
            }
        }

        private void Learn()
        {
            Lesson = Lesson.None;
            Pointing = false;
        }
    }
}

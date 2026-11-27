using System;
using System.Collections.Generic;

namespace Chromacolony.Core
{
    /// <summary>
    /// The board in play: starts it, keeps its tutorial up to date, and pays a win when it ends.
    /// </summary>
    public sealed class BoardSession
    {
        private readonly GameModel _model;
        private readonly IReadOnlyList<BoardSpec> _boards;
        private readonly Progress _progress;
        private readonly Prices _prices;
        private Tutorial _tutorial = new Tutorial(Lesson.None);
        private bool _starting;

        public event Action Started;

        public event Action<BoardEnded> Ended;

        public event Action Left;

        public event Action TutorialChanged;

        public int Board { get; private set; }

        public Lesson Lesson => _tutorial.Lesson;

        public BoardSession(GameModel model, IReadOnlyList<BoardSpec> boards, Progress progress, Prices prices)
        {
            _model = model;
            _boards = boards;
            _progress = progress;
            _prices = prices;

            model.BoardWon += () => End(true);
            model.BoardStuck += () => End(false);
            model.BoardLeft += () => Left?.Invoke();
            model.CrewSent += e => OnCrewSent(e.Crew);
            model.CrewDone += _ => OnCrewDone();
            model.CrewRevealed += _ => OnCrewRevealed();
            model.QueueShuffled += OnBoardMoved;

            // The lesson on slots shows how many there are.
            model.SlotAdded += _ => OnBoardMoved();
        }

        /// <summary>
        /// The column the tutorial's pointer shows, or -1 while the pointer is hidden or no column is ready.
        /// </summary>
        public int PointedColumn()
        {
            return _tutorial.PointedColumn(_model);
        }

        public void Start(int board)
        {
            Board = board % _boards.Count;

            // The model reveals the opening crews as it starts. The board's tutorial comes after, so those reveals neither teach
            // its lesson nor count as a change to it.
            _starting = true;
            try
            {
                _model.Start(_boards[Board]);
            }
            finally
            {
                _starting = false;
            }

            _model.Fast = _progress.Fast;
            _tutorial = new Tutorial(Lessons.For(Board, _boards));
            Started?.Invoke();
        }

        private void OnCrewSent(Crew crew)
        {
            _tutorial.OnCrewSent(crew);
            TutorialChanged?.Invoke();
        }

        private void OnCrewDone()
        {
            _tutorial.OnCrewDone();
            TutorialChanged?.Invoke();
        }

        private void OnCrewRevealed()
        {
            if (_starting)
            {
                return;
            }

            _tutorial.OnCrewRevealed();
            TutorialChanged?.Invoke();
        }

        private void OnBoardMoved()
        {
            TutorialChanged?.Invoke();
        }

        private void End(bool won)
        {
            int coinsBefore = _progress.Coins;
            if (won)
            {
                _progress.Won(Board, _boards.Count, _prices);
            }

            Ended?.Invoke(new BoardEnded(won, Board == _boards.Count - 1, coinsBefore, _progress.Coins));
        }
    }
}

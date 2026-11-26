using System;
using System.Collections.Generic;

namespace Chromacolony.Core
{
    public sealed class GameModel
    {
        private readonly StateMachine<GamePhase> _phase = new StateMachine<GamePhase>(GamePhase.Menu)
            .Allow(GamePhase.Menu, GamePhase.Playing)
            .Allow(GamePhase.Playing, GamePhase.Playing)
            .Allow(GamePhase.Playing, GamePhase.Won)
            .Allow(GamePhase.Playing, GamePhase.Stuck)
            .Allow(GamePhase.Playing, GamePhase.Menu)
            .Allow(GamePhase.Won, GamePhase.Playing)
            .Allow(GamePhase.Won, GamePhase.Menu)
            .Allow(GamePhase.Stuck, GamePhase.Playing)
            .Allow(GamePhase.Stuck, GamePhase.Menu);

        private readonly Rules _rules;
        private readonly List<List<Crew>> _columns = new List<List<Crew>>();
        private Crew[] _slots = new Crew[0];

        public event Action<BoardStarted> BoardStarted;

        public event Action<CrewSent> CrewSent;

        public event Action<CrabSent> CrabSent;

        public event Action<CrewWaiting> CrewWaiting;

        public event Action<CrewDone> CrewDone;

        public event Action<CrewRevealed> CrewRevealed;

        public event Action<SendRefused> SendRefused;

        public event Action<SlotAdded> SlotAdded;

        public event Action QueueShuffled;

        public event Action<CrewBack> CrewBack;

        public event Action BoardWon;

        public event Action BoardStuck;

        public GamePhase Phase => _phase.Current;

        public BoardSpec Board { get; private set; }

        public Mosaic Mosaic { get; private set; }

        public bool Fast { get; set; }

        public int SlotCount { get; private set; }

        public int ColumnCount => _columns.Count;

        private bool CanAddSlot => InPlay && SlotCount < _slots.Length;

        private bool CanShuffle => Phase == GamePhase.Playing && Shufflable().Count >= 2;

        private bool InPlay => Phase == GamePhase.Playing || Phase == GamePhase.Stuck;

        public GameModel(Rules rules)
        {
            _rules = rules;
        }

        public Crew SlotAt(int slot)
        {
            return _slots[slot];
        }

        public IReadOnlyList<Crew> Column(int column)
        {
            return _columns[column];
        }

        public void Start(BoardSpec board)
        {
            _phase.Go(GamePhase.Playing);
            Board = board;
            Mosaic = new Mosaic(board.Width, board.Height, board.Cells);
            SlotCount = _rules.Slots;
            _slots = new Crew[_rules.Slots + 1];
            _columns.Clear();
            int id = 0;
            for (int c = 0; c < board.Columns.Count; c++)
            {
                var column = new List<Crew>();
                foreach (CrewSpec spec in board.Columns[c])
                {
                    column.Add(new Crew(id++, spec, c));
                }

                _columns.Add(column);
            }

            BoardStarted?.Invoke(new BoardStarted(board));
            for (int c = 0; c < _columns.Count; c++)
            {
                RevealFront(c);
            }
        }

        public GameModel Copy()
        {
            var copy = new GameModel(_rules);
            if (Phase != GamePhase.Menu)
            {
                copy._phase.Go(GamePhase.Playing);
                if (Phase != GamePhase.Playing)
                {
                    copy._phase.Go(Phase);
                }
            }

            copy.Board = Board;
            copy.Mosaic = Mosaic?.Copy();
            copy.Fast = Fast;
            copy.SlotCount = SlotCount;
            var crews = new Dictionary<Crew, Crew>();
            copy._slots = new Crew[_slots.Length];
            for (int s = 0; s < _slots.Length; s++)
            {
                copy._slots[s] = CopyOf(_slots[s], crews);
            }

            foreach (List<Crew> column in _columns)
            {
                var copied = new List<Crew>(column.Count);
                foreach (Crew crew in column)
                {
                    copied.Add(CopyOf(crew, crews));
                }

                copy._columns.Add(copied);
            }

            return copy;
        }

        public void BackToMenu()
        {
            if (Phase != GamePhase.Menu)
            {
                _phase.Go(GamePhase.Menu);
            }
        }

        public bool TrySend(int column)
        {
            if (Phase != GamePhase.Playing || column < 0 || column >= _columns.Count)
            {
                return false;
            }

            if (_columns[column].Count == 0)
            {
                return Refuse(column, Refusal.Empty);
            }

            Crew crew = _columns[column][0];
            int partner = -1;
            if (crew.Link >= 0)
            {
                partner = PartnerColumn(crew, column);
                if (partner < 0)
                {
                    return Refuse(column, Refusal.PartnerNotReady);
                }
            }

            if (FreeSlots() < (partner >= 0 ? 2 : 1))
            {
                return Refuse(column, Refusal.NoRoom);
            }

            Place(column);
            if (partner >= 0)
            {
                Place(partner);
            }

            return true;
        }

        public void Tick(float dt)
        {
            if (Phase != GamePhase.Playing)
            {
                return;
            }

            float step = Fast ? dt * 2f : dt;
            for (int s = 0; s < SlotCount; s++)
            {
                Crew crew = _slots[s];
                if (crew == null)
                {
                    continue;
                }

                crew.Cooldown -= step;
                while (crew.Remaining > 0 && crew.Cooldown <= 0f)
                {
                    int cell = Mosaic.Nearest(crew.Color);
                    if (cell < 0)
                    {
                        crew.Cooldown = 0f;
                        SetWaiting(crew, true);
                        break;
                    }

                    SetWaiting(crew, false);
                    Mosaic.Take(cell);
                    crew.Remaining--;
                    crew.Cooldown += _rules.CrabInterval;
                    CrabSent?.Invoke(new CrabSent(crew, s, cell));
                }

                if (crew.Remaining == 0)
                {
                    _slots[s] = null;
                    CrewDone?.Invoke(new CrewDone(crew, s));
                }
            }

            if (Mosaic.Left == 0)
            {
                _phase.Go(GamePhase.Won);
                BoardWon?.Invoke();
            }
            else if (!CanProgress() && !CanSendAny())
            {
                _phase.Go(GamePhase.Stuck);
                BoardStuck?.Invoke();
            }
        }

        public bool CanBack(int slot)
        {
            return InPlay && slot >= 0 && slot < SlotCount && _slots[slot] != null;
        }

        private bool AnyToBack()
        {
            for (int s = 0; s < SlotCount; s++)
            {
                if (CanBack(s))
                {
                    return true;
                }
            }

            return false;
        }

        public bool CanUse(Helper helper)
        {
            return helper switch
            {
                Helper.Slot => CanAddSlot,
                Helper.Shuffle => CanShuffle,
                Helper.Back => AnyToBack(),
                _ => throw new ArgumentOutOfRangeException(nameof(helper), helper, "no such helper"),
            };
        }

        public void AddSlot()
        {
            if (!CanAddSlot)
            {
                return;
            }

            SlotCount++;
            SlotAdded?.Invoke(new SlotAdded(SlotCount - 1));
            PlayOn();
        }

        public void Shuffle(IRandom random)
        {
            if (!CanShuffle)
            {
                return;
            }

            List<(int Column, int Depth)> places = Shufflable();
            var crews = new List<Crew>(places.Count);
            foreach ((int column, int depth) in places)
            {
                crews.Add(_columns[column][depth]);
            }

            for (int i = crews.Count - 1; i > 0; i--)
            {
                int j = random.Range(0, i + 1);
                Crew swap = crews[i];
                crews[i] = crews[j];
                crews[j] = swap;
            }

            for (int i = 0; i < places.Count; i++)
            {
                _columns[places[i].Column][places[i].Depth] = crews[i];
                crews[i].Column = places[i].Column;
            }

            QueueShuffled?.Invoke();
            for (int c = 0; c < _columns.Count; c++)
            {
                RevealFront(c);
            }
        }

        public void Back(int slot)
        {
            if (!CanBack(slot))
            {
                return;
            }

            Crew crew = _slots[slot];
            _slots[slot] = null;
            crew.Waiting = false;
            crew.Link = -1;
            _columns[crew.Column].Insert(0, crew);
            CrewBack?.Invoke(new CrewBack(crew));
            PlayOn();
        }

        public int FreeSlots()
        {
            int free = 0;
            for (int s = 0; s < SlotCount; s++)
            {
                if (_slots[s] == null)
                {
                    free++;
                }
            }

            return free;
        }

        private void PlayOn()
        {
            if (Phase == GamePhase.Stuck)
            {
                _phase.Go(GamePhase.Playing);
            }
        }

        private List<(int Column, int Depth)> Shufflable()
        {
            var places = new List<(int Column, int Depth)>();
            for (int c = 0; c < _columns.Count; c++)
            {
                for (int d = 0; d < _columns[c].Count; d++)
                {
                    if (_columns[c][d].Link < 0)
                    {
                        places.Add((c, d));
                    }
                }
            }

            return places;
        }

        private static Crew CopyOf(Crew crew, Dictionary<Crew, Crew> copies)
        {
            if (crew == null)
            {
                return null;
            }

            if (!copies.TryGetValue(crew, out Crew copy))
            {
                copies[crew] = copy = new Crew(crew);
            }

            return copy;
        }

        private bool Refuse(int column, Refusal reason)
        {
            SendRefused?.Invoke(new SendRefused(column, reason));
            return false;
        }

        private void Place(int column)
        {
            Crew crew = _columns[column][0];
            int slot = 0;
            while (_slots[slot] != null)
            {
                slot++;
            }

            _columns[column].RemoveAt(0);
            _slots[slot] = crew;
            crew.Hidden = false;
            crew.Cooldown = 0f;
            CrewSent?.Invoke(new CrewSent(crew, column, slot));
            RevealFront(column);
        }

        private void RevealFront(int column)
        {
            if (_columns[column].Count == 0)
            {
                return;
            }

            Crew front = _columns[column][0];
            if (front.Hidden)
            {
                front.Hidden = false;
                CrewRevealed?.Invoke(new CrewRevealed(front));
            }
        }

        private int PartnerColumn(Crew crew, int column)
        {
            for (int c = 0; c < _columns.Count; c++)
            {
                if (c != column && _columns[c].Count > 0 && _columns[c][0].Link == crew.Link)
                {
                    return c;
                }
            }

            return -1;
        }

        private void SetWaiting(Crew crew, bool waiting)
        {
            if (crew.Waiting != waiting)
            {
                crew.Waiting = waiting;
                CrewWaiting?.Invoke(new CrewWaiting(crew, waiting));
            }
        }

        private bool CanProgress()
        {
            for (int s = 0; s < SlotCount; s++)
            {
                Crew crew = _slots[s];
                if (crew != null && crew.Remaining > 0 && Mosaic.AnyInReach(crew.Color))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CanSendAny()
        {
            int free = FreeSlots();
            for (int c = 0; c < _columns.Count; c++)
            {
                if (_columns[c].Count == 0)
                {
                    continue;
                }

                Crew crew = _columns[c][0];
                if (crew.Link < 0 ? free >= 1 : free >= 2 && PartnerColumn(crew, c) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

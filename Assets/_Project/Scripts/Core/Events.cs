namespace Chromacolony.Core
{
    public readonly struct BoardStarted
    {
        public BoardStarted(BoardSpec board)
        {
            Board = board;
        }

        public BoardSpec Board { get; }
    }

    public readonly struct CrewSent
    {
        public CrewSent(Crew crew, int column, int slot)
        {
            Crew = crew;
            Column = column;
            Slot = slot;
        }

        public Crew Crew { get; }

        public int Column { get; }

        public int Slot { get; }
    }

    public readonly struct CrabSent
    {
        public CrabSent(Crew crew, int slot, int cell)
        {
            Crew = crew;
            Slot = slot;
            Cell = cell;
        }

        public Crew Crew { get; }

        public int Slot { get; }

        public int Cell { get; }
    }

    public readonly struct CrewWaiting
    {
        public CrewWaiting(Crew crew, bool waiting)
        {
            Crew = crew;
            Waiting = waiting;
        }

        public Crew Crew { get; }

        public bool Waiting { get; }
    }

    public readonly struct CrewDone
    {
        public CrewDone(Crew crew, int slot)
        {
            Crew = crew;
            Slot = slot;
        }

        public Crew Crew { get; }

        public int Slot { get; }
    }

    public readonly struct CrewRevealed
    {
        public CrewRevealed(Crew crew, int column)
        {
            Crew = crew;
            Column = column;
        }

        public Crew Crew { get; }

        public int Column { get; }
    }

    public enum Refusal
    {
        Empty,

        NoRoom,

        PartnerNotReady,
    }

    public readonly struct SendRefused
    {
        public SendRefused(int column, Refusal reason)
        {
            Column = column;
            Reason = reason;
        }

        public int Column { get; }

        public Refusal Reason { get; }
    }

    public readonly struct SlotAdded
    {
        public SlotAdded(int slot)
        {
            Slot = slot;
        }

        public int Slot { get; }
    }

    public readonly struct QueueShuffled
    {
    }

    public readonly struct CrewBack
    {
        public CrewBack(Crew crew, int slot, int column)
        {
            Crew = crew;
            Slot = slot;
            Column = column;
        }

        public Crew Crew { get; }

        public int Slot { get; }

        public int Column { get; }
    }

    public readonly struct BoardWon
    {
    }

    public readonly struct BoardStuck
    {
    }
}

namespace Chromacolony.Core
{
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
}

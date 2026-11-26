namespace Chromacolony.Core
{
    public readonly struct CrewSent
    {
        public Crew Crew { get; }

        public int Column { get; }

        public int Slot { get; }

        public CrewSent(Crew crew, int column, int slot)
        {
            Crew = crew;
            Column = column;
            Slot = slot;
        }
    }
}

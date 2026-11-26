namespace Chromacolony.Core
{
    public readonly struct CrewBack
    {
        public Crew Crew { get; }

        public int Slot { get; }

        public int Column { get; }

        public CrewBack(Crew crew, int slot, int column)
        {
            Crew = crew;
            Slot = slot;
            Column = column;
        }
    }
}

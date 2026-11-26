namespace Chromacolony.Core
{
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
}

namespace Chromacolony.Core
{
    public readonly struct CrabSent
    {
        public Crew Crew { get; }

        public int Slot { get; }

        public int Cell { get; }

        public CrabSent(Crew crew, int slot, int cell)
        {
            Crew = crew;
            Slot = slot;
            Cell = cell;
        }
    }
}

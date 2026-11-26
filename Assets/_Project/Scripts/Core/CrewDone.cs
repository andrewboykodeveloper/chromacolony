namespace Chromacolony.Core
{
    public readonly struct CrewDone
    {
        public Crew Crew { get; }

        public int Slot { get; }

        public CrewDone(Crew crew, int slot)
        {
            Crew = crew;
            Slot = slot;
        }
    }
}

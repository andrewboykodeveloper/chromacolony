namespace Chromacolony.Core
{
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
}

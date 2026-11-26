namespace Chromacolony.Core
{
    public readonly struct CrewRevealed
    {
        public Crew Crew { get; }

        public CrewRevealed(Crew crew)
        {
            Crew = crew;
        }
    }
}

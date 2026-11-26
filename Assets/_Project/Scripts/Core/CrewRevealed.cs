namespace Chromacolony.Core
{
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
}

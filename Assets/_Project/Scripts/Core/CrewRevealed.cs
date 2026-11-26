namespace Chromacolony.Core
{
    public readonly struct CrewRevealed
    {
        public Crew Crew { get; }

        public int Column { get; }

        public CrewRevealed(Crew crew, int column)
        {
            Crew = crew;
            Column = column;
        }
    }
}

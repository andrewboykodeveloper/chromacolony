namespace Chromacolony.Core
{
    public readonly struct CrewWaiting
    {
        public CrewWaiting(Crew crew, bool waiting)
        {
            Crew = crew;
            Waiting = waiting;
        }

        public Crew Crew { get; }

        public bool Waiting { get; }
    }
}

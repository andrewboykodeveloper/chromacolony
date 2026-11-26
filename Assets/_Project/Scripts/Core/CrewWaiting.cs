namespace Chromacolony.Core
{
    public readonly struct CrewWaiting
    {
        public Crew Crew { get; }

        public bool Waiting { get; }

        public CrewWaiting(Crew crew, bool waiting)
        {
            Crew = crew;
            Waiting = waiting;
        }
    }
}

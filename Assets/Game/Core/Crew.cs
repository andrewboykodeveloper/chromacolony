namespace Chromacolony.Core
{
    public sealed class Crew
    {
        internal Crew(int id, CrewSpec spec, int column)
        {
            Id = id;
            Color = spec.Color;
            Crabs = spec.Crabs;
            Remaining = spec.Crabs;
            Link = spec.Link;
            Hidden = spec.Hidden;
            Column = column;
        }

        internal Crew(Crew other)
        {
            Id = other.Id;
            Color = other.Color;
            Crabs = other.Crabs;
            Remaining = other.Remaining;
            Link = other.Link;
            Hidden = other.Hidden;
            Waiting = other.Waiting;
            Column = other.Column;
            Cooldown = other.Cooldown;
        }

        public int Id { get; }

        public int Color { get; }

        public int Crabs { get; }

        public int Remaining { get; internal set; }

        public int Link { get; internal set; }

        public bool Hidden { get; internal set; }

        public bool Waiting { get; internal set; }

        public int Column { get; }

        internal float Cooldown { get; set; }
    }
}

namespace Chromacolony.Core
{
    public sealed class CrewSpec
    {
        public int Color { get; }

        public int Crabs { get; }

        public int Link { get; }

        public bool Hidden { get; }

        public CrewSpec(int color, int crabs, int link = -1, bool hidden = false)
        {
            Color = color;
            Crabs = crabs;
            Link = link;
            Hidden = hidden;
        }
    }
}

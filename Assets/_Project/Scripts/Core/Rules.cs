namespace Chromacolony.Core
{
    public sealed class Rules
    {
        public int Slots { get; }

        public float CrabInterval { get; }

        public Rules(int slots, float crabInterval)
        {
            Slots = slots;
            CrabInterval = crabInterval;
        }
    }
}

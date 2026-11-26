namespace Chromacolony.Core
{
    public sealed class Rules
    {
        public Rules(int slots, float crabInterval)
        {
            Slots = slots;
            CrabInterval = crabInterval;
        }

        public int Slots { get; }

        public float CrabInterval { get; }
    }
}

namespace Chromacolony.Core
{
    public readonly struct SlotAdded
    {
        public SlotAdded(int slot)
        {
            Slot = slot;
        }

        public int Slot { get; }
    }
}

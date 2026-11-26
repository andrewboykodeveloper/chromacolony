namespace Chromacolony.Core
{
    public readonly struct SlotAdded
    {
        public int Slot { get; }

        public SlotAdded(int slot)
        {
            Slot = slot;
        }
    }
}

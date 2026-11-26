namespace Chromacolony.Foundation
{
    public interface IRandom
    {
        uint NextUInt();

        int Range(int minInclusive, int maxExclusive);

        float Value();
    }

    public sealed class Pcg32 : IRandom
    {
        private const ulong Multiplier = 6364136223846793005UL;

        private readonly ulong _increment;
        private ulong _state;

        public Pcg32(ulong seed, ulong sequence = 54)
        {
            _state = 0;
            _increment = (sequence << 1) | 1UL;
            NextUInt();
            _state = unchecked(_state + seed);
            NextUInt();
        }

        public uint NextUInt()
        {
            var old = _state;
            _state = unchecked(old * Multiplier + _increment);
            var shifted = (uint)(((old >> 18) ^ old) >> 27);
            var rotation = (int)(old >> 59);
            return (shifted >> rotation) | (shifted << ((-rotation) & 31));
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new System.ArgumentException($"empty range: {minInclusive}..{maxExclusive}");
            }

            var span = (uint)((long)maxExclusive - minInclusive);
            var threshold = unchecked(0u - span) % span;
            while (true)
            {
                var r = NextUInt();
                if (r >= threshold)
                {
                    return (int)((long)minInclusive + r % span);
                }
            }
        }

        public float Value()
        {
            return (NextUInt() >> 8) * (1f / 16777216f);
        }
    }
}

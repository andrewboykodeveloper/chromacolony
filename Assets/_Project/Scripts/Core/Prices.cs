using System;

namespace Chromacolony.Core
{
    public sealed class Prices
    {
        private readonly int _slot;
        private readonly int _shuffle;
        private readonly int _back;

        public int CoinsPerWin { get; }

        public int StartStock { get; }

        public Prices(int coinsPerWin, int startStock, int slot, int shuffle, int back)
        {
            CoinsPerWin = coinsPerWin;
            StartStock = startStock;
            _slot = slot;
            _shuffle = shuffle;
            _back = back;
        }

        public int Of(Helper helper)
        {
            return helper switch
            {
                Helper.Slot => _slot,
                Helper.Shuffle => _shuffle,
                Helper.Back => _back,
                _ => throw new ArgumentOutOfRangeException(nameof(helper), helper, "no such helper"),
            };
        }
    }
}

namespace Chromacolony.Core
{
    public sealed class Prices
    {
        private readonly int[] _helpers;

        public Prices(int coinsPerWin, int startStock, int slot, int shuffle, int back)
        {
            CoinsPerWin = coinsPerWin;
            StartStock = startStock;
            _helpers = new[] { slot, shuffle, back };
        }

        public int CoinsPerWin { get; }

        public int StartStock { get; }

        public int Of(Helper helper)
        {
            return _helpers[(int)helper];
        }
    }
}

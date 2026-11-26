namespace Chromacolony.Core
{
    public readonly struct BoardEnded
    {
        public bool Won { get; }

        public bool Last { get; }

        public int CoinsBefore { get; }

        public int CoinsAfter { get; }

        public BoardEnded(bool won, bool last, int coinsBefore, int coinsAfter)
        {
            Won = won;
            Last = last;
            CoinsBefore = coinsBefore;
            CoinsAfter = coinsAfter;
        }
    }
}

namespace Chromacolony.Core
{
    public readonly struct BoardStarted
    {
        public BoardSpec Board { get; }

        public BoardStarted(BoardSpec board)
        {
            Board = board;
        }
    }
}

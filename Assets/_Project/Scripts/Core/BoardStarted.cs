namespace Chromacolony.Core
{
    public readonly struct BoardStarted
    {
        public BoardStarted(BoardSpec board)
        {
            Board = board;
        }

        public BoardSpec Board { get; }
    }
}

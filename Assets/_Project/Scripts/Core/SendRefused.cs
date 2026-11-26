namespace Chromacolony.Core
{
    public readonly struct SendRefused
    {
        public int Column { get; }

        public Refusal Reason { get; }

        public SendRefused(int column, Refusal reason)
        {
            Column = column;
            Reason = reason;
        }
    }
}

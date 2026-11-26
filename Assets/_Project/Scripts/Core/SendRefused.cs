namespace Chromacolony.Core
{
    public readonly struct SendRefused
    {
        public SendRefused(int column, Refusal reason)
        {
            Column = column;
            Reason = reason;
        }

        public int Column { get; }

        public Refusal Reason { get; }
    }
}

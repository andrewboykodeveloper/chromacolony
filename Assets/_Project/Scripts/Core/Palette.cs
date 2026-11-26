namespace Chromacolony.Core
{
    public static class Palette
    {
        public const string Letters = "ROYGTBPK";

        public static int Count => Letters.Length;

        public static int Of(char letter)
        {
            return Letters.IndexOf(letter);
        }
    }
}

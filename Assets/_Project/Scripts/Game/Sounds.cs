namespace Chromacolony.Game
{
    public static class Sounds
    {
        public static string Pick(int turn)
        {
            switch (turn % 3)
            {
                case 0:
                    return SoundKeys.Pick1;
                case 1:
                    return SoundKeys.Pick2;
                default:
                    return SoundKeys.Pick3;
            }
        }
    }
}

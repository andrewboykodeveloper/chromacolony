namespace Chromacolony.Game
{
    public static class Sounds
    {
        private const int PickSounds = 3;

        public static string Pick(int turn)
        {
            switch (turn % PickSounds)
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

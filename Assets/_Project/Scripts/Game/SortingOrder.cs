namespace Chromacolony.Game
{
    /// <summary>
    /// The order the world's sprites draw in, from back to front. In a queue, each crew draws one step below the crew in front of
    /// it, and its sign one step above itself, so the crews behind the front ones draw at or below the ropes.
    /// </summary>
    public static class SortingOrder
    {
        public const int Backdrop = -102;
        public const int BeachShadow = -101;
        public const int Beach = -100;
        public const int Tray = -5;
        public const int Slot = 0;
        public const int Castle = 1;
        public const int Pebble = 2;
        public const int PebbleGlow = 3;
        public const int PebbleLit = 4;
        public const int Rope = 9;
        public const int Crew = 10;
        public const int CrewSign = 11;
        public const int CrewInSlot = 20;
        public const int CrewSignInSlot = 21;
        public const int Sitter = 22;
        public const int CrabShadow = 39;
        public const int CrabLeg = 40;
        public const int CrabShell = 41;
        public const int CrabClaw = 42;
        public const int CarriedPebble = 43;
        public const int Effect = 55;
        public const int Pointer = 60;
    }
}

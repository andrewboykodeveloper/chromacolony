namespace Chromacolony.Game
{
    /// <summary>
    /// The names of the textures and sprites that the views draw at run time. A scene does not free them, so each view destroys
    /// its own.
    /// </summary>
    public static class RuntimeArt
    {
        public const string ColumnShadow = "Column shadow";
        public const string Sign = "Sign";
        public const string SoftOval = "Soft oval";
        public const string Tray = "Tray";
    }
}

namespace Chromacolony.Game
{
    public interface ISafeArea
    {
        (float Top, float Bottom, float Left, float Right) Insets { get; }
    }
}

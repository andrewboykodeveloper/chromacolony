namespace Chromacolony.Game
{
    public interface IInput
    {
        bool TryGetPress(out float x, out float y);
    }
}

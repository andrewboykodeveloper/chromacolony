namespace Chromacolony.Core
{
    public interface IInput
    {
        bool TryGetPress(out float x, out float y);
    }
}

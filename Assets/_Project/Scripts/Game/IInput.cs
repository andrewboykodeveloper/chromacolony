namespace Chromacolony.Game
{
    public interface IInput
    {
        /// <summary>
        /// A press that began this frame, in screen pixels from the top left, as the interface counts them.
        /// </summary>
        bool TryGetPress(out float x, out float y);
    }
}

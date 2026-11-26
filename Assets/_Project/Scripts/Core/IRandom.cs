namespace Chromacolony.Core
{
    public interface IRandom
    {
        uint NextUInt();

        int Range(int minInclusive, int maxExclusive);

        float Value();
    }
}

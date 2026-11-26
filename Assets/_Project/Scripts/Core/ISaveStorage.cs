namespace Chromacolony.Core
{
    public interface ISaveStorage
    {
        string Read(string key);

        void Write(string key, string text);
    }
}

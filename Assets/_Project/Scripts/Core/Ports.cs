namespace Chromacolony.Foundation.Ports
{
    public interface IAudio
    {
        void Play(string sound);

        void Say(string line);

        void PlayMusic(string track);
    }

    public interface IInput
    {
        bool TryGetPress(out float x, out float y);
    }

    public interface ISaveStorage
    {
        string Read(string key);

        void Write(string key, string text);
    }
}

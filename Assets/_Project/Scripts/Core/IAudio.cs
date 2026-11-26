namespace Chromacolony.Core
{
    public interface IAudio
    {
        void Play(string sound);

        void Say(string line);

        void PlayMusic(string track);
    }
}

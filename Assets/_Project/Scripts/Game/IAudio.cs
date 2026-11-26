namespace Chromacolony.Game
{
    public interface IAudio
    {
        void Play(string sound);

        void Say(string line);

        void PlayMusic(string track);
    }
}

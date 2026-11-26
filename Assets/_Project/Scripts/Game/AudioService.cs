using System.Collections.Generic;
using Chromacolony.Core;

namespace Chromacolony.Game
{
    /// <summary>
    /// Plays a sound, a voice line or the music only as the player's settings allow, and drops a sound played again within its gap.
    /// </summary>
    public sealed class AudioService
    {
        private readonly IAudio _audio;
        private readonly Progress _progress;
        private readonly Localization _text;
        private readonly float _soundGap;
        private readonly float _pickGap;
        private readonly float _coinGap;
        private readonly Dictionary<string, float> _played = new Dictionary<string, float>();
        private float _time;

        public AudioService(IAudio audio, Progress progress, Localization text, float soundGap, float pickGap, float coinGap)
        {
            _audio = audio;
            _progress = progress;
            _text = text;
            _soundGap = soundGap;
            _pickGap = pickGap;
            _coinGap = coinGap;
        }

        public void Update(float dt)
        {
            _time += dt;
        }

        public void Play(string sound)
        {
            if (!_progress.Sound)
            {
                return;
            }

            // The three pick sounds take turns as the crabs lift pebbles, so they share one gap.
            bool pick = sound == SoundKeys.Pick1 || sound == SoundKeys.Pick2 || sound == SoundKeys.Pick3;
            string group = pick ? SoundKeys.Pick1 : sound;
            float gap = pick ? _pickGap : sound == SoundKeys.Coin ? _coinGap : _soundGap;
            if (_played.TryGetValue(group, out float last) && _time - last < gap)
            {
                return;
            }

            _played[group] = _time;
            _audio.Play(sound);
        }

        public void Say(string line)
        {
            if (_progress.Voice)
            {
                _audio.Say(LineKeys.For(line, _text.Language));
            }
        }

        public void ApplyMusic()
        {
            _audio.PlayMusic(_progress.Music ? MusicKeys.Theme : null);
        }
    }
}

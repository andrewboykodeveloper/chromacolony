using System.Collections.Generic;
using Chromacolony.Core;
using UnityEngine;

namespace Chromacolony.Game
{
    public sealed class UnityAudio : IAudio
    {
        private readonly GameObject _host;
        private readonly AudioSource _effects;
        private readonly AudioSource _music;
        private readonly Dictionary<string, AudioClip> _sounds;
        private readonly Dictionary<string, AudioClip> _voiceLines;
        private readonly Dictionary<string, AudioClip> _tracks;
        private readonly HashSet<string> _reported = new HashSet<string>();
        private AudioSource _voice;

        public UnityAudio(GameObject host, IEnumerable<AudioClip> sounds, IEnumerable<AudioClip> voiceLines, IEnumerable<AudioClip> tracks)
        {
            _host = host;
            _effects = host.AddComponent<AudioSource>();
            _music = host.AddComponent<AudioSource>();
            _music.loop = true;
            _sounds = ByName(sounds, "sound");
            _voiceLines = ByName(voiceLines, "voice line");
            _tracks = ByName(tracks, "music track");
        }

        public void Play(string sound)
        {
            AudioClip clip = Find(_sounds, sound, "sound");
            if (clip != null)
            {
                _effects.PlayOneShot(clip);
            }
        }

        public void Say(string line)
        {
            AudioClip clip = Find(_voiceLines, line, "voice line");
            if (clip == null)
            {
                return;
            }

            if (_voice == null)
            {
                _voice = _host.AddComponent<AudioSource>();
                _voice.playOnAwake = false;
            }

            _voice.Stop();
            _voice.clip = clip;
            _voice.Play();
        }

        public void PlayMusic(string track)
        {
            if (string.IsNullOrEmpty(track))
            {
                _music.Stop();
                _music.clip = null;
                return;
            }

            AudioClip clip = Find(_tracks, track, "music track");
            if (clip != null && _music.clip != clip)
            {
                _music.clip = clip;
                _music.Play();
            }
        }

        private static Dictionary<string, AudioClip> ByName(IEnumerable<AudioClip> clips, string kind)
        {
            var byName = new Dictionary<string, AudioClip>();
            foreach (AudioClip clip in clips)
            {
                if (clip == null)
                {
                    Debug.LogWarning($"an empty {kind} slot in the game content");
                }
                else if (byName.ContainsKey(clip.name))
                {
                    Debug.LogWarning($"two {kind}s named {clip.name} in the game content");
                }
                else
                {
                    byName.Add(clip.name, clip);
                }
            }

            return byName;
        }

        private AudioClip Find(Dictionary<string, AudioClip> clips, string name, string kind)
        {
            if (clips.TryGetValue(name, out AudioClip clip))
            {
                return clip;
            }

            if (_reported.Add(kind + " " + name))
            {
                Debug.LogWarning($"no {kind} named {name}");
            }

            return null;
        }
    }
}

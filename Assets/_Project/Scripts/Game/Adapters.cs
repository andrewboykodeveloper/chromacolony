using System;
using System.Collections.Generic;
using System.IO;
using Chromacolony.Foundation;
using Chromacolony.Foundation.Ports;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Chromacolony.Foundation.Unity
{
    public sealed class FileSaveStorage : ISaveStorage
    {
        private readonly string _folder;

        public static string DefaultFolder => Path.Combine(Application.persistentDataPath, "saves");

        public FileSaveStorage(string folder = null)
        {
            _folder = folder ?? DefaultFolder;
        }

        public string Read(string key)
        {
            var path = PathOf(key);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        public void Write(string key, string text)
        {
            try
            {
                Directory.CreateDirectory(_folder);
                var path = PathOf(key);
                var temp = path + ".tmp";
                File.WriteAllText(temp, text);
                if (File.Exists(path))
                {
                    File.Replace(temp, path, null);
                }
                else
                {
                    File.Move(temp, path);
                }
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"FileSaveStorage: {key} could not be written: {e.Message}");
                throw;
            }
        }

        private string PathOf(string key)
        {
            return Path.Combine(_folder, key + ".txt");
        }
    }

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

    public sealed class PointerInput : IInput
    {
        public bool TryGetPress(out float x, out float y)
        {
            var pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame)
            {
                var position = pointer.position.ReadValue();
                x = position.x;
                y = Screen.height - position.y;
                return true;
            }

            x = 0;
            y = 0;
            return false;
        }
    }

    public static class BackKey
    {
        public static bool WasPressedThisFrame()
        {
            foreach (var device in InputSystem.devices)
            {
                if (device is Keyboard keyboard && keyboard.escapeKey.wasPressedThisFrame)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public sealed class GameTicker
    {
        private readonly FixedStepTicker _ticker;
        private readonly Action<float> _step;

        public GameTicker(float stepSeconds, Action<float> step)
        {
            _ticker = new FixedStepTicker(stepSeconds);
            _step = step;
        }

        public void Update()
        {
            _ticker.Advance(Time.deltaTime, _step);
        }
    }
}

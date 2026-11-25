using System;
using System.Collections.Generic;

namespace Chromacolony.Foundation.Ports
{
    public interface IAudio
    {
        bool Muted { get; set; }

        void Play(string sound);

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

        void Delete(string key);
    }

    public interface IAnalytics
    {
        void Track(string eventName, IReadOnlyDictionary<string, string> properties = null);
    }

    public interface IAds
    {
        bool RewardedReady { get; }

        void ShowRewarded(Action<bool> finished);

        void ShowInterstitial();
    }

    public interface IPurchases
    {
        bool Owns(string product);

        void Buy(string product, Action<bool> finished);
    }

    public sealed class FakeAudio : IAudio
    {
        public List<string> Played { get; } = new List<string>();

        public string Music { get; private set; }

        public bool Muted { get; set; }

        public void Play(string sound)
        {
            Played.Add(sound);
        }

        public void PlayMusic(string track)
        {
            Music = track;
        }
    }

    public sealed class FakeInput : IInput
    {
        private readonly Queue<(float X, float Y)> _presses = new Queue<(float, float)>();

        public void Press(float x, float y)
        {
            _presses.Enqueue((x, y));
        }

        public bool TryGetPress(out float x, out float y)
        {
            if (_presses.Count == 0)
            {
                x = 0;
                y = 0;
                return false;
            }

            (x, y) = _presses.Dequeue();
            return true;
        }
    }

    public sealed class FakeSaveStorage : ISaveStorage
    {
        public Dictionary<string, string> Files { get; } = new Dictionary<string, string>();

        public string Read(string key)
        {
            return Files.TryGetValue(key, out var text) ? text : null;
        }

        public void Write(string key, string text)
        {
            Files[key] = text;
        }

        public void Delete(string key)
        {
            Files.Remove(key);
        }
    }

    public sealed class FakeAnalytics : IAnalytics
    {
        public List<string> Events { get; } = new List<string>();

        public void Track(string eventName, IReadOnlyDictionary<string, string> properties = null)
        {
            Events.Add(eventName);
        }
    }

    public sealed class FakeAds : IAds
    {
        public bool RewardedReady { get; set; } = true;

        public bool RewardResult { get; set; } = true;

        public int Interstitials { get; private set; }

        public void ShowRewarded(Action<bool> finished)
        {
            finished(RewardedReady && RewardResult);
        }

        public void ShowInterstitial()
        {
            Interstitials++;
        }
    }

    public sealed class FakePurchases : IPurchases
    {
        private readonly HashSet<string> _owned = new HashSet<string>();

        public bool Succeed { get; set; } = true;

        public bool Owns(string product)
        {
            return _owned.Contains(product);
        }

        public void Buy(string product, Action<bool> finished)
        {
            if (Succeed)
            {
                _owned.Add(product);
            }

            finished(Succeed);
        }
    }
}

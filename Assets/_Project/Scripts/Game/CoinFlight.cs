using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chromacolony.Game
{
    public sealed class CoinFlight
    {
        private const int Coins = 7;
        private const float Stagger = 0.08f;
        private const float FlySeconds = 0.6f;

        private readonly VisualElement _layer;
        private readonly Label _counter;
        private readonly List<VisualElement> _shown = new List<VisualElement>();
        private Vector2 _from;
        private Vector2 _to;
        private int _start;
        private int _end;
        private int _counted;
        private float _time = -1f;

        public bool Flying => _time >= 0f;

        public System.Action Landed { get; set; }

        public CoinFlight(VisualElement layer, Label counter)
        {
            _layer = layer;
            _counter = counter;
        }

        public void Fly(Vector2 from, Vector2 to, int start, int end)
        {
            Stop();
            _from = from;
            _to = to;
            _start = start;
            _end = end;
            _time = 0f;
            for (int i = 0; i < Coins; i++)
            {
                var coin = new VisualElement();
                coin.AddToClassList(UiClasses.FlyingCoin);
                coin.pickingMode = PickingMode.Ignore;
                coin.style.opacity = 0f;
                _layer.Add(coin);
                _shown.Add(coin);
            }

            _counted = start;
            _counter.text = start.ToString(CultureInfo.InvariantCulture);
        }

        public void Update(float dt)
        {
            if (_time < 0f)
            {
                return;
            }

            _time += dt;
            int landed = 0;
            for (int i = 0; i < _shown.Count; i++)
            {
                float t = Mathf.Clamp01((_time - i * Stagger) / FlySeconds);
                VisualElement coin = _shown[i];
                if (t >= 1f)
                {
                    landed++;
                    if (coin.style.opacity.value > 0f)
                    {
                        Landed?.Invoke();
                    }

                    coin.style.opacity = 0f;
                    continue;
                }

                float eased = t * t * (3f - 2f * t);
                Vector2 point = Vector2.Lerp(_from, _to, eased) + new Vector2(0f, -320f * Mathf.Sin(eased * Mathf.PI));
                coin.style.left = point.x;
                coin.style.top = point.y;
                coin.style.opacity = t > 0f ? 1f : 0f;
                float scale = 1f - 0.35f * eased;
                coin.style.scale = new Scale(new Vector3(scale, scale, 1f));
            }

            int counted = _start + (_end - _start) * landed / Coins;
            if (counted != _counted)
            {
                _counted = counted;
                _counter.text = counted.ToString(CultureInfo.InvariantCulture);
            }

            if (landed == Coins)
            {
                Stop();
            }
        }

        public void Stop()
        {
            foreach (VisualElement coin in _shown)
            {
                coin.RemoveFromHierarchy();
            }

            _shown.Clear();
            if (_time >= 0f)
            {
                _counter.text = _end.ToString(CultureInfo.InvariantCulture);
            }

            _time = -1f;
        }
    }
}

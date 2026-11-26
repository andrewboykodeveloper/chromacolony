using System.Collections.Generic;
using UnityEngine;
using UIElements = UnityEngine.UIElements;

namespace Chromacolony.Unity
{
    public sealed class CoinFlight
    {
        private const int Coins = 7;
        private const float Stagger = 0.08f;
        private const float FlySeconds = 0.6f;
        private const string CoinClass = "flying-coin";

        private readonly UIElements.VisualElement _layer;
        private readonly UIElements.Label _counter;
        private readonly List<UIElements.VisualElement> _shown = new List<UIElements.VisualElement>();
        private Vector2 _from;
        private Vector2 _to;
        private int _start;
        private int _end;
        private float _time = -1f;

        public CoinFlight(UIElements.VisualElement layer, UIElements.Label counter)
        {
            _layer = layer;
            _counter = counter;
        }

        public bool Flying => _time >= 0f;

        public System.Action Landed { get; set; }

        public void Fly(Vector2 from, Vector2 to, int start, int end)
        {
            Stop();
            _from = from;
            _to = to;
            _start = start;
            _end = end;
            _time = 0f;
            for (var i = 0; i < Coins; i++)
            {
                var coin = new UIElements.VisualElement();
                coin.AddToClassList(CoinClass);
                coin.pickingMode = UIElements.PickingMode.Ignore;
                coin.style.opacity = 0f;
                _layer.Add(coin);
                _shown.Add(coin);
            }

            _counter.text = start.ToString();
        }

        public void Update(float dt)
        {
            if (_time < 0f)
            {
                return;
            }

            _time += dt;
            var landed = 0;
            for (var i = 0; i < _shown.Count; i++)
            {
                var t = Mathf.Clamp01((_time - i * Stagger) / FlySeconds);
                var coin = _shown[i];
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

                var eased = t * t * (3f - 2f * t);
                var point = Vector2.Lerp(_from, _to, eased) + new Vector2(0f, -320f * Mathf.Sin(eased * Mathf.PI));
                coin.style.left = point.x;
                coin.style.top = point.y;
                coin.style.opacity = t > 0f ? 1f : 0f;
                var scale = 1f - 0.35f * eased;
                coin.style.scale = new UIElements.Scale(new Vector3(scale, scale, 1f));
            }

            _counter.text = (_start + (_end - _start) * landed / Coins).ToString();
            if (landed == Coins)
            {
                Stop();
            }
        }

        public void Stop()
        {
            foreach (var coin in _shown)
            {
                coin.RemoveFromHierarchy();
            }

            _shown.Clear();
            if (_time >= 0f)
            {
                _counter.text = _end.ToString();
            }

            _time = -1f;
        }
    }
}

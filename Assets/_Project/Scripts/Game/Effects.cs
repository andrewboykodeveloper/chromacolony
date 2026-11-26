using System;
using System.Collections.Generic;
using Chromacolony.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Chromacolony.Game
{
    public sealed class Effects : IDisposable
    {
        private sealed class Bit
        {
            public SpriteRenderer Renderer;
            public Vector2 From;
            public Vector2 Way;
            public float SizeFrom;
            public float SizeTo;
            public float Life;
            public float Alpha;
            public float Spin;
            public Color Tint;
            public float Age;
        }

        private readonly Transform _holder;
        private readonly Material _material;
        private readonly IRandom _random;
        private readonly Sprite _puff;
        private readonly Sprite _sparkle;
        private readonly List<Bit> _live = new List<Bit>();
        private readonly Stack<SpriteRenderer> _pool = new Stack<SpriteRenderer>();

        public Effects(Material material, IRandom random, GameContent content)
        {
            _holder = new GameObject("Effects").transform;
            _material = material;
            _random = random;
            _puff = content.SandPuff;
            _sparkle = content.Sparkle;
        }

        public void Puff(Vector2 at, float size)
        {
            float spin = (_random.Value() - 0.5f) * 60f;
            Spawn(_puff, at, new Vector2(0f, size * 0.6f), size * 0.5f, size * 1.25f, 0.45f, 0.8f, spin, Color.white);
        }

        public void Sparkles(Vector2 at, float radius, int count, Color tint)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = (i + _random.Value() * 0.6f) / count * Mathf.PI * 2f;
                Vector2 way = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * (0.7f + 0.6f * _random.Value());
                float size = radius * (0.28f + 0.18f * _random.Value());
                Spawn(_sparkle, at, way * 2f / 0.55f, size, size * 0.4f, 0.55f, 1f, 220f * (_random.Value() - 0.5f), tint);
            }
        }

        public void Twinkle(Vector2 at, float size)
        {
            Twinkle(at, size, Color.white);
        }

        public void Twinkle(Vector2 at, float size, Color tint)
        {
            Spawn(_sparkle, at, Vector2.zero, size * 0.2f, size, 0.35f, 1f, 90f, tint);
        }

        public void Update(float dt)
        {
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                Bit bit = _live[i];
                bit.Age += dt;
                float t = Mathf.Clamp01(bit.Age / bit.Life);
                Transform transform = bit.Renderer.transform;
                transform.position = (Vector3)(bit.From + bit.Way * (1f - (1f - t) * (1f - t)) * bit.Life / 2f);
                float size = Mathf.Lerp(bit.SizeFrom, bit.SizeTo, Mathf.Sin(t * Mathf.PI * 0.5f));
                transform.localScale = SpriteRenderers.HeightScale(bit.Renderer.sprite, size);
                transform.localRotation = Quaternion.Euler(0f, 0f, bit.Spin * bit.Age);
                Color tint = bit.Tint;
                tint.a = bit.Alpha * (t < 0.2f ? t / 0.2f : 1f - (t - 0.2f) / 0.8f);
                bit.Renderer.color = tint;
                if (t >= 1f)
                {
                    bit.Renderer.gameObject.SetActive(false);
                    _pool.Push(bit.Renderer);
                    _live.RemoveAt(i);
                }
            }
        }

        public void Clear()
        {
            foreach (Bit bit in _live)
            {
                bit.Renderer.gameObject.SetActive(false);
                _pool.Push(bit.Renderer);
            }

            _live.Clear();
        }

        public void Dispose()
        {
            if (_holder != null)
            {
                Object.Destroy(_holder.gameObject);
            }
        }

        private void Spawn(Sprite sprite, Vector2 at, Vector2 way, float sizeFrom, float sizeTo, float life, float alpha, float spin, Color tint)
        {
            SpriteRenderer renderer;
            if (_pool.Count > 0)
            {
                renderer = _pool.Pop();
                renderer.gameObject.SetActive(true);
            }
            else
            {
                renderer = SpriteRenderers.Create(_holder, sprite, _material, "Effect", SortingOrder.Effect);
            }

            renderer.sprite = sprite;
            renderer.transform.position = at;
            renderer.transform.localScale = SpriteRenderers.HeightScale(sprite, sizeFrom);
            renderer.color = new Color(tint.r, tint.g, tint.b, 0f);
            _live.Add(new Bit
            {
                Renderer = renderer,
                From = at,
                Way = way,
                SizeFrom = sizeFrom,
                SizeTo = sizeTo,
                Life = life,
                Alpha = alpha,
                Spin = spin,
                Tint = tint,
            });
        }
    }
}

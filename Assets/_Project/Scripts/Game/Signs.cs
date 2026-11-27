using System;
using Chromacolony.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Chromacolony.Game
{
    public sealed class Signs : IDisposable
    {
        private const int Size = 64;
        private const int Samples = 4;

        private const float Radius = 0.72f;
        private const float RimWidth = 0.14f;
        private const float PointedReach = 1.12f;
        private const float TriangleDrop = 0.08f;
        private const float StarInner = 0.5f;
        private const int StarPoints = 5;
        private const float SquareHalf = 0.82f;
        private const float HeartSpan = 1.15f;
        private const float HeartDrop = 0.12f;
        private const float RingInner = 0.38f;
        private const float HexagonHalf = 0.9f;
        private const float Cos30 = 0.866f;
        private const float Sin30 = 0.5f;

        private readonly Sprite[] _sprites = new Sprite[Palette.Count];

        private static Color RimColor => new Color(0.04f, 0.07f, 0.18f, 0.85f);

        public Signs()
        {
            for (int color = 0; color < _sprites.Length; color++)
            {
                _sprites[color] = Draw(color);
            }
        }

        public Sprite For(int color)
        {
            return _sprites[Mathf.Clamp(color, 0, _sprites.Length - 1)];
        }

        public void Dispose()
        {
            foreach (Sprite sprite in _sprites)
            {
                if (sprite != null)
                {
                    Object.Destroy(sprite.texture);
                    Object.Destroy(sprite);
                }
            }
        }

        private static Sprite Draw(int color)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = RuntimeArt.Sign + " " + color,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float inside = 0f, rim = 0f;
                    for (int sy = 0; sy < Samples; sy++)
                    {
                        for (int sx = 0; sx < Samples; sx++)
                        {
                            float px = (x + (sx + 0.5f) / Samples) / Size * 2f - 1f;
                            float py = (y + (sy + 0.5f) / Samples) / Size * 2f - 1f;
                            inside += In(color, px, py, 0f) ? 1f : 0f;
                            rim += In(color, px, py, RimWidth) ? 1f : 0f;
                        }
                    }

                    inside /= Samples * Samples;
                    rim /= Samples * Samples;
                    var white = new Color(1f, 1f, 1f, 1f);
                    Color dark = RimColor;
                    var c = Color.Lerp(dark, white, inside);
                    c.a = Mathf.Max(rim * dark.a, inside);
                    texture.SetPixel(x, y, c);
                }
            }

            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size, 0, SpriteMeshType.FullRect);
            sprite.name = texture.name;
            return sprite;
        }

        private static bool In(int color, float x, float y, float grow)
        {
            float r = Radius + grow;
            float d = Mathf.Sqrt(x * x + y * y);
            switch (color)
            {
                case 0:
                    return d < r;
                case 1:
                    return Triangle(x, y + TriangleDrop, r * PointedReach);
                case 2:
                    return Star(x, y, r * PointedReach, r * StarInner);
                case 3:
                    return Mathf.Abs(x) < r * SquareHalf && Mathf.Abs(y) < r * SquareHalf;
                case 4:
                    return Mathf.Abs(x) + Mathf.Abs(y) < r * PointedReach;
                case 5:
                    return Heart(x, y, r);
                case 6:
                    return d < r && d > RingInner - grow;
                default:
                    float ax = Mathf.Abs(x);
                    float ay = Mathf.Abs(y);
                    return Mathf.Max(ax * Cos30 + ay * Sin30, ay) < r * HexagonHalf;
            }
        }

        private static bool Triangle(float x, float y, float r)
        {
            var a = new Vector2(0f, r);
            var b = new Vector2(-r * Cos30, -r * Sin30);
            var c = new Vector2(r * Cos30, -r * Sin30);
            var p = new Vector2(x, y);
            return Side(p, a, b) >= 0f && Side(p, b, c) >= 0f && Side(p, c, a) >= 0f;
        }

        private static float Side(Vector2 p, Vector2 a, Vector2 b)
        {
            return (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
        }

        private static bool Heart(float x, float y, float r)
        {
            float k = r / HeartSpan;
            float u = x / k;
            float v = y / k + HeartDrop;
            float a = u * u + v * v - 1f;
            return a * a * a - u * u * v * v * v <= 0f;
        }

        private static bool Star(float x, float y, float outer, float inner)
        {
            bool inside = false;
            int corners = 2 * StarPoints;
            for (int i = 0, j = corners - 1; i < corners; j = i++)
            {
                float ai = Mathf.PI / 2f + i * Mathf.PI / StarPoints;
                float aj = Mathf.PI / 2f + j * Mathf.PI / StarPoints;
                float ri = i % 2 == 0 ? outer : inner;
                float rj = j % 2 == 0 ? outer : inner;
                float xi = Mathf.Cos(ai) * ri;
                float yi = Mathf.Sin(ai) * ri;
                float xj = Mathf.Cos(aj) * rj;
                float yj = Mathf.Sin(aj) * rj;
                if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}

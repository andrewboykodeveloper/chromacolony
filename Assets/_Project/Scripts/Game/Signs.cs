using UnityEngine;

namespace Chromacolony.Game
{
    public sealed class Signs
    {
        private const int Size = 64;
        private const int Samples = 4;
        private readonly Sprite[] _sprites = new Sprite[8];

        public Signs()
        {
            for (var color = 0; color < _sprites.Length; color++)
            {
                _sprites[color] = Draw(color);
            }
        }

        public Sprite For(int color)
        {
            return _sprites[Mathf.Clamp(color, 0, _sprites.Length - 1)];
        }

        private static Sprite Draw(int color)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = "Sign " + color,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    float inside = 0f, rim = 0f;
                    for (var sy = 0; sy < Samples; sy++)
                    {
                        for (var sx = 0; sx < Samples; sx++)
                        {
                            var px = (x + (sx + 0.5f) / Samples) / Size * 2f - 1f;
                            var py = (y + (sy + 0.5f) / Samples) / Size * 2f - 1f;
                            inside += In(color, px, py, 0f) ? 1f : 0f;
                            rim += In(color, px, py, 0.14f) ? 1f : 0f;
                        }
                    }

                    inside /= Samples * Samples;
                    rim /= Samples * Samples;
                    var white = new Color(1f, 1f, 1f, 1f);
                    var dark = new Color(0.04f, 0.07f, 0.18f, 0.85f);
                    var c = Color.Lerp(dark, white, inside);
                    c.a = Mathf.Max(rim * dark.a, inside);
                    texture.SetPixel(x, y, c);
                }
            }

            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size, 0, SpriteMeshType.FullRect);
        }

        private static bool In(int color, float x, float y, float grow)
        {
            var r = 0.72f + grow;
            var d = Mathf.Sqrt(x * x + y * y);
            switch (color)
            {
                case 0:
                    return d < r;
                case 1:
                    return Triangle(x, y + 0.08f, r * 1.12f);
                case 2:
                    return Star(x, y, r * 1.12f, r * 0.5f);
                case 3:
                    return Mathf.Abs(x) < r * 0.82f && Mathf.Abs(y) < r * 0.82f;
                case 4:
                    return Mathf.Abs(x) + Mathf.Abs(y) < r * 1.12f;
                case 5:
                    return Heart(x, y, r);
                case 6:
                    return d < r && d > 0.38f - grow;
                default:
                    var ax = Mathf.Abs(x);
                    var ay = Mathf.Abs(y);
                    return Mathf.Max(ax * 0.866f + ay * 0.5f, ay) < r * 0.9f;
            }
        }

        private static bool Triangle(float x, float y, float r)
        {
            var a = new Vector2(0f, r);
            var b = new Vector2(-r * 0.866f, -r * 0.5f);
            var c = new Vector2(r * 0.866f, -r * 0.5f);
            var p = new Vector2(x, y);
            return Side(p, a, b) >= 0f && Side(p, b, c) >= 0f && Side(p, c, a) >= 0f;
        }

        private static float Side(Vector2 p, Vector2 a, Vector2 b)
        {
            return (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
        }

        private static bool Heart(float x, float y, float r)
        {
            var k = r / 1.15f;
            var u = x / k;
            var v = y / k + 0.12f;
            var a = u * u + v * v - 1f;
            return a * a * a - u * u * v * v * v <= 0f;
        }

        private static bool Star(float x, float y, float outer, float inner)
        {
            var inside = false;
            for (int i = 0, j = 9; i < 10; j = i++)
            {
                var ai = Mathf.PI / 2f + i * Mathf.PI / 5f;
                var aj = Mathf.PI / 2f + j * Mathf.PI / 5f;
                var ri = i % 2 == 0 ? outer : inner;
                var rj = j % 2 == 0 ? outer : inner;
                var xi = Mathf.Cos(ai) * ri;
                var yi = Mathf.Sin(ai) * ri;
                var xj = Mathf.Cos(aj) * rj;
                var yj = Mathf.Sin(aj) * rj;
                if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}

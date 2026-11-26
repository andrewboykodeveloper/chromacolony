using UnityEngine;

namespace Chromacolony.Game
{
    public sealed class BeachView
    {
        private const int Order = -100;

        private const float BoardShoreY = -0.3f;

        private const float Glide = 3f;

        private const float CoverReach = 1.12f;

        private const float ShadowWidth = 0.9f;
        private const float ShadowAlpha = 0.6f;

        private readonly Camera _camera;
        private readonly Material _material;
        private readonly Sprite _sprite;
        private readonly float _shore;
        private readonly Transform _root;
        private readonly SpriteRenderer _picture;
        private readonly SpriteRenderer _backdrop;
        private readonly SpriteRenderer[] _shadows = new SpriteRenderer[2];
        private float _rise;

        public BeachView(Camera camera, Material material, float shore, Sprite sprite)
        {
            _camera = camera;
            _material = material;
            _shore = Mathf.Clamp(shore, 0.1f, 0.9f);
            _sprite = sprite;
            _root = new GameObject("Beach").transform;
            _picture = Make(_sprite, Order);
            _backdrop = Make(_sprite, Order - 2);
            _backdrop.color = BackdropTint;
            var shadow = Shadow();
            for (var i = 0; i < _shadows.Length; i++)
            {
                _shadows[i] = Make(shadow, Order - 1);
                _shadows[i].flipX = i == 0;
            }
        }

        public bool OnBoard { get; set; }

        public void Update(float dt)
        {
            _rise = Mathf.MoveTowards(_rise, OnBoard ? 1f : 0f, dt * Glide);
            var eased = _rise * _rise * (3f - 2f * _rise);
            var halfHeight = _camera.orthographicSize;
            var halfWidth = halfHeight * _camera.aspect;
            var center = (Vector2)_camera.transform.position;
            var top = center.y + halfHeight;
            var bottom = center.y - halfHeight;
            var size = _sprite.bounds.size;

            var menuHeight = halfHeight * 2f;
            var coverHeight = halfWidth * 2f * size.y / size.x;
            if (coverHeight > menuHeight && coverHeight <= menuHeight * CoverReach)
            {
                menuHeight = coverHeight;
            }

            var boardHeight = Mathf.Max(menuHeight, Mathf.Max((top - BoardShoreY) / _shore, (BoardShoreY - bottom) / (1f - _shore)));
            var height = Mathf.Lerp(menuHeight, boardHeight, eased);
            var pictureTop = Mathf.Lerp(top, BoardShoreY + _shore * boardHeight, eased);
            var scale = height / size.y;
            var width = size.x * scale;
            _picture.transform.position = new Vector3(center.x, pictureTop - height / 2f, 0f);
            _picture.transform.localScale = new Vector3(scale, scale, 1f);

            var wide = halfWidth > width / 2f + 0.01f;
            _backdrop.enabled = wide;
            if (wide)
            {
                var backdrop = _backdrop.sprite.bounds.size;
                var cover = Mathf.Max(halfWidth * 2f / backdrop.x, halfHeight * 2f / backdrop.y);
                _backdrop.transform.position = new Vector3(center.x, center.y, 0f);
                _backdrop.transform.localScale = new Vector3(cover, cover, 1f);
            }

            for (var i = 0; i < _shadows.Length; i++)
            {
                var shadow = _shadows[i];
                shadow.enabled = wide;
                if (!wide)
                {
                    continue;
                }

                var unit = shadow.sprite.bounds.size;
                var x = center.x + (i == 0 ? -1f : 1f) * (width / 2f + ShadowWidth / 2f);
                shadow.transform.position = new Vector3(x, center.y, 0f);
                shadow.transform.localScale = new Vector3(ShadowWidth / unit.x, halfHeight * 2f / unit.y, 1f);
            }
        }

        private static Color BackdropTint => new Color(0.55f, 0.57f, 0.7f);

        private static Sprite Shadow()
        {
            const int size = 64;
            var texture = new Texture2D(size, 1, TextureFormat.RGBA32, false)
            {
                name = "Column shadow",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            for (var x = 0; x < size; x++)
            {
                var t = 1f - (x + 0.5f) / size;
                texture.SetPixel(x, 0, new Color(0.01f, 0.02f, 0.08f, ShadowAlpha * t * t));
            }

            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, 1), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
        }

        private SpriteRenderer Make(Sprite sprite, int order)
        {
            var go = new GameObject("Beach picture");
            go.transform.SetParent(_root, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = _material;
            renderer.sortingOrder = order;
            return renderer;
        }
    }
}

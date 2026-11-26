using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Chromacolony.Game
{
    public sealed class BeachView : IDisposable
    {
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
        private readonly Sprite _shadow;
        private float _rise;

        public bool OnBoard { get; set; }

        private static Color BackdropTint => new Color(0.55f, 0.57f, 0.7f);

        public BeachView(Camera camera, Material material, float shore, Sprite sprite)
        {
            _camera = camera;
            _material = material;
            _shore = Mathf.Clamp(shore, 0.1f, 0.9f);
            _sprite = sprite;
            _root = new GameObject("Beach").transform;
            _picture = Make(_sprite, SortingOrder.Beach);
            _backdrop = Make(_sprite, SortingOrder.Backdrop);
            _backdrop.color = BackdropTint;
            _shadow = Shadow();
            for (int i = 0; i < _shadows.Length; i++)
            {
                _shadows[i] = Make(_shadow, SortingOrder.BeachShadow);
                _shadows[i].flipX = i == 0;
            }
        }

        public void Update(float dt)
        {
            _rise = Mathf.MoveTowards(_rise, OnBoard ? 1f : 0f, dt * Glide);
            float eased = _rise * _rise * (3f - 2f * _rise);
            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;
            var center = (Vector2)_camera.transform.position;
            float top = center.y + halfHeight;
            float bottom = center.y - halfHeight;
            Vector3 size = _sprite.bounds.size;

            float menuHeight = halfHeight * 2f;
            float coverHeight = halfWidth * 2f * size.y / size.x;
            if (coverHeight > menuHeight && coverHeight <= menuHeight * CoverReach)
            {
                menuHeight = coverHeight;
            }

            float boardHeight = Mathf.Max(menuHeight, Mathf.Max((top - BoardShoreY) / _shore, (BoardShoreY - bottom) / (1f - _shore)));
            float height = Mathf.Lerp(menuHeight, boardHeight, eased);
            float pictureTop = Mathf.Lerp(top, BoardShoreY + _shore * boardHeight, eased);
            float scale = height / size.y;
            float width = size.x * scale;
            _picture.transform.position = new Vector3(center.x, pictureTop - height / 2f, 0f);
            _picture.transform.localScale = new Vector3(scale, scale, 1f);

            bool wide = halfWidth > width / 2f + 0.01f;
            _backdrop.enabled = wide;
            if (wide)
            {
                Vector3 backdrop = _backdrop.sprite.bounds.size;
                float cover = Mathf.Max(halfWidth * 2f / backdrop.x, halfHeight * 2f / backdrop.y);
                _backdrop.transform.position = new Vector3(center.x, center.y, 0f);
                _backdrop.transform.localScale = new Vector3(cover, cover, 1f);
            }

            for (int i = 0; i < _shadows.Length; i++)
            {
                SpriteRenderer shadow = _shadows[i];
                shadow.enabled = wide;
                if (!wide)
                {
                    continue;
                }

                Vector3 unit = shadow.sprite.bounds.size;
                float x = center.x + (i == 0 ? -1f : 1f) * (width / 2f + ShadowWidth / 2f);
                shadow.transform.position = new Vector3(x, center.y, 0f);
                shadow.transform.localScale = new Vector3(ShadowWidth / unit.x, halfHeight * 2f / unit.y, 1f);
            }
        }

        public void Dispose()
        {
            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
            }

            if (_shadow != null)
            {
                Object.Destroy(_shadow.texture);
                Object.Destroy(_shadow);
            }
        }

        private static Sprite Shadow()
        {
            const int Size = 64;
            var texture = new Texture2D(Size, 1, TextureFormat.RGBA32, false)
            {
                name = RuntimeArt.ColumnShadow,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            for (int x = 0; x < Size; x++)
            {
                float t = 1f - (x + 0.5f) / Size;
                texture.SetPixel(x, 0, new Color(0.01f, 0.02f, 0.08f, ShadowAlpha * t * t));
            }

            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, Size, 1), new Vector2(0.5f, 0.5f), Size, 0, SpriteMeshType.FullRect);
            sprite.name = texture.name;
            return sprite;
        }

        private SpriteRenderer Make(Sprite sprite, int order)
        {
            var go = new GameObject("Beach picture");
            go.transform.SetParent(_root, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = _material;
            renderer.sortingOrder = order;
            return renderer;
        }
    }
}

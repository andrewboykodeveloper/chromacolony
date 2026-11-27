using UnityEngine;

namespace Chromacolony.Game
{
    public static class SpriteRenderers
    {
        // Guards a division by a sprite's size.
        private const float MinExtent = 0.0001f;

        public static SpriteRenderer Create(Transform parent, Sprite sprite, Material material, string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            return renderer;
        }

        public static void Place(SpriteRenderer renderer, Vector2 position, Vector3 scale)
        {
            renderer.transform.position = position;
            renderer.transform.localScale = scale;
        }

        public static Vector3 HeightScale(Sprite sprite, float height)
        {
            float s = height / Mathf.Max(sprite.bounds.size.y, MinExtent);
            return new Vector3(s, s, 1f);
        }
    }
}

using UnityEngine;
using UnityEngine.UIElements;

namespace Chromacolony.Game
{
    public static class WorldToPanel
    {
        public static Vector2 Point(Camera camera, Vector3 world, VisualElement panelRoot)
        {
            Vector3 v = camera.WorldToViewportPoint(world);
            Vector2 size = panelRoot.worldBound.size;
            return new Vector2(v.x * size.x, (1f - v.y) * size.y);
        }
    }
}

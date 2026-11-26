using UnityEngine;
using UnityEngine.UIElements;

namespace Chromacolony.Game
{
    public interface ISafeArea
    {
        (float Top, float Bottom, float Left, float Right) Insets { get; }
    }

    public sealed class ScreenSafeArea : ISafeArea
    {
        public const float PanelWidth = 1080f;
        public const float PanelHeight = 1920f;

        public (float Top, float Bottom, float Left, float Right) Insets
        {
            get
            {
                float points = Mathf.Max(PanelWidth / Screen.width, PanelHeight / Screen.height);
                Rect safe = Screen.safeArea;
                return ((Screen.height - safe.yMax) * points, safe.yMin * points, safe.xMin * points, (Screen.width - safe.xMax) * points);
            }
        }
    }

    public static class SafeAreas
    {
        public static void Apply(VisualElement safeElement, ISafeArea area)
        {
            (float top, float bottom, float left, float right) = area.Insets;
            safeElement.style.top = top;
            safeElement.style.bottom = bottom;
            safeElement.style.left = left;
            safeElement.style.right = right;
        }

        public static void Keep(VisualElement safeElement, ISafeArea area)
        {
            (float Top, float Bottom, float Left, float Right) applied = area.Insets;
            Apply(safeElement, area);
            safeElement.schedule.Execute(() =>
            {
                (float Top, float Bottom, float Left, float Right) insets = area.Insets;
                if (!insets.Equals(applied))
                {
                    applied = insets;
                    Apply(safeElement, area);
                }
            }).Every(500);
        }
    }

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

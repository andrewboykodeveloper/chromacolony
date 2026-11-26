using UnityEngine;
using UnityEngine.UIElements;

namespace Chromacolony.Foundation.Unity
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
                var points = Mathf.Max(PanelWidth / Screen.width, PanelHeight / Screen.height);
                var safe = Screen.safeArea;
                return ((Screen.height - safe.yMax) * points, safe.yMin * points, safe.xMin * points, (Screen.width - safe.xMax) * points);
            }
        }
    }

    public static class SafeAreas
    {
        public static void Apply(VisualElement safeElement, ISafeArea area)
        {
            var (top, bottom, left, right) = area.Insets;
            safeElement.style.top = top;
            safeElement.style.bottom = bottom;
            safeElement.style.left = left;
            safeElement.style.right = right;
        }

        public static void Keep(VisualElement safeElement, ISafeArea area)
        {
            var applied = area.Insets;
            Apply(safeElement, area);
            safeElement.schedule.Execute(() =>
            {
                var insets = area.Insets;
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
            var v = camera.WorldToViewportPoint(world);
            var size = panelRoot.worldBound.size;
            return new Vector2(v.x * size.x, (1f - v.y) * size.y);
        }
    }
}

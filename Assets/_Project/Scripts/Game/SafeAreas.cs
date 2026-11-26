using UnityEngine.UIElements;

namespace Chromacolony.Game
{
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

        public static IVisualElementScheduledItem Keep(VisualElement safeElement, ISafeArea area)
        {
            (float Top, float Bottom, float Left, float Right) applied = area.Insets;
            Apply(safeElement, area);
            return safeElement.schedule.Execute(() =>
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
}

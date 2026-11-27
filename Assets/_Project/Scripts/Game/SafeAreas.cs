using UnityEngine;
using UnityEngine.UIElements;

namespace Chromacolony.Game
{
    public static class SafeAreas
    {
        private const long CheckMilliseconds = 500;

        /// <summary>
        /// How far a safe area on the screen lies inside each edge, in the panel's own units: the panel settings scale the screen, so
        /// the screen's points go through the panel.
        /// </summary>
        public static (float Top, float Bottom, float Left, float Right) Insets(IPanel panel, Rect safe, Vector2 screen)
        {
            // The screen's y runs up, and the interface's down from the top.
            Vector2 origin = RuntimePanelUtils.ScreenToPanel(panel, Vector2.zero);
            Vector2 topLeft = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(safe.xMin, screen.y - safe.yMax)) - origin;
            Vector2 bottomRight = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(safe.xMax, screen.y - safe.yMin)) - origin;
            Vector2 size = RuntimePanelUtils.ScreenToPanel(panel, screen) - origin;
            return (topLeft.y, size.y - bottomRight.y, topLeft.x, size.x - bottomRight.x);
        }

        public static IVisualElementScheduledItem Keep(VisualElement safeElement)
        {
            (float Top, float Bottom, float Left, float Right) applied = ScreenInsets(safeElement.panel);
            Apply(safeElement, applied);
            return safeElement.schedule.Execute(() =>
            {
                (float Top, float Bottom, float Left, float Right) insets = ScreenInsets(safeElement.panel);
                if (!insets.Equals(applied))
                {
                    applied = insets;
                    Apply(safeElement, insets);
                }
            }).Every(CheckMilliseconds);
        }

        private static (float Top, float Bottom, float Left, float Right) ScreenInsets(IPanel panel)
        {
            return Insets(panel, Screen.safeArea, new Vector2(Screen.width, Screen.height));
        }

        private static void Apply(VisualElement safeElement, (float Top, float Bottom, float Left, float Right) insets)
        {
            safeElement.style.top = insets.Top;
            safeElement.style.bottom = insets.Bottom;
            safeElement.style.left = insets.Left;
            safeElement.style.right = insets.Right;
        }
    }
}

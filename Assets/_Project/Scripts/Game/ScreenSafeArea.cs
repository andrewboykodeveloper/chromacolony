using UnityEngine;

namespace Chromacolony.Game
{
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
}

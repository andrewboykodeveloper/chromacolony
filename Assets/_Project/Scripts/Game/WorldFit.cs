using System;

namespace Chromacolony.Game
{
    public static class WorldFit
    {
        public static float HalfHeight(float designHalfWidth, float designHalfHeight, float aspect)
        {
            if (aspect <= 0f || designHalfWidth <= 0f || designHalfHeight <= 0f)
            {
                throw new ArgumentException($"a world fits a positive area into a positive shape, not {designHalfWidth} by {designHalfHeight} into {aspect}");
            }

            return Math.Max(designHalfHeight, designHalfWidth / aspect);
        }
    }
}

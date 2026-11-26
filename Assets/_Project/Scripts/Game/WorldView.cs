using UnityEngine;

namespace Chromacolony.Game
{
    public static class WorldView
    {
        public static void Fit(Camera camera, float designHalfWidth, float designHalfHeight)
        {
            camera.orthographic = true;
            camera.orthographicSize = WorldFit.HalfHeight(designHalfWidth, designHalfHeight, camera.aspect);
        }
    }
}

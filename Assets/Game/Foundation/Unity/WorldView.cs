using Chromacolony.Foundation;
using UnityEngine;

namespace Chromacolony.Foundation.Unity
{
    public static class WorldView
    {
        public static void Fit(Camera camera, float designHalfWidth, float designHalfHeight)
        {
            camera.orthographic = true;
            camera.orthographicSize = WorldFit.HalfHeight(designHalfWidth, designHalfHeight, camera.aspect);
        }

        public static Vector2 HalfSize(Camera camera)
        {
            return new Vector2(camera.orthographicSize * camera.aspect, camera.orthographicSize);
        }
    }
}

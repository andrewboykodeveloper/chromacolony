using UnityEngine.InputSystem;

namespace Chromacolony.Game
{
    public static class BackKey
    {
        public static bool WasPressedThisFrame()
        {
            foreach (InputDevice device in InputSystem.devices)
            {
                if (device is Keyboard keyboard && keyboard.escapeKey.wasPressedThisFrame)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

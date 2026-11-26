using Chromacolony.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Chromacolony.Game
{
    public sealed class PointerInput : IInput
    {
        public bool TryGetPress(out float x, out float y)
        {
            Pointer pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame)
            {
                Vector2 position = pointer.position.ReadValue();
                x = position.x;
                y = Screen.height - position.y;
                return true;
            }

            x = 0;
            y = 0;
            return false;
        }
    }
}

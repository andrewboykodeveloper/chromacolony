using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Chromacolony.Game
{
    /// <summary>
    /// The click handlers given to the buttons of the interface, kept so that they can all be taken back when it is unbound.
    /// </summary>
    public sealed class ButtonBindings
    {
        private readonly List<(Button Button, Action Action)> _bound = new List<(Button Button, Action Action)>();

        public void Click(Button button, Action action)
        {
            button.clicked += action;
            _bound.Add((button, action));
        }

        public void Clear()
        {
            foreach ((Button button, Action action) in _bound)
            {
                button.clicked -= action;
            }

            _bound.Clear();
        }
    }
}

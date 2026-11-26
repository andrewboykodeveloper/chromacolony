using System;
using Chromacolony.Core;
using UnityEngine;

namespace Chromacolony.Game
{
    public sealed class GameTicker
    {
        private readonly FixedStepTicker _ticker;
        private readonly Action<float> _step;

        public GameTicker(float stepSeconds, Action<float> step)
        {
            _ticker = new FixedStepTicker(stepSeconds);
            _step = step;
        }

        public void Update()
        {
            _ticker.Advance(Time.deltaTime, _step);
        }
    }
}

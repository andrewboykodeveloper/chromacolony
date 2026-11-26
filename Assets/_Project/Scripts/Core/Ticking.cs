using System;

namespace Chromacolony.Core
{
    public sealed class FixedStepTicker
    {
        private readonly double _step;
        private readonly int _maxStepsPerAdvance;
        private double _pending;

        public FixedStepTicker(double step, int maxStepsPerAdvance = 8)
        {
            if (step <= 0)
            {
                throw new ArgumentException("the step must be positive");
            }

            _step = step;
            _maxStepsPerAdvance = maxStepsPerAdvance;
        }

        public int Advance(double frameSeconds, Action<float> tick)
        {
            _pending += frameSeconds;
            var steps = 0;
            while (_pending >= _step && steps < _maxStepsPerAdvance)
            {
                tick((float)_step);
                _pending -= _step;
                steps++;
            }

            if (steps == _maxStepsPerAdvance && _pending >= _step)
            {
                _pending = 0;
            }

            return steps;
        }
    }
}

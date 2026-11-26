using System;
using System.Collections.Generic;

namespace Chromacolony.Core
{
    public sealed class StateMachine<TState> where TState : struct
    {
        private readonly HashSet<(TState From, TState To)> _allowed = new HashSet<(TState, TState)>();

        public TState Current { get; private set; }

        public StateMachine(TState initial)
        {
            Current = initial;
        }

        public StateMachine<TState> Allow(TState from, TState to)
        {
            _allowed.Add((from, to));
            return this;
        }

        public bool CanGo(TState to)
        {
            return _allowed.Contains((Current, to));
        }

        public void Go(TState to)
        {
            if (!CanGo(to))
            {
                throw new InvalidOperationException($"no transition from {Current} to {to}");
            }

            Current = to;
        }
    }
}

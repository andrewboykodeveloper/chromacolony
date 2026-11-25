using System;
using System.Collections.Generic;

namespace Chromacolony.Foundation
{
    public sealed class EventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();

        public IDisposable Subscribe<T>(Action<T> handler)
        {
            if (!_handlers.TryGetValue(typeof(T), out var list))
            {
                _handlers[typeof(T)] = list = new List<Delegate>();
            }

            list.Add(handler);
            return new Subscription(() => list.Remove(handler));
        }

        public void Publish<T>(T message)
        {
            if (!_handlers.TryGetValue(typeof(T), out var list))
            {
                return;
            }

            foreach (var handler in list.ToArray())
            {
                ((Action<T>)handler)(message);
            }
        }

        private sealed class Subscription : IDisposable
        {
            private Action _remove;

            public Subscription(Action remove)
            {
                _remove = remove;
            }

            public void Dispose()
            {
                _remove?.Invoke();
                _remove = null;
            }
        }
    }
}

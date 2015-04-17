using System;
using System.Collections.Generic;
using Elders.Cronus.DomainModeling;

namespace Elders.Cronus.Projections.ElasticSearch
{
    public sealed class StateBuilder<TState> : StateBuilder<TState, IEvent>
    {
    }

    public class StateBuilder<TState, V>
    {
        private TState state;
        private readonly List<V> events;
        private readonly Dictionary<Type, Func<object, TState, TState>> handlers;

        public StateBuilder(List<V> events = null)
        {
            this.events = events ?? new List<V>();
            this.handlers = new Dictionary<Type, Func<object, TState, TState>>();
        }

        public void Handle(V e)
        {
            events.Add(e);
        }

        public StateBuilder<TState, V> Init(Func<TState> state)
        {
            this.state = state();
            return this;
        }

        public StateBuilder<TState, V> When<T>(Func<T, TState, TState> handler)
        {
            //  Check and throw custom exception when trying to handle an event type twice
            handlers.Add(typeof(T), (x, y) => handler((T)x, y));
            return this;
        }

        public TState Build()
        {
            foreach (var item in events)
            {
                var t = item.GetType();
                var handler = handlers[t];
                state = handler(item, state);
            }
            return state;
        }
    }
}

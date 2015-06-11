using System;
using System.Collections.Generic;
using Elders.Cronus.DomainModeling;

namespace Elders.Cronus.Projections.ElasticSearch
{
    public sealed class StateBuilder<TState> : StateBuilder<TState, IEvent>
    {
    }

    public class StateBuilder<TState, TEvent>
    {
        private TState state;
        private readonly List<TEvent> events;
        private readonly Dictionary<Type, Func<object, TState, TState>> handlers;

        public StateBuilder(List<TEvent> events = null)
        {
            this.events = events ?? new List<TEvent>();
            this.handlers = new Dictionary<Type, Func<object, TState, TState>>();
        }

        public void Handle(TEvent e)
        {
            events.Add(e);
        }

        public StateBuilder<TState, TEvent> Init(Func<TState> state)
        {
            this.state = state();
            return this;
        }

        public StateBuilder<TState, TEvent> When<T>(Func<T, TState, TState> handler)
        {
            //  Check and throw custom exception when trying to handle an event type twice
            handlers.Add(typeof(T), (x, y) => handler((T)x, y));
            return this;
        }

        public TState Build()
        {
            var result = new StateBuilderResult<TState>();
            foreach (var item in events)
            {
                var t = item.GetType();
                var handler = handlers[t];
                state = handler(item, state);
            }
            return state;
        }

        public StateBuilderResult<TState> BuildNoMatterWhat()
        {
            var result = new StateBuilderResult<TState>();
            foreach (var item in events)
            {
                var t = item.GetType();
                var handler = handlers[t];
                try
                {
                    state = handler(item, state);
                }
                catch (Exception ex)
                {
                    result.Errors.Add(ex);
                }
            }
            result.Result = state;
            return result;
        }

        public class StateBuilderResult<TResult>
        {
            public StateBuilderResult()
            {
                Errors = new List<Exception>();
            }

            public TResult Result { get; internal set; }

            public List<Exception> Errors { get; internal set; }

            public bool IsSuccess { get { return Errors.Count == 0; } }
        }
    }
}

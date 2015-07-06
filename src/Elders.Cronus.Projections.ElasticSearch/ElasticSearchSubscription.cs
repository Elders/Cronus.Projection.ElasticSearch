using System;
using Elders.Cronus.DomainModeling;
using Elders.Cronus.MessageProcessing;

namespace Elders.Cronus.Projections.ElasticSearch
{
    public class ElasticSearchSubscription : MessageProcessorSubscription
    {
        private readonly IHandlerFactory handlerFactory;

        public ElasticSearchSubscription(string name, Type messageType, IHandlerFactory factory)
            : base(name, messageType, factory.MessageHandlerType)
        {
            handlerFactory = factory;
        }

        protected override void InternalOnNext(Message value)
        {
            dynamic handler = handlerFactory.Create();
            var theEvent = (IEvent)value.Payload;
            var entityEvent = theEvent as EntityEvent;
            if (ReferenceEquals(null, entityEvent) == false)
                theEvent = entityEvent;
            var se = new SearchableEvent()
            {
                Event = theEvent,
                AggregateId = value.Headers["ar_id"],
                Revision = int.Parse(value.Headers["ar_revision"]),
                Timestamp = DateTime.Parse(value.Headers["publish_timestamp"]),
                EventPosition = int.Parse(value.Headers["event_position"]),
            };
            handler.Handle((dynamic)se);
        }
    }
}

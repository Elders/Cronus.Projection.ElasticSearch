using System;
using Elders.Cronus.DomainModeling;
using Newtonsoft.Json;

namespace Elders.Cronus.Projections.ElasticSearch
{
    public class SearchableEvent
    {
        public string AggregateId { get; set; }

        public object EventInternal { get; set; }

        [JsonIgnore]
        public IEvent Event { get { return EventInternal as IEvent; } set { EventInternal = value as IEvent; } }

        public int EventPosition { get; set; }

        public int Revision { get; set; }

        public DateTime Timestamp { get; set; }
    }
}

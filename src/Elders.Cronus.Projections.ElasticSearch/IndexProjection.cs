using Elders.Cronus.DomainModeling;

namespace Elders.Cronus.Projections.ElasticSearch
{
    public class IndexProjection : IProjection
    {
        private readonly ProjectionApi projection;

        IndexProjection() { }

        public IndexProjection(ProjectionApi projectionApi)
        {
            projection = projectionApi;
        }

        public void Handle(SearchableEvent @event)
        {
            projection.Index(@event);
        }
    }
}

using Elders.Cronus.DomainModeling;

namespace Elders.Cronus.Projections.ElasticSearch.Linq
{
    public static class ProjectionQueryFactory
    {
        public static ProjectionQueryable<T> Queryable<T>(ProjectionApi projectionApi)
        {
            return new ProjectionQueryable<T>(projectionApi);
        }

        public static ProjectionQueryable<T> Query<T>(this ProjectionApi projectionApi) where T : IEvent
        {
            return new ProjectionQueryable<T>(projectionApi);
        }
    }
}
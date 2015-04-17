using System.Linq;
using System.Linq.Expressions;
using Remotion.Linq;
using Remotion.Linq.Parsing.Structure;

namespace Elders.Cronus.Projections.ElasticSearch.Linq
{
    /// <summary>
    /// Provides the main entry point to a LINQ query.
    /// </summary>
    public class ProjectionQueryable<T> : QueryableBase<T>
    {
        public ProjectionQueryable(ProjectionApi projectionApi) : base(QueryProviderFactory.CreateQueryProvider<T>(projectionApi)) { }

        public ProjectionQueryable(IQueryProvider provider, Expression expression) : base(provider, expression) { }
    }

    static class QueryProviderFactory
    {
        public static IQueryProvider CreateQueryProvider<T>(ProjectionApi session)
        {
            return new DefaultQueryProvider(typeof(ProjectionQueryable<>), QueryParser.CreateDefault(), CreateExecutor(session));
        }

        private static IQueryExecutor CreateExecutor(ProjectionApi session)
        {
            return new ProjectionExecutor(session);
        }
    }
}
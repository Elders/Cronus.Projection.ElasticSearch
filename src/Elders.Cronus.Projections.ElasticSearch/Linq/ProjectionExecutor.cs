using System.Collections.Generic;
using System.Linq;
using Elders.Cronus.DomainModeling;
using Remotion.Linq;

namespace Elders.Cronus.Projections.ElasticSearch.Linq
{
    public class ProjectionExecutor : IQueryExecutor
    {
        private readonly ProjectionApi projectionsApi;

        public ProjectionExecutor(ProjectionApi projectionsApi)
        {
            this.projectionsApi = projectionsApi;
        }

        /// <summary>
        /// Executes a query with a scalar result, i.e. a query that ends with a result operator such as Count, Sum, or Average.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queryModel">The query model.</param>
        /// <returns></returns>
        public T ExecuteScalar<T>(QueryModel queryModel)
        {
            return ExecuteCollection<T>(queryModel).Single();
        }

        /// <summary>
        /// Executes a query with a single result object, i.e. a query that ends with a result operator such as First, Last,
        /// Single, Min, or Max.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queryModel">The query model.</param>
        /// <param name="returnDefaultWhenEmpty">if set to <c>true</c> [return default when empty].</param>
        /// <returns></returns>
        public T ExecuteSingle<T>(QueryModel queryModel, bool returnDefaultWhenEmpty)
        {
            return returnDefaultWhenEmpty
                ? ExecuteCollection<T>(queryModel).SingleOrDefault()
                : ExecuteCollection<T>(queryModel).Single();
        }

        /// <summary>
        /// Executes a query with a collection result.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queryModel">The query model.</param>
        /// <returns></returns>
        public IEnumerable<T> ExecuteCollection<T>(QueryModel queryModel)
        {
            var elasticSearchRequest = ProjectionQueryModelVisitor.GenerateElasticSearchRequest(queryModel);

            List<IEvent> events = projectionsApi
                .MultiSearch(elasticSearchRequest)
                .OrderBy(x => x.Revision).ThenBy(x => x.EventPosition).ThenBy(x => x.Timestamp)
                .Select(x => x.Event).ToList();
            dynamic handler = (dynamic)FastActivator.CreateInstance(typeof(T));
            foreach (var item in events)
            {
                handler.Handle((dynamic)item);
            }
            yield return (T)handler;
        }
    }
}

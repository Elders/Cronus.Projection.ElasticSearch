using System.Collections.Generic;
using System.Linq;
using Remotion.Linq.Clauses;

namespace Elders.Cronus.Projections.ElasticSearch.Linq
{
    public class QueryPartsAggregator
    {
        public QueryPartsAggregator()
        {
            FromParts = new List<LuceneIndexExpression>();
            WhereParts = new List<LuceneIndexExpression>();
            OrderByParts = new List<string>();
        }

        public string SelectPart { get; set; }
        private List<LuceneIndexExpression> FromParts { get; set; }
        private List<LuceneIndexExpression> WhereParts { get; set; }
        private List<string> OrderByParts { get; set; }

        public void AddFromPart(LuceneIndexExpression querySource)
        {
            FromParts.Add(querySource);
        }

        public void AddWherePart(LuceneIndexExpression formatString)
        {
            WhereParts.Add(formatString);
        }

        public ElasticMultiSearchRequest Build()
        {
            var query = new ElasticMultiSearchRequest();

            foreach (var header in FromParts)
            {
                var item = new ElasticMultiSearchItem();
                item.Index = header.Index;
                var from = WhereParts.Where(x => x.Index.index == item.Index.index).SingleOrDefault();
                if (from != null)
                    item.Query = new QueryData() { query = new SearchQuery(from.FormatExpression()) };

                query.MultiIndexSearchItems.Add(item);
            }



            return query;
        }

        private string GetEntityName(IQuerySource querySource)
        {
            return querySource.ItemType.Name;
        }
    }
}
using System.Collections.Generic;
using Elders.Cronus.DomainModeling;

namespace Elders.Cronus.Projections.ElasticSearch.Linq
{
    public class ElasticMultiSearchRequest
    {
        public ElasticMultiSearchRequest()
        {
            MultiIndexSearchItems = new List<ElasticMultiSearchItem>();
        }

        public string Uri { get { return "_msearch"; } }

        public List<ElasticMultiSearchItem> MultiIndexSearchItems { get; set; }
    }

    public class ElasticMultiSearchItem
    {
        public ElasticMultiSearchItem()
        {
            Query = new QueryData();
        }

        public QueryIndex Index { get; set; }

        public QueryData Query { get; set; }

    }

    public class QueryIndex : ValueObject<QueryIndex>
    {
        public QueryIndex(string index)
        {
            this.index = index;
        }

        public string index { get; private set; }
    }

    public class QueryData
    {
        public QueryData()
        {
            query = new SearchQuery();
            from = 0;
            size = 1000;
        }

        public int size { get; private set; }
        public int from { get; private set; }

        public SearchQuery query { get; set; }

        public bool HasMore(int total)
        {
            return total >= from;
        }

        public QueryData NextPage()
        {
            var nextPage = new QueryData();
            nextPage.query = new SearchQuery(query.query_string.query);
            nextPage.from = from + size;
            nextPage.size = size;
            return nextPage;
        }
    }

    public class SearchQuery
    {
        public SearchQuery(string query = "*")
        {
            query_string = new SearchQueryString(query);
        }

        public SearchQueryString query_string { get; private set; }

        public class SearchQueryString
        {
            public SearchQueryString(string query = "*", string defaultOperator = "OR", string useDisMax = "true")
            {
                this.query = query;
                this.default_operator = defaultOperator;
                this.use_dis_max = useDisMax;
            }

            public string query { get; private set; }

            public string default_operator { get; private set; }

            public string use_dis_max { get; private set; }
        }
    }
}

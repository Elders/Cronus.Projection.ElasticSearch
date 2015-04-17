namespace Elders.Cronus.Projections.ElasticSearch.Linq
{
    public class ElasticMultiSearchResult
    {
        public ElasticSearchResult[] Responses { get; set; }
    }

    public class ElasticSearchResult
    {
        public Results Hits { get; set; }

        public string Error { get; set; }

        public bool IsSuccess { get { return Error == null; } }

        public bool HasResults { get { return IsSuccess && Hits != null && Hits.HasResults; } }
    }

    public class Results
    {
        public int Total { get; set; }
        public float? max_score { get; set; }
        public Result[] Hits { get; set; }

        public bool HasResults { get { return Hits != null && Hits.Length > 0; } }
    }

    public class Result
    {
        public string _index { get; set; }
        public string _type { get; set; }
        public string _id { get; set; }
        public float _score { get; set; }
        public object _source { get; set; }    //Document
    }
}

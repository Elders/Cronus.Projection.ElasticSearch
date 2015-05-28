using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Elders.Cronus.DomainModeling;
using Elders.Cronus.Projections.ElasticSearch.Linq;
using RestSharp;

namespace Elders.Cronus.Projections.ElasticSearch
{

    public class ProjectionApi
    {
        private readonly Json json;

        private readonly IRestClient client;

        private readonly ITypeEvaluator typeEvaluator;

        public ProjectionApi(string baseUrl, IContractsRepository contractsRepository)
        {
            this.json = new Json(contractsRepository);
            this.client = new RestClient(baseUrl);
            this.client.Timeout = 5000;
            this.typeEvaluator = new OverqualifiedNameInspector(1000);
        }

        public bool ConfigureMappings()
        {
            string body = @"
{
    ""template"": ""*"",
    ""settings"": {
        ""number_of_shards"": 1,
        ""number_of_replicas"": 0
    },
    ""mappings"": {
            ""_default_"": {
                ""_source"": {
                    ""enabled"": true
                },
            ""dynamic_templates"": [
                {
                    ""typecontract_store_noindex"": {
                        ""match"": ""$type"",
                        ""mapping"": {
                            ""index"": ""no"",
                            ""store"":""yes""
                        }
                    }
                },
                {
                    ""eventposition_store_noindex"": {
                        ""match"": ""EventPosition"",
                        ""mapping"": {
                            ""index"": ""no"",
                            ""store"":""yes""
                        }
                    }
                },
                {
                    ""revision_store_noindex"": {
                        ""match"": ""Revision"",
                        ""mapping"": {
                            ""index"": ""no"",
                            ""store"":""yes""
                        }
                    }
                },
                {
                    ""aggregateid_store_noindex"": {
                        ""match"": ""AggregateId"",
                        ""mapping"": {
                            ""index"": ""no"",
                            ""store"":""yes""
                        }
                    }
                },
                {
                    ""timestamp_store_noindex"": {
                        ""match"": ""Timestamp"",
                        ""mapping"": {
                            ""index"": ""no"",
                            ""store"":""yes""
                        }
                    }
                },
                {
                    ""do_not_analize"": {
                        ""match"": ""*"",
                        ""mapping"": {
                            ""index"": ""not_analyzed""
                        }
                    }
                }
            ]
        }
    }
}";
            var request = new RestRequest("_template/cronus_projection", Method.POST);

            request.AddParameter("text/json", body, ParameterType.RequestBody);
            var response = client.Execute(request);
            return response.StatusCode == System.Net.HttpStatusCode.OK;
        }

        public bool IndexExists(Type eventType)
        {
            var request = new RestRequest(eventType.GetContractId(), Method.GET);
            var response = client.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                return true;
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return false;
            else
                throw new Exception();
        }

        public bool CreateIndex(Type eventType)
        {
            var request = new RestRequest(eventType.GetContractId(), Method.POST);
            var body = "{\"settings\":{ \"number_of_shards\":1,\"number_of_replicas\":0}}";

            request.AddParameter("text/json", body, ParameterType.RequestBody);
            var response = client.Execute(request);
            return response.StatusCode == System.Net.HttpStatusCode.OK;
        }

        public bool Index(SearchableEvent @event)
        {
            var eventType = Uri.EscapeDataString(typeEvaluator.Evaluate(@event.EventInternal));
            var request = new RestRequest(@event.Event.GetType().GetContractId() + "/" + eventType, Method.POST);

            var body = json.Serialize(@event);
            request.AddParameter("text/json", body, ParameterType.RequestBody);
            var response = client.Execute(request);
            var isSuccess = response.StatusCode == System.Net.HttpStatusCode.Created;
            if (isSuccess == false)
            {
                string error =
                    "Unable to index event in Projections." + Environment.NewLine +
                    "Request: " + body + Environment.NewLine +
                    "Response: " + response.StatusCode + " " + response.ErrorMessage;
                throw new Exception(error);
            }
            return isSuccess;
        }

        public IEnumerable<SearchableEvent> MultiSearch(ElasticMultiSearchRequest elasticSearchRequest)
        {
            var reader = new ElasticSearchReader(this, elasticSearchRequest);
            return reader;
        }

        internal sealed class ElasticSearchReader : IEnumerable<SearchableEvent>
        {
            private readonly ProjectionApi projection;
            private readonly ElasticMultiSearchRequest elasticSearchRequest;

            public ElasticSearchReader(ProjectionApi projection, ElasticMultiSearchRequest elasticSearchRequest)
            {
                this.projection = projection;
                this.elasticSearchRequest = elasticSearchRequest;
            }

            public IEnumerator<SearchableEvent> GetEnumerator()
            {
                var pager = new Pager(projection, elasticSearchRequest);
                while (pager != null)
                {
                    var items = pager.Load();
                    foreach (var item in items)
                    {
                        yield return item;
                    }
                    pager = pager.NextPage();
                }
            }


            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            class Pager
            {
                private readonly ProjectionApi projection;
                private readonly ElasticMultiSearchRequest request;
                private ElasticMultiSearchResult response;

                public Pager(ProjectionApi projection, ElasticMultiSearchRequest request)
                {
                    this.projection = projection;
                    this.request = request;
                }

                public IEnumerable<SearchableEvent> Load()
                {
                    var request = new RestRequest(this.request.Uri, Method.POST);
                    var body = this.request.JsonBody();
                    request.AddParameter("text/json", body, ParameterType.RequestBody);

                    var rawResponse = projection.client.Execute(request);
                    response = projection.json.Deserialize<ElasticMultiSearchResult>(rawResponse.Content);

                    if (response.Responses != null)
                        return response.Responses
                            .Where(x => x.HasResults)
                            .SelectMany(z => z.Hits.Hits.Select(x => x._source as SearchableEvent));

                    return Enumerable.Empty<SearchableEvent>();
                }

                public Pager NextPage()
                {
                    if (request.MultiIndexSearchItems.Count == 0)
                        return null;

                    var pageRequest = new ElasticMultiSearchRequest();
                    foreach (var rq in request.MultiIndexSearchItems)
                    {
                        var matchingResponse = response.Responses
                            .Where(x => x.HasResults)
                            .SingleOrDefault(x => x.Hits.Hits.Any(r => r._index == rq.Index.index));

                        if (matchingResponse == null)
                            continue;

                        if (rq.Query.HasMore(matchingResponse.Hits.Total))
                        {
                            var nextPageQuery = rq.Query.NextPage();
                            var indexQuery = new ElasticMultiSearchItem()
                            {
                                Index = new QueryIndex(rq.Index.index),
                                Query = nextPageQuery,
                            };
                            pageRequest.MultiIndexSearchItems.Add(indexQuery);
                        }
                    }

                    if (pageRequest.MultiIndexSearchItems.Count == 0)
                        return null;

                    return new Pager(projection, pageRequest);
                }
            }
        }
    }

    internal static class ElasticMultiSearchRequestExtensions
    {
        internal static string JsonBody(this ElasticMultiSearchRequest requestData)
        {
            string body = string.Empty;
            foreach (var searchItem in requestData.MultiIndexSearchItems)
            {
                body += Newtonsoft.Json.JsonConvert.SerializeObject(searchItem.Index) + System.Environment.NewLine;
                body += Newtonsoft.Json.JsonConvert.SerializeObject(searchItem.Query) + System.Environment.NewLine;
            }
            body += System.Environment.NewLine;
            return body;
        }
    }
}

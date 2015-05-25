using System.Collections.ObjectModel;
using System.Linq.Expressions;
using Elders.Cronus.DomainModeling;
using Remotion.Linq;
using Remotion.Linq.Clauses;

namespace Elders.Cronus.Projections.ElasticSearch.Linq
{

    public class SubQueryModelVisitor : QueryModelVisitorBase
    {
        public static ElasticMultiSearchRequest GenerateElasticSearchRequest(QueryModel queryModel)
        {
            var visitor = new ProjectionQueryModelVisitor();
            visitor.VisitQueryModel(queryModel);
            return visitor.GetElasticSearchRequest();
        }

        private readonly QueryPartsAggregator _queryParts = new QueryPartsAggregator();

        public ElasticMultiSearchRequest GetElasticSearchRequest()
        {
            return _queryParts.Build();
        }

        public override void VisitResultOperator(ResultOperatorBase resultOperator, QueryModel queryModel, int index)
        {
            base.VisitResultOperator(resultOperator, queryModel, index);
        }

        public override void VisitMainFromClause(MainFromClause fromClause, QueryModel queryModel)
        {
            var luceneExpression = new LuceneIndexExpression();
            luceneExpression.AttachIndex(fromClause.ItemType.GetContractId());
            luceneExpression.Append(fromClause.ItemType.GetContractId());
            _queryParts.AddFromPart(luceneExpression);

            fromClause.ItemName = "EventInternal";

            base.VisitMainFromClause(fromClause, queryModel);
        }

        public override void VisitSelectClause(SelectClause selectClause, QueryModel queryModel)
        {
            _queryParts.SelectPart = selectClause.Selector.Type.Name;

            base.VisitSelectClause(selectClause, queryModel);
        }

        public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index)
        {
            _queryParts.AddWherePart(GetLuceneExpression(whereClause.Predicate));

            base.VisitWhereClause(whereClause, queryModel, index);
        }

        public override void VisitQueryModel(QueryModel queryModel)
        {
            base.VisitQueryModel(queryModel);
        }

        protected override void VisitResultOperators(ObservableCollection<ResultOperatorBase> resultOperators, QueryModel queryModel)
        {
            base.VisitResultOperators(resultOperators, queryModel);
        }

        public override void VisitAdditionalFromClause(AdditionalFromClause fromClause, QueryModel queryModel, int index)
        {
            var luceneExpression = new LuceneIndexExpression();
            luceneExpression.AttachIndex(fromClause.ItemType.GetContractId());
            luceneExpression.Append(fromClause.ItemType.GetContractId());
            _queryParts.AddFromPart(luceneExpression);

            fromClause.ItemName = "EventInternal";

            base.VisitAdditionalFromClause(fromClause, queryModel, index);
        }

        private LuceneIndexExpression GetLuceneExpression(Expression expression)
        {
            return ElasticSearchExpressionTreeVisitor.GetLuceneExpression(expression);
        }
    }
}

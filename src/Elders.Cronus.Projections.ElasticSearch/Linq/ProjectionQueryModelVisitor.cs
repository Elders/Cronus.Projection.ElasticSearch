using System.Collections.ObjectModel;
using System.Linq.Expressions;
using Elders.Cronus.DomainModeling;
using Remotion.Linq;
using Remotion.Linq.Clauses;

namespace Elders.Cronus.Projections.ElasticSearch.Linq
{

    public class ProjectionQueryModelVisitor : QueryModelVisitorBase
    {
        public static ElasticMultiSearchRequest GenerateElasticSearchRequest(QueryModel queryModel)
        {
            var visitor = new ProjectionQueryModelVisitor();
            visitor.VisitQueryModel(queryModel);
            return visitor.GetElasticSearchRequest();
        }

        //// Instead of generating an HQL string, we could also use a NHibernate ASTFactory to generate IASTNodes.
        private readonly QueryPartsAggregator _queryParts = new QueryPartsAggregator();

        public ElasticMultiSearchRequest GetElasticSearchRequest()
        {
            return _queryParts.Build();
        }

        //public override void VisitQueryModel(QueryModel queryModel)
        //{
        //    queryModel.SelectClause.Accept(this, queryModel);
        //    queryModel.MainFromClause.Accept(this, queryModel);
        //    VisitBodyClauses(queryModel.BodyClauses, queryModel);
        //    VisitResultOperators(queryModel.ResultOperators, queryModel);
        //}

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

        //public override void VisitOrderByClause(OrderByClause orderByClause, QueryModel queryModel, int index)
        //{
        //    _queryParts.AddOrderByPart(orderByClause.Orderings.Select(o => GetHqlExpression(o.Expression)));

        //    base.VisitOrderByClause(orderByClause, queryModel, index);
        //}

        //public override void VisitJoinClause(JoinClause joinClause, QueryModel queryModel, int index)
        //{
        //    // HQL joins work differently, need to simulate using a cross join with a where condition

        //    _queryParts.AddFromPart(joinClause);
        //    _queryParts.AddWherePart(
        //        "({0} = {1})",
        //        GetHqlExpression(joinClause.OuterKeySelector),
        //        GetHqlExpression(joinClause.InnerKeySelector));

        //    base.VisitJoinClause(joinClause, queryModel, index);
        //}


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

        //public override void VisitGroupJoinClause(GroupJoinClause groupJoinClause, QueryModel queryModel, int index)
        //{
        //    throw new NotSupportedException("Adding a join ... into ... implementation to the query provider is left to the reader for extra points.");
        //}

        public override void VisitGroupJoinClause(GroupJoinClause groupJoinClause, QueryModel queryModel, int index)
        {
            base.VisitGroupJoinClause(groupJoinClause, queryModel, index);
        }

        private LuceneIndexExpression GetLuceneExpression(Expression expression)
        {
            return ElasticSearchExpressionTreeVisitor.GetLuceneExpression(expression);
        }
    }
}

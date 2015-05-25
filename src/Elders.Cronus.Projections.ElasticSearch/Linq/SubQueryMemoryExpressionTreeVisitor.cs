using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Elders.Cronus.DomainModeling;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.ExpressionTreeVisitors;
using Remotion.Linq.Parsing;

namespace Elders.Cronus.Projections.ElasticSearch.Linq
{
    public class SubLuceneIndexExpression
    {
        public SubLuceneIndexExpression()
        {
            ExpressionBuilder = new StringBuilder();
        }

        public QueryIndex Index { get; private set; }

        public StringBuilder ExpressionBuilder { get; private set; }

        public void AttachIndex(string index)
        {
            Index = new QueryIndex(index);
        }

        public void Append(string expressionPart)
        {
            ExpressionBuilder.Append(expressionPart);
        }

        public string FormatExpression()
        {
            return "+(" + ExpressionBuilder.ToString() + ")";
        }
    }

    public class SubQueryMemoryExpressionTreeVisitor : ThrowingExpressionTreeVisitor
    {
        public static LuceneIndexExpression GetLuceneExpression(Expression linqExpression)
        {
            var visitor = new SubQueryMemoryExpressionTreeVisitor();
            visitor.VisitExpression(linqExpression);
            return visitor.GetLuceneExpression();
        }

        private readonly LuceneIndexExpression luceneExpression;

        private SubQueryMemoryExpressionTreeVisitor()
        {
            luceneExpression = new LuceneIndexExpression();
        }

        private LuceneIndexExpression GetLuceneExpression()
        {
            return luceneExpression;
        }

        protected override Expression VisitQuerySourceReferenceExpression(QuerySourceReferenceExpression expression)
        {
            luceneExpression.AttachIndex(expression.ReferencedQuerySource.ItemType.GetContractId());
            luceneExpression.Append(expression.ReferencedQuerySource.ItemName);
            return expression;
        }

        protected override Expression VisitBinaryExpression(BinaryExpression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Equal:  //  left:right
                    VisitExpression(expression.Left);
                    luceneExpression.Append(":");
                    VisitExpression(expression.Right);
                    break;
                case ExpressionType.NotEqual:   //  -(left:right)
                    luceneExpression.Append("-");
                    luceneExpression.Append("(");
                    VisitExpression(expression.Left);
                    luceneExpression.Append(":");
                    VisitExpression(expression.Right);
                    luceneExpression.Append(")");
                    break;
                case ExpressionType.AndAlso:
                case ExpressionType.And:    //  +(left)+(right)

                    luceneExpression.Append("+");
                    luceneExpression.Append("(");
                    VisitExpression(expression.Left);
                    luceneExpression.Append(")");

                    luceneExpression.Append("+");
                    luceneExpression.Append("(");
                    VisitExpression(expression.Right);
                    luceneExpression.Append(")");
                    break;

                case ExpressionType.OrElse:     //  (left)|(right)
                case ExpressionType.Or:

                    luceneExpression.Append("(");
                    VisitExpression(expression.Left);
                    luceneExpression.Append(")");
                    luceneExpression.Append("|");
                    luceneExpression.Append("(");
                    VisitExpression(expression.Right);
                    luceneExpression.Append(")");
                    break;

                case ExpressionType.Not:    //  -(left)
                    luceneExpression.Append("-");
                    luceneExpression.Append("(");
                    VisitExpression(expression.Left);
                    luceneExpression.Append(")");
                    break;


                default:
                    base.VisitBinaryExpression(expression);
                    break;
            }

            return expression;
        }


        private Expression VisitMemberExpressionInternal(MemberExpression expression)
        {
            if (expression.Expression is MemberExpression)
            {
                VisitMemberExpressionInternal(expression.Expression as MemberExpression);
            }
            else
            {
                VisitExpression(expression.Expression);
            }

            var contractOrder = expression.Member.CustomAttributes
                .Where(attr => typeof(DataMemberAttribute).IsAssignableFrom(attr.AttributeType))
                .SingleOrDefault();

            if (contractOrder == null)
            {
                string internalFieldName = expression.Member.Name + "Internal";
                var internalField = expression.Member.DeclaringType.GetMember(internalFieldName, BindingFlags.NonPublic | BindingFlags.Instance).SingleOrDefault();
                contractOrder = internalField.CustomAttributes
                                                .Where(attr => typeof(DataMemberAttribute).IsAssignableFrom(attr.AttributeType))
                                                .SingleOrDefault();
            }

            var memberName = contractOrder == null
                ? expression.Member.Name
                : contractOrder.NamedArguments.Where(arg => arg.MemberName == "Order").Select(x => x.TypedValue.Value).Single();
            var propertyInfo = expression.Member as PropertyInfo;
            luceneExpression.Append("." + memberName);
            if (propertyInfo != null && typeof(byte[]).IsAssignableFrom(propertyInfo.PropertyType))//Simple type
                luceneExpression.Append(".$value");


            return expression;
        }

        bool reduced = false;
        protected override Expression VisitMemberExpression(MemberExpression expression)
        {
            var aggregateIdProperty = expression.Member as PropertyInfo;
            var canReduce = aggregateIdProperty != null && reduced == false &&
                typeof(IAggregateRootId).IsAssignableFrom(aggregateIdProperty.PropertyType) && aggregateIdProperty.PropertyType.IsInterface == false;
            reduced = true;
            if (canReduce)
                return VisitExpression(new ArExpression(expression, aggregateIdProperty.PropertyType));
            else
                return VisitMemberExpressionInternal(expression);
        }


        private Expression VisitArExpression(ArExpression expression)
        {
            var exp = base.VisitExpression(expression.Expression);

            var rawIdIndex = expression.ArTyoe
                .GetAllMembers().Where(x => x.Name == "RawId")
                .Single().GetAttrubuteValue<DataMemberAttribute, int>(attr => attr.Order)
                .ToString();

            luceneExpression.Append(string.Format(".{0}.$value", rawIdIndex));

            return expression;
        }
        public class ArExpression : Expression
        {
            public Expression Expression { get; private set; }
            public Type ArTyoe { get; private set; }
            public ArExpression(Expression ex, Type arType)
            {
                Expression = ex;
                ArTyoe = arType;
            }
            public override Type Type { get { return Expression.Type; } }
            public override ExpressionType NodeType { get { return Expression.NodeType; } }
            public override bool CanReduce { get { return Expression.CanReduce; } }
            public override Expression Reduce() { return Expression.Reduce(); }
        }
        protected override MemberBinding VisitMemberAssignment(MemberAssignment memberAssigment)
        {
            return base.VisitMemberAssignment(memberAssigment);
        }

        protected override Expression VisitConditionalExpression(ConditionalExpression expression)
        {
            return base.VisitConditionalExpression(expression);
        }

        protected override ElementInit VisitElementInit(ElementInit elementInit)
        {
            return base.VisitElementInit(elementInit);
        }

        protected override Expression VisitMemberInitExpression(MemberInitExpression expression)
        {
            return base.VisitMemberInitExpression(expression);
        }

        protected override MemberBinding VisitMemberBinding(MemberBinding memberBinding)
        {
            return base.VisitMemberBinding(memberBinding);
        }

        public override ReadOnlyCollection<T> VisitAndConvert<T>(ReadOnlyCollection<T> expressions, string callerName)
        {
            return base.VisitAndConvert<T>(expressions, callerName);
        }

        public override T VisitAndConvert<T>(T expression, string methodName)
        {
            return base.VisitAndConvert<T>(expression, methodName);
        }

        protected override ReadOnlyCollection<ElementInit> VisitElementInitList(ReadOnlyCollection<ElementInit> expressions)
        {
            return base.VisitElementInitList(expressions);
        }

        public override Expression VisitExpression(Expression expression)
        {
            if (expression is ArExpression)
                return VisitArExpression(expression as ArExpression);
            else
                return base.VisitExpression(expression);
        }

        protected override Expression VisitExtensionExpression(ExtensionExpression expression)
        {
            return base.VisitExtensionExpression(expression);
        }

        protected override Expression VisitInvocationExpression(InvocationExpression expression)
        {
            return VisitExpression(expression.Expression);
        }

        protected override Expression VisitConstantExpression(ConstantExpression expression)
        {
            if (typeof(IAggregateRootId).IsAssignableFrom(expression.Type))
            {
                var aggregateId = expression.Value as IAggregateRootId;
                var aggregateIdAsBase64String = System.Convert.ToBase64String(aggregateId.RawId);
                luceneExpression.Append(string.Format("(\"{0}\")", aggregateIdAsBase64String));
            }
            else if (typeof(byte[]).IsAssignableFrom(expression.Type))
            {
                var valueAsBase64String = System.Convert.ToBase64String(expression.Value as byte[]);
                luceneExpression.Append(string.Format("(\"{0}\")", valueAsBase64String));
            }
            else
            {
                luceneExpression.Append(string.Format("(\"{0}\")", expression.Value));
            }

            return expression;
        }

        protected override Expression VisitLambdaExpression(LambdaExpression expression)
        {
            return base.VisitLambdaExpression(expression);
        }

        protected override Expression VisitListInitExpression(ListInitExpression expression)
        {
            return base.VisitListInitExpression(expression);
        }

        protected override ReadOnlyCollection<MemberBinding> VisitMemberBindingList(ReadOnlyCollection<MemberBinding> expressions)
        {
            return base.VisitMemberBindingList(expressions);
        }

        protected override MemberBinding VisitMemberListBinding(MemberListBinding listBinding)
        {
            return base.VisitMemberListBinding(listBinding);
        }

        protected override MemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
        {
            return base.VisitMemberMemberBinding(binding);
        }

        protected override Expression VisitNewArrayExpression(NewArrayExpression expression)
        {
            return base.VisitNewArrayExpression(expression);
        }

        protected override Expression VisitNewExpression(NewExpression expression)
        {
            return base.VisitNewExpression(expression);
        }

        protected override Expression VisitParameterExpression(ParameterExpression expression)
        {
            return base.VisitParameterExpression(expression);
        }

        protected override Expression VisitSubQueryExpression(SubQueryExpression expression)
        {
            var asd = ProjectionQueryModelVisitor.GenerateElasticSearchRequest(expression.QueryModel);
            return expression;
            return base.VisitSubQueryExpression(expression);
        }

        protected override Expression VisitTypeBinaryExpression(TypeBinaryExpression expression)
        {
            return base.VisitTypeBinaryExpression(expression);
        }

        protected override Expression VisitUnaryExpression(UnaryExpression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Not:    //  -(left)
                    luceneExpression.Append("-");
                    luceneExpression.Append("(");
                    VisitExpression(expression.Operand);
                    luceneExpression.Append(")");
                    break;
                case ExpressionType.TypeAs:
                case ExpressionType.Convert:
                    var memberExp = expression.Operand as MemberExpression;
                    if (memberExp != null && reduced == false)
                    {
                        if (typeof(IAggregateRootId).IsAssignableFrom(expression.Type))
                            VisitExpression(new ArExpression(expression.Operand, expression.Type));
                        return expression;
                    }
                    else
                        base.VisitExpression(expression.Operand);
                    break;
                default:
                    base.VisitUnaryExpression(expression);
                    break;
            }

            return expression;
        }



        protected override TResult VisitUnhandledItem<TItem, TResult>(TItem unhandledItem, string visitMethod, Func<TItem, TResult> baseBehavior)
        {
            return base.VisitUnhandledItem<TItem, TResult>(unhandledItem, visitMethod, baseBehavior);
        }

        protected override Expression VisitUnknownNonExtensionExpression(Expression expression)
        {
            return base.VisitUnknownNonExtensionExpression(expression);
        }

        protected override Expression VisitMethodCallExpression(MethodCallExpression expression)
        {
            // In production code, handle this via method lookup tables.

            var supportedMethod = typeof(string).GetMethod("Contains");
            if (expression.Method.Equals(supportedMethod))
            {
                luceneExpression.Append("(");
                VisitExpression(expression.Object);
                luceneExpression.Append(" like '%'+");
                VisitExpression(expression.Arguments[0]);
                luceneExpression.Append("+'%')");
                return expression;
            }
            else
            {
                return base.VisitMethodCallExpression(expression); // throws
            }
        }

        // Called when a LINQ expression type is not handled above.
        protected override Exception CreateUnhandledItemException<T>(T unhandledItem, string visitMethod)
        {
            string itemText = FormatUnhandledItem(unhandledItem);
            var message = string.Format("The expression '{0}' (type: {1}) is not supported by this LINQ provider.", itemText, typeof(T));
            return new NotSupportedException(message);
        }

        private string FormatUnhandledItem<T>(T unhandledItem)
        {
            var itemAsExpression = unhandledItem as Expression;
            return itemAsExpression != null ? FormattingExpressionTreeVisitor.Format(itemAsExpression) : unhandledItem.ToString();
        }
    }
}

﻿namespace LinqToQuerystring.TreeNodes
{
    using System.Diagnostics;
    using System.Linq;
    using System.Linq.Expressions;

    using Antlr.Runtime;

    using LinqToQuerystring.TreeNodes.Base;

    public class AscNode : ExplicitOrderByBase
    {
        public AscNode(IToken payload)
            : base(payload)
        {
        }

        public override Expression BuildLinqExpression<T>(IQueryable query, Expression expression, Expression item = null)
        {
            var parameter = item ?? Expression.Parameter(typeof(T), "o");
            Expression childExpression = expression;

            var temp = parameter;

            foreach (var child in this.Children.Cast<TreeNode>())
            {
                childExpression = child.BuildLinqExpression<T>(query, childExpression, temp);
                temp = childExpression;
            }

            Debug.Assert(childExpression != null, "childExpression should never be null");

            var methodName = "OrderBy";
            if (query.Provider.GetType().Name.Contains("DbQueryProvider") && !this.IsFirstChild)
            {
                methodName = "ThenBy";
            }

            var lambda = Expression.Lambda(childExpression, new[] { parameter as ParameterExpression });
            return Expression.Call(typeof(Queryable), methodName, new[] { query.ElementType, childExpression.Type }, query.Expression, lambda);
        }
    }
}
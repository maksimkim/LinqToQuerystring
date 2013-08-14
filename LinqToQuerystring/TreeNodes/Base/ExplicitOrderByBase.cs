namespace LinqToQuerystring.TreeNodes.Base
{
    using System.Diagnostics;
    using System.Linq.Expressions;
    using Antlr.Runtime;

    public abstract class ExplicitOrderByBase : SingleChildNode
    {
        protected ExplicitOrderByBase(IToken payload, TreeNodeFactory treeNodeFactory)
            : base(payload, treeNodeFactory)
        {
        }

        public override Expression BuildLinqExpression(Expression item = null)
        {
            var childExpression = ChildNode.BuildLinqExpression(item);

            Debug.Assert(childExpression != null, "childExpression should never be null");

            var lambda = Expression.Lambda(childExpression, new[] { item as ParameterExpression });

            return lambda;
        }
    }
}
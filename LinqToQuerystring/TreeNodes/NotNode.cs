namespace LinqToQuerystring.TreeNodes
{
    using System;
    using System.Linq.Expressions;

    using Antlr.Runtime;

    using LinqToQuerystring.TreeNodes.Base;

    public class NotNode : UnaryNode
    {
        public NotNode(IToken payload, TreeNodeFactory treeNodeFactory)
            : base(payload, treeNodeFactory)
        {
        }

        public override Expression BuildLinqExpression(Expression item = null)
        {
            var childExpression = this.ChildNode.BuildLinqExpression(item);

            if (!typeof(bool).IsAssignableFrom(childExpression.Type))
            {
                childExpression = Expression.Convert(childExpression, typeof(bool));
            }

            return Expression.Not(childExpression);
        }
    }
}
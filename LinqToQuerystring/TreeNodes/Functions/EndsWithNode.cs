namespace LinqToQuerystring.TreeNodes.Functions
{
    using System;
    using System.Linq.Expressions;

    using Antlr.Runtime;

    using LinqToQuerystring.TreeNodes.Base;

    public class EndsWithNode : BinaryNode
    {
        public EndsWithNode(IToken payload, TreeNodeFactory treeNodeFactory)
            : base(payload, treeNodeFactory)
        {
        }

        public override Expression BuildLinqExpression(Expression item = null)
        {
            var leftExpression = this.LeftNode.BuildLinqExpression(item);
            var rightExpression = this.RightNode.BuildLinqExpression(item);

            if (!leftExpression.Type.IsAssignableFrom(typeof(string)))
            {
                leftExpression = Expression.Convert(leftExpression, typeof(string));
            }

            if (!rightExpression.Type.IsAssignableFrom(typeof(string)))
            {
                rightExpression = Expression.Convert(rightExpression, typeof(string));
            }

            return Expression.Call(leftExpression, "EndsWith", null, new[] { rightExpression });
        }
    }
}
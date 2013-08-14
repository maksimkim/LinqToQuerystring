namespace LinqToQuerystring.TreeNodes.Comparisons
{
    using System;
    using System.Linq.Expressions;

    using Antlr.Runtime;

    using LinqToQuerystring.TreeNodes.Base;

    public class NotEqualsNode : BinaryNode
    {
        public NotEqualsNode(IToken payload, TreeNodeFactory treeNodeFactory)
            : base(payload, treeNodeFactory)
        {
        }

        public override Expression BuildLinqExpression(Expression item = null)
        {
            var leftExpression = this.LeftNode.BuildLinqExpression(item);
            var rightExpression = this.RightNode.BuildLinqExpression(item);

            NormalizeTypes(ref leftExpression, ref rightExpression);

            return ApplyWithNullAsValidAlternative(Expression.NotEqual, leftExpression, rightExpression);
        }
    }
}
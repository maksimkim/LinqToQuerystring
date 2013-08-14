namespace LinqToQuerystring.TreeNodes
{
    using System;
    using System.Linq.Expressions;

    using Antlr.Runtime;

    using LinqToQuerystring.TreeNodes.Base;

    public class FilterNode : UnaryNode
    {
        public FilterNode(IToken payload, TreeNodeFactory treeNodeFactory)
            : base(payload, treeNodeFactory)
        {
        }

        public LambdaExpression BuildFilter(Type itemType)
        {
            var parameter = Expression.Parameter(itemType, "o");

            return Expression.Lambda(this.ChildNode.BuildLinqExpression(parameter), parameter);
        }

        public override Expression BuildLinqExpression(Expression item)
        {
            throw new NotSupportedException("Filter is just a placeholder and should be handled differently in Extensions.cs");
        }

        public override int CompareTo(TreeNode other)
        {
            return -1;
        }
    }
}
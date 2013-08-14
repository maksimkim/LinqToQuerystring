namespace LinqToQuerystring.TreeNodes.DataTypes
{
    using System;
    using System.Linq.Expressions;

    using Antlr.Runtime;

    using LinqToQuerystring.TreeNodes.Base;

    public class NullNode : TreeNode
    {
        public NullNode(IToken payload, TreeNodeFactory treeNodeFactory)
            : base(payload, treeNodeFactory)
        {
        }

        public override Expression BuildLinqExpression(Expression item = null)
        {
            return Expression.Constant(null);
        }
    }
}
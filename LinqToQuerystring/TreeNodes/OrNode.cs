namespace LinqToQuerystring.TreeNodes
{
    using System;
    using System.Linq.Expressions;

    using Antlr.Runtime;

    using LinqToQuerystring.TreeNodes.Base;

    public class OrNode : TwoChildNode
    {
        public OrNode(IToken payload, TreeNodeFactory treeNodeFactory)
            : base(payload, treeNodeFactory)
        {
        }

        public override Expression BuildLinqExpression(Expression item = null)
        {
            return Expression.Or(
                this.LeftNode.BuildLinqExpression(item),
                this.RightNode.BuildLinqExpression(item));
        }
    }
}
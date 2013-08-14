namespace LinqToQuerystring.TreeNodes.Aggregates
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;

    using Antlr.Runtime;

    using LinqToQuerystring.TreeNodes.Base;

    public class SumNode : TreeNode
    {
        public SumNode(IToken payload, TreeNodeFactory treeNodeFactory)
            : base(payload, treeNodeFactory)
        {
        }

        public override Expression BuildLinqExpression(Expression item = null)
        {
            var property = this.ChildNodes.ElementAt(0).BuildLinqExpression(item);
            return Expression.Call(typeof(Enumerable), "Sum", null, property);
        }
    }
}

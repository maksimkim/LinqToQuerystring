namespace LinqToQuerystring.TreeNodes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Antlr.Runtime;

    using Base;

    public class ExpandNode : TreeNode
    {
        public ExpandNode(IToken payload, TreeNodeFactory treeNodeFactory)
            : base(payload, treeNodeFactory)
        {
        }

        public IEnumerable<LambdaExpression> BuildExpands(Type elementType)
        {
            return 
                ChildNodes
                .Select(child =>
                {
                    var param = Expression.Parameter(elementType, "o");
                    return Expression.Lambda(child.BuildLinqExpression(param), param);
                });
        }

        public override Expression BuildLinqExpression(Expression item = null)
        {
            throw new NotSupportedException(
                "Orderby is just a placeholder and should be handled differently in Extensions.cs");
        }

        public override int CompareTo(TreeNode other)
        {
            return -1;
        }
    }
}
namespace LinqToQuerystring.TreeNodes.Functions
{
    using System;
    using System.Linq.Expressions;

    using Antlr.Runtime;

    using LinqToQuerystring.TreeNodes.Base;

    public class ToLowerNode : UnaryNode
    {
        public ToLowerNode(IToken payload, TreeNodeFactory treeNodeFactory)
            : base(payload, treeNodeFactory)
        {
        }

        public override Expression BuildLinqExpression(Expression item = null)
        {
            var childexpression = this.ChildNode.BuildLinqExpression(item);

            if (!childexpression.Type.IsAssignableFrom(typeof(string)))
            {
                childexpression = Expression.Convert(childexpression, typeof(string));
            }

            return Expression.Call(childexpression, "ToLower", null, null);
        }
    }
}
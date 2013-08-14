namespace LinqToQuerystring.TreeNodes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    using Antlr.Runtime;

    using Base;

    public class OrderByNode : TreeNode
    {
        public IEnumerable<SortDescription> BuildSorts(Type elementType)
        {
            foreach (var child in Children.Cast<UnaryNode>())
            {
                bool desc;

                if (child is AscNode)
                    desc = false;
                else if (child is DescNode)
                    desc = true;
                else
                    throw new InvalidOperationException();

                var orderExpression = (LambdaExpression)child.BuildLinqExpression(Expression.Parameter(elementType, "o"));

                yield return new SortDescription(orderExpression, desc);
            }
        }
        
        public OrderByNode(IToken payload, TreeNodeFactory treeNodeFactory)
            : base(payload, treeNodeFactory)
        {
        }

        public override Expression BuildLinqExpression(Expression item = null)
        {
            throw new NotSupportedException(
                "Orderby is just a placeholder and should be handled differently in Extensions.cs");
        }

        public override int CompareTo(TreeNode other)
        {
            if (other is FilterNode)
            {
                return 1;
            }

            if (other is ExpandNode)
            {
                return 1;
            }

            return -1;
        }
    }
}
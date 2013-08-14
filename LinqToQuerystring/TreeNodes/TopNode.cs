namespace LinqToQuerystring.TreeNodes
{
    using System;
    using System.Linq.Expressions;

    using Antlr.Runtime;
    using DataTypes;
    using Base;

    public class TopNode : UnaryNode
    {
        public TopNode(IToken payload, TreeNodeFactory treeNodeFactory)
            : base(payload, treeNodeFactory)
        {
        }

        public int Value
        {
            get { return (ChildNode as ConstantNode<int>).Value; }
        }

        public override Expression BuildLinqExpression(Expression item = null)
        {
            throw new NotSupportedException("Top is just a placeholder and should be handled differently in Extensions.cs");
        }

        public override int CompareTo(TreeNode other)
        {
            if (other is OrderByNode || other is FilterNode || other is SkipNode)
            {
                return 1;
            }

            return -1;
        }
    }
}
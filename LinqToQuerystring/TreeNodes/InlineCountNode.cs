﻿namespace LinqToQuerystring.TreeNodes
{
    using System;
    using System.Linq.Expressions;

    using Antlr.Runtime;

    using LinqToQuerystring.TreeNodes.Base;

    public class InlineCountNode : SingleChildNode
    {
        public InlineCountNode(IToken payload, TreeNodeFactory treeNodeFactory)
            : base(payload, treeNodeFactory)
        {
        }

        public override Expression BuildLinqExpression(Expression item = null)
        {
            throw new NotSupportedException(
                "InlineCountNode is just a placeholder and should be handled differently in Extensions.cs");
        }

        public override int CompareTo(TreeNode other)
        {
            return 1;
        }
    }
}
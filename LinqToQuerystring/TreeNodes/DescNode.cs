namespace LinqToQuerystring.TreeNodes
{
    using System;
    using System.Diagnostics;
    using System.Linq.Expressions;

    using Antlr.Runtime;

    using Base;

    public class DescNode : ExplicitOrderByBase
    {
        public DescNode(IToken payload, TreeNodeFactory treeNodeFactory)
            : base(payload, treeNodeFactory)
        {
        }
    }
}
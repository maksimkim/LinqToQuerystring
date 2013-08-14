namespace LinqToQuerystring.TreeNodes
{
    using System;
    using System.Diagnostics;
    using System.Linq.Expressions;

    using Antlr.Runtime;

    using Base;

    public class AscNode : ExplicitOrderByBase
    {
        public AscNode(IToken payload, TreeNodeFactory treeNodeFactory)
            : base(payload, treeNodeFactory)
        {
        }
    }
}
namespace LinqToQuerystring
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using Antlr.Runtime.Tree;
    using Exceptions;

    public static class CommonTreeExtensions
    {
        public static IEnumerable<CommonTree> ChildNodes(this CommonTree node)
        {
            return node.Children.Cast<CommonTree>();
        }

        public static CommonTree Child(this CommonTree node)
        {
            Contract.Assert(node.ChildCount == 1);
            
            return node.ChildNodes().First();
        }

        public static void Invalid(this ITree node)
        {
            throw new QueryParserException("Invalid 'token' " + node.Text);
        }
    }
}
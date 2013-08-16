namespace LinqToQuerystring
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using Antlr.Runtime.Tree;

    public static class CommonTreeExtensions
    {
        public static IEnumerable<CommonTree> TreeChildren(this CommonTree node)
        {
            return node.Children.Cast<CommonTree>();
        }

        public static CommonTree Child(this CommonTree node)
        {
            Contract.Assert(node.ChildCount == 1);
            
            return node.TreeChildren().First();
        }

        public static void Invalid(this ITree node)
        {
            throw new InvalidOperationException("Invalid 'token' " + node.Text);
        }
    }
}
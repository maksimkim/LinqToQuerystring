namespace LinqToQuerystring.TreeNodes.DataTypes
{
    using System;
    using System.Globalization;
    using Antlr.Runtime;

    public class IntNode : ConstantNode<int>
    {
        public IntNode(IToken payload, TreeNodeFactory treeNodeFactory)
            : base(payload, treeNodeFactory)
        {
        }

        protected override int ParseValue(string text)
        {
            return Convert.ToInt32(text, CultureInfo.InvariantCulture);
        }
    }
}
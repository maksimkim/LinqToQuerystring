namespace LinqToQuerystring.TreeNodes.DataTypes
{
    using System;
    using System.Globalization;
    using Antlr.Runtime;

    public class BoolNode : ConstantNode<bool>
    {
        public BoolNode(IToken payload, TreeNodeFactory treeNodeFactory)
            : base(payload, treeNodeFactory)
        {
        }

        protected override bool ParseValue(string text)
        {
            return Convert.ToBoolean(text, CultureInfo.InvariantCulture);
        }
    }
}
namespace LinqToQuerystring.TreeNodes.DataTypes
{
    using System;
    using System.Globalization;
    using Antlr.Runtime;

    public class DoubleNode : ConstantNode<double>
    {
        public DoubleNode(IToken payload, TreeNodeFactory treeNodeFactory)
            : base(payload, treeNodeFactory)
        {
        }

        protected override double ParseValue(string text)
        {
            return Convert.ToDouble(text, CultureInfo.InvariantCulture);
        }
    }
}
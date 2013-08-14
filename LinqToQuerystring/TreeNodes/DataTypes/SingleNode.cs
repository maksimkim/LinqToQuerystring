namespace LinqToQuerystring.TreeNodes.DataTypes
{
    using System;
    using System.Globalization;
    using Antlr.Runtime;

    public class SingleNode : ConstantNode<float>
    {
        public SingleNode(IToken payload, TreeNodeFactory treeNodeFactory)
            : base(payload, treeNodeFactory)
        {
        }

        protected override float ParseValue(string text)
        {
            return Convert.ToSingle(text.Replace("f", string.Empty), CultureInfo.InvariantCulture);
        }
    }
}
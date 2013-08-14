namespace LinqToQuerystring.TreeNodes.DataTypes
{
    using System;
    using Antlr.Runtime;

    public class LongNode : ConstantNode<long>
    {
        public LongNode(IToken payload, TreeNodeFactory treeNodeFactory)
            : base(payload, treeNodeFactory)
        {
        }

        protected override long ParseValue(string text)
        {
            return Convert.ToInt64(text.Replace("L", string.Empty));
        }
    }
}
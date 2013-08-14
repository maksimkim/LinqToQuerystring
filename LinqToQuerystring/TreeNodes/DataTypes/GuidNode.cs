namespace LinqToQuerystring.TreeNodes.DataTypes
{
    using System;
    using Antlr.Runtime;

    public class GuidNode : ConstantNode<Guid>
    {
        public GuidNode(IToken payload, TreeNodeFactory treeNodeFactory)
            : base(payload, treeNodeFactory)
        {
        }

        protected override Guid ParseValue(string text)
        {
            var guidText = text.Replace("guid'", string.Empty).Replace("'", string.Empty);
            return new Guid(guidText);
        }
    }
}
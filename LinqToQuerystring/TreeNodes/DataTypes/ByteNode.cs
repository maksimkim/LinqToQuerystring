namespace LinqToQuerystring.TreeNodes.DataTypes
{
    using System;
    using Antlr.Runtime;

    public class ByteNode : ConstantNode<byte>
    {
        public ByteNode(IToken payload, TreeNodeFactory treeNodeFactory)
            : base(payload, treeNodeFactory)
        {
        }

        protected override byte ParseValue(string text)
        {
            return Convert.ToByte(this.Text.Replace("0x", string.Empty), 16);
        }
    }
}
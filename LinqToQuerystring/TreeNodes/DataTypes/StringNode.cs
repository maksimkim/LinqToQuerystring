namespace LinqToQuerystring.TreeNodes.DataTypes
{
    using System;
    using Antlr.Runtime;

    public class StringNode : ConstantNode<string>
    {
        public StringNode(IToken payload, TreeNodeFactory treeNodeFactory)
            : base(payload, treeNodeFactory)
        {
        }

        protected override string ParseValue(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                text = text.Trim('\'');
                text = text.Replace(@"\\", @"\");
                text = text.Replace(@"\b", "\b");
                text = text.Replace(@"\t", "\t");
                text = text.Replace(@"\n", "\n");
                text = text.Replace(@"\f", "\f");
                text = text.Replace(@"\r", "\r");
                text = text.Replace(@"\'", "'");
                text = text.Replace(@"''", "'");
            }

            return text;
        }
    }
}
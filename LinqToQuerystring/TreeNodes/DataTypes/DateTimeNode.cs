namespace LinqToQuerystring.TreeNodes.DataTypes
{
    using System;
    using System.Globalization;
    using Antlr.Runtime;

    public class DateTimeNode : ConstantNode<DateTime>
    {
        public DateTimeNode(IToken payload, TreeNodeFactory treeNodeFactory)
            : base(payload, treeNodeFactory)
        {
        }

        protected override DateTime ParseValue(string text)
        {
            var dateText = text
                .Replace("datetime'", string.Empty)
                .Replace("'", string.Empty)
                .Replace(".", ":");

            return DateTime.Parse(dateText, null, DateTimeStyles.RoundtripKind);
        }
    }
}
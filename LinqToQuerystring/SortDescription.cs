namespace LinqToQuerystring
{
    using System.Diagnostics.Contracts;
    using System.Linq.Expressions;

    public class SortDescription
    {
        public LambdaExpression Expression { get; private set; }

        public bool Desc { get; private set; }

        public SortDescription(LambdaExpression expression, bool desc = false) 
        {
            Contract.Assert(expression != null);

            Expression = expression;

            Desc = desc;
        }
    }
}
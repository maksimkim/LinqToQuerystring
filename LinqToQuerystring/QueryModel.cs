namespace LinqToQuerystring
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    public class QueryModel
    {
        public LambdaExpression Filter { get; set; }

        public IEnumerable<SortDescription> OrderBy { get; set; }

        public LambdaExpression Select { get; set; }

        public IEnumerable<LambdaExpression> Expand { get; set; }

        public int? Skip { get; set; }

        public int? Top { get; set; }

        public bool InlineCount { get; set; }

        public QueryModel()
        {
            OrderBy = Enumerable.Empty<SortDescription>();

            Expand = Enumerable.Empty<LambdaExpression>();
        }
    }
}
namespace LinqToQuerystring
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using Antlr.Runtime;
    using Antlr.Runtime.Tree;
    using TypeSystem;
    using ODataQuery;

    public static class Extensions
    {
        public static TResult LinqToQuerystring<T, TResult>(this IQueryable<T> query, string queryString = "", int maxPageSize = -1)
        {
            return (TResult)LinqToQuerystring(query, typeof(T), queryString, maxPageSize);
        }

        public static IQueryable<T> LinqToQuerystring<T>(this IQueryable<T> query, string queryString = "", int maxPageSize = -1)
        {
            return (IQueryable<T>)LinqToQuerystring(query, typeof(T), queryString, maxPageSize);
        }

        public static object LinqToQuerystring(this IQueryable query, Type inputType, string queryString = "", int maxPageSize = -1)
        {
            var queryResult = query;
            var constrainedQuery = query;

            if (query == null)
            {
                throw new ArgumentNullException("query", "Query cannot be null");
            }

            if (queryString == null)
            {
                throw new ArgumentNullException("querystring", "Query String cannot be null");
            }

            if (queryString.StartsWith("?"))
            {
                queryString = queryString.Substring(1);
            }

            var odataQueries = queryString.Split('&').Where(o => o.StartsWith("$")).ToList();
            if (maxPageSize > 0)
            {
                var top = odataQueries.FirstOrDefault(o => o.StartsWith("$top"));
                if (top != null)
                {
                    int pagesize;
                    if (!int.TryParse(top.Split('=')[1], out pagesize) || pagesize >= maxPageSize)
                    {
                        odataQueries.Remove(top);
                        odataQueries.Add("$top=" + maxPageSize);
                    }
                }
                else
                {
                    odataQueries.Add("$top=" + maxPageSize);
                }
            }

            var odataQuerystring = string.Join("&", odataQueries.ToArray());

            var input = new ANTLRReaderStream(new StringReader(odataQuerystring));
            var lexer = new ODataQueryLexer(input);
            var tokStream = new CommonTokenStream(lexer);

            var parser = new ODataQueryParser(tokStream);

            var result = parser.parse();

            return ApplyQuery(result.Tree as CommonTree, ref queryResult, ref constrainedQuery);
        }

        private static object ApplyQuery(CommonTree tree, ref IQueryable queryResult, ref IQueryable constrainedQuery)
        {
            if (tree == null)
                return constrainedQuery;

            var visitor = new QueryBuilder(new DefaultTypeInfoProvider(), new DefaultTypeBuilder());

            var query = visitor.Build(tree, constrainedQuery.ElementType);

            if (query.Filter != null)
            {
                queryResult = ApplyMethod(queryResult, "Where", query.Filter);

                constrainedQuery = ApplyMethod(constrainedQuery, "Where", query.Filter);
            }

            if (query.OrderBy.Any())
            {
                var first = true;

                foreach (var child in query.OrderBy)
                {
                    var method = child.Desc ? (first ? "OrderByDescending" : "ThenByDescending") : (first ? "OrderBy" : "ThenBy");

                    constrainedQuery = ApplyMethod(constrainedQuery, method, child.Expression, child.Expression.ReturnType);

                    first = false;
                }
            }

            if (query.Skip.HasValue)
                constrainedQuery = ApplyMethod(constrainedQuery, "Skip", Expression.Constant(query.Skip.Value, typeof(int)));

            if (query.Top.HasValue)
                constrainedQuery = ApplyMethod(constrainedQuery, "Take", Expression.Constant(query.Top.Value, typeof(int)));

            if (query.Select != null)
                constrainedQuery = ApplyMethod(constrainedQuery, "Select", query.Select, query.Select.ReturnType);
           
            
            return query.InlineCount ? PackageResults(queryResult, constrainedQuery) : constrainedQuery;
        }

        private static IQueryable ApplyMethod(IQueryable source, string method, Expression arg, Type typeArg = null)
        {
            var typeArgs = new Type[typeArg == null ? 1 : 2];
            
            typeArgs[0] = source.ElementType;
            
            if (typeArg != null)
                typeArgs[1] = typeArg;

            return source.Provider.CreateQuery(
                Expression.Call(typeof(Queryable), method, typeArgs, source.Expression, arg)
            );
        }

        private static object PackageResults(IQueryable query, IQueryable constrainedQuery)
        {
            return new Dictionary<string, object> { { "Count", query.Cast<object>().Count() }, { "Results", constrainedQuery } };
        }
    }
}
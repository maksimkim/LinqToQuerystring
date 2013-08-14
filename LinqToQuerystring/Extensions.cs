namespace LinqToQuerystring
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using Antlr.Runtime;
    using Antlr.Runtime.Tree;

    using LinqToQuerystring.TreeNodes;
    using LinqToQuerystring.TreeNodes.Base;
    using TreeNodes.DataTypes;

    public static class Extensions
    {
        public static TResult LinqToQuerystring<T, TResult>(this IQueryable<T> query, string queryString = "", bool forceDynamicProperties = false, int maxPageSize = -1)
        {
            return (TResult)LinqToQuerystring(query, typeof(T), queryString, forceDynamicProperties, maxPageSize);
        }

        public static IQueryable<T> LinqToQuerystring<T>(this IQueryable<T> query, string queryString = "", bool forceDynamicProperties = false, int maxPageSize = -1)
        {
            return (IQueryable<T>)LinqToQuerystring(query, typeof(T), queryString, forceDynamicProperties, maxPageSize);
        }

        public static object LinqToQuerystring(this IQueryable query, Type inputType, string queryString = "", bool forceDynamicProperties = false, int maxPageSize = -1)
        {
            IQueryable queryResult = query;
            IQueryable constrainedQuery = query;

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
            var lexer = new LinqToQuerystringLexer(input);
            var tokStream = new CommonTokenStream(lexer);

            var parser = new LinqToQuerystringParser(tokStream)
            {
                TreeAdaptor = new TreeNodeFactory(inputType, forceDynamicProperties)
            };

            var result = parser.prog();

            var singleNode = result.Tree as TreeNode;
            if (singleNode != null && !(singleNode is IdentifierNode))
            {
                if (!(singleNode is SelectNode) && !(singleNode is InlineCountNode))
                {
                    BuildQuery(singleNode, ref queryResult, ref constrainedQuery);
                    return constrainedQuery;
                }

                if (singleNode is SelectNode)
                {
                    return Apply(constrainedQuery, "Select", ((SelectNode)singleNode).BuildProjection(constrainedQuery.ElementType), typeof(Dictionary<string, object>));
                }

                return PackageResults(queryResult, constrainedQuery);
            }

            var tree = result.Tree as CommonTree;
            if (tree != null)
            {
                var children = tree.Children.Cast<TreeNode>().ToList();
                children.Sort();

                // These should always come first
                foreach (var node in children.Where(o => !(o is SelectNode) && !(o is InlineCountNode)))
                {
                    BuildQuery(node, ref queryResult, ref constrainedQuery);
                }

                var selectNode = children.FirstOrDefault(o => o is SelectNode);
                if (selectNode != null)
                {
                    constrainedQuery = Apply(constrainedQuery, "Select", ((SelectNode)selectNode).BuildProjection(constrainedQuery.ElementType), typeof(Dictionary<string, object>));
                }

                var inlineCountNode = children.FirstOrDefault(o => o is InlineCountNode);
                if (inlineCountNode != null)
                {
                    return PackageResults(queryResult, constrainedQuery);
                }
            }

            return constrainedQuery;
        }

        private static void BuildQuery(TreeNode node, ref IQueryable queryResult, ref IQueryable constrainedQuery)
        {
            var type = queryResult.Provider.GetType().Name;

            var mappings = 
                (!string.IsNullOrEmpty(type) && Configuration.CustomNodes.ContainsKey(type))
                ? Configuration.CustomNodes[type]
                : null;

            if (mappings != null)
            {
                node = mappings.MapNode(node, queryResult.Expression);
            }

            var elementType = constrainedQuery.ElementType;

            var filterNode = node as FilterNode;
            var orderByNode = node as OrderByNode;
            var skipNode = node as SkipNode;
            var topNode = node as TopNode;
            var expandNode = node as ExpandNode;

            if (filterNode != null)
            {
                var filter = filterNode.BuildFilter(elementType);
                
                queryResult = Apply(queryResult, "Where", filter);

                constrainedQuery = Apply(constrainedQuery, "Where", filter);
            }
            else if (orderByNode != null)
            {
                var first = true;

                foreach (var child in orderByNode.BuildSorts(elementType))
                {
                    var method = child.Desc ? (first ? "OrderByDescending" : "ThenByDescending") : (first ? "OrderBy" : "ThenBy");

                    constrainedQuery = Apply(constrainedQuery, method, child.Expression, child.Expression.ReturnType);
                    
                    first = false;
                }
            }
            else if (skipNode != null)
            {
                constrainedQuery = Apply(constrainedQuery, "Skip", Expression.Constant(skipNode.Value, typeof(int)));
            }
            else if (topNode != null)
            {
                constrainedQuery = Apply(constrainedQuery, "Take", Expression.Constant(topNode.Value, typeof(int)));
            }
            else if (expandNode != null)
            {
                var expands = expandNode.BuildExpands(elementType).ToList();
            }
        }

        private static IQueryable Apply(IQueryable source, string method, Expression arg, Type typeArg = null)
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
namespace LinqToQuerystring
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Antlr.Runtime;
    using Antlr.Runtime.Tree;
    using ODataQuery.Exceptions;
    using TypeSystem;
    using ODataQuery;

    public class QueryBuilder
    {
        private class VisitContext
        {
            public Expression Param { get; private set; }

            public VisitContext(Expression param)
            {
                Param = param;
            }
        }

        private readonly ITypeInfoProvider _typeInfoProvider;

        private readonly ITypeBuilder _typeBuilder;

        private readonly Func<CommonTree, Expression[], VisitContext, Expression>[] _visitors = new Func<CommonTree, Expression[], VisitContext, Expression>[65];

        private const int TypeShift = ODataQueryLexer.T_BOOL;

        private const int BoolOpShift = ODataQueryLexer.BOP_AND;

        private const int MethodShift = ODataQueryLexer.M_ALL;

        private readonly Func<string, IFormatProvider, object>[] _converters = new Func<string, IFormatProvider, object>[ODataQueryLexer.T_STRING - TypeShift + 1];

        private readonly Type[] _types = new Type[ODataQueryLexer.T_STRING - TypeShift + 1];

        private readonly Func<Expression, Expression, Expression>[] _binaries = new Func<Expression, Expression, Expression>[ODataQueryLexer.BOP_OR - BoolOpShift + 1];

        private readonly MethodInfo[] _methods = new MethodInfo[ODataQueryLexer.M_TRIM - MethodShift + 1];

        private static readonly IDictionary<string, LambdaExpression> ProjectionCache 
            = new Dictionary<string, LambdaExpression>(StringComparer.Create(CultureInfo.InvariantCulture, true));

        private static readonly ConcurrentDictionary<Type, IDictionary<string, PropertyInfo>> PropertyCache
            = new ConcurrentDictionary<Type, IDictionary<string, PropertyInfo>>();

        public QueryBuilder(ITypeInfoProvider typeInfoProvider, ITypeBuilder typeBuilder)
        {
            _typeInfoProvider = typeInfoProvider;
            _typeBuilder = typeBuilder;

            FillVisitors();
            FillTypes();
            FillConverters();
            FillBinaries();
            FillMethods();
        }

        private void FillVisitors()
        {
            _visitors[ODataQueryLexer.M_ALL] =
                _visitors[ODataQueryLexer.M_ANY] =
                    _visitors[ODataQueryLexer.M_COUNT] =
                        _visitors[ODataQueryLexer.M_MAX] =
                            _visitors[ODataQueryLexer.M_MIN] = VisitAggregate;

            _visitors[ODataQueryLexer.M_SUM] = VisitSum;
            _visitors[ODataQueryLexer.M_AVERAGE] = VisitAverage;

            _visitors[ODataQueryLexer.BOP_NOT] = VisitNot;

            _visitors[ODataQueryLexer.BOP_EQUALS] = VisitEquals;
            _visitors[ODataQueryLexer.BOP_NOTEQUALS] = VisitNotEquals;
            _visitors[ODataQueryLexer.BOP_OR] =
                _visitors[ODataQueryLexer.BOP_AND] =
                    _visitors[ODataQueryLexer.BOP_GREATERTHAN] =
                        _visitors[ODataQueryLexer.BOP_GREATERTHANOREQUAL] =
                            _visitors[ODataQueryLexer.BOP_LESSTHAN] =
                                _visitors[ODataQueryLexer.BOP_LESSTHANOREQUAL] = VisitBinary;
            
            _visitors[ODataQueryLexer.T_BOOL] =
                _visitors[ODataQueryLexer.T_BYTE] =
                    _visitors[ODataQueryLexer.T_DATETIME] =
                        _visitors[ODataQueryLexer.T_DOUBLE] =
                            _visitors[ODataQueryLexer.T_GUID] =
                                _visitors[ODataQueryLexer.T_INT] =
                                    _visitors[ODataQueryLexer.T_LONG] =
                                        _visitors[ODataQueryLexer.T_SINGLE] =
                                            _visitors[ODataQueryLexer.T_STRING] =
                                                _visitors[ODataQueryLexer.T_NULL] = VisitConstant;

            _visitors[ODataQueryLexer.IDENTIFIER] = 
                _visitors[ODataQueryLexer.DYNAMICIDENTIFIER] = VisitIdentifier;

            _visitors[ODataQueryLexer.ALIAS] = VisitAlias;
            _visitors[ODataQueryLexer.LAMBDA] = VisitLambda;

            _visitors[ODataQueryLexer.M_STARTSWITH] =
                _visitors[ODataQueryLexer.M_ENDSWITH] =
                    _visitors[ODataQueryLexer.M_INDEXOF] =
                        _visitors[ODataQueryLexer.M_SUBSTRINGOF] =
                            _visitors[ODataQueryLexer.M_TOLOWER] =
                                _visitors[ODataQueryLexer.M_TOUPPER] =
                                    _visitors[ODataQueryLexer.M_LENGTH] =
                                        _visitors[ODataQueryLexer.M_TRIM] = VisitStringFunction;

        }

        private void FillMethods()
        {
            _methods[ODataQueryLexer.M_STARTSWITH - MethodShift] = typeof(string).GetMethod("StartsWith", new[] { typeof(string) });
            _methods[ODataQueryLexer.M_ENDSWITH - MethodShift] = typeof(string).GetMethod("EndsWith", new[] { typeof(string) });
            _methods[ODataQueryLexer.M_SUBSTRINGOF - MethodShift] = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            _methods[ODataQueryLexer.M_INDEXOF - MethodShift] = typeof(string).GetMethod("IndexOf", new[] { typeof(string) });

            _methods[ODataQueryLexer.M_TOLOWER - MethodShift] = typeof(string).GetMethod("ToLowerInvariant", Type.EmptyTypes);
            _methods[ODataQueryLexer.M_TOUPPER - MethodShift] = typeof(string).GetMethod("ToUpperInvariant", Type.EmptyTypes);
            _methods[ODataQueryLexer.M_LENGTH - MethodShift] = typeof(string).GetMethod("Length", Type.EmptyTypes);
            _methods[ODataQueryLexer.M_TRIM - MethodShift] = typeof(string).GetMethod("Trim", Type.EmptyTypes);

            _methods[ODataQueryLexer.M_MAX - MethodShift] = (((Expression<Func<IEnumerable<object>, object>>)(_ => _.Max())).Body as MethodCallExpression).Method.GetGenericMethodDefinition();
            _methods[ODataQueryLexer.M_MIN - MethodShift] = (((Expression<Func<IEnumerable<object>, object>>)(_ => _.Min())).Body as MethodCallExpression).Method.GetGenericMethodDefinition();
            _methods[ODataQueryLexer.M_COUNT - MethodShift] = (((Expression<Func<IEnumerable<object>, int>>)(_ => _.Count())).Body as MethodCallExpression).Method.GetGenericMethodDefinition();

            _methods[ODataQueryLexer.M_ALL - MethodShift] = (((Expression<Func<IEnumerable<object>, bool>>)(_ => _.All(i => true))).Body as MethodCallExpression).Method.GetGenericMethodDefinition();
            _methods[ODataQueryLexer.M_ANY - MethodShift] = (((Expression<Func<IEnumerable<object>, bool>>)(_ => _.Any(i => true))).Body as MethodCallExpression).Method.GetGenericMethodDefinition();
        }

        private void FillTypes()
        {
            _types[ODataQueryLexer.T_BOOL - TypeShift] = typeof(bool);
            _types[ODataQueryLexer.T_BYTE - TypeShift] = typeof(byte);
            _types[ODataQueryLexer.T_DATETIME - TypeShift] = typeof(DateTime);
            _types[ODataQueryLexer.T_DOUBLE - TypeShift] = typeof(double);
            _types[ODataQueryLexer.T_GUID - TypeShift] = typeof(Guid);
            _types[ODataQueryLexer.T_INT - TypeShift] = typeof(int);
            _types[ODataQueryLexer.T_LONG - TypeShift] = typeof(long);
            _types[ODataQueryLexer.T_SINGLE - TypeShift] = typeof(float);
            _types[ODataQueryLexer.T_STRING - TypeShift] = typeof(string);
        }

        private void FillConverters()
        {
            _converters[ODataQueryLexer.T_NULL - TypeShift] = (v, p) => null;
            _converters[ODataQueryLexer.T_BOOL - TypeShift] = (v, p) => Convert.ToBoolean(v, p);
            _converters[ODataQueryLexer.T_DOUBLE - TypeShift] = (v, p) => Convert.ToDouble(v, p);
            _converters[ODataQueryLexer.T_INT - TypeShift] = (v, p) => Convert.ToInt32(v, p);

            _converters[ODataQueryLexer.T_BYTE - TypeShift] = (v, p) => Convert.ToByte(v.Replace("0x", string.Empty), 16);
            _converters[ODataQueryLexer.T_GUID - TypeShift] = (v, p) => new Guid(v.Replace("guid'", string.Empty).Replace("'", string.Empty));
            _converters[ODataQueryLexer.T_LONG - TypeShift] = (v, p) => Convert.ToInt64(v.Replace("L", string.Empty), p);
            _converters[ODataQueryLexer.T_SINGLE - TypeShift] = (v, p) => Convert.ToSingle(v.Replace("f", string.Empty), p);

            _converters[ODataQueryLexer.T_DATETIME - TypeShift] = (v, p) =>
            {
                v = v.Replace("datetime'", string.Empty).Replace("'", string.Empty).Replace(".", ":");
                return DateTime.Parse(v, null, DateTimeStyles.RoundtripKind);
            };

            _converters[ODataQueryLexer.T_STRING - TypeShift] = (v, p) =>
            {
                if (!string.IsNullOrWhiteSpace(v))
                {
                    v = v.Trim('\'')
                        .Replace(@"\\", @"\")
                        .Replace(@"\b", "\b")
                        .Replace(@"\t", "\t")
                        .Replace(@"\n", "\n")
                        .Replace(@"\f", "\f")
                        .Replace(@"\r", "\r")
                        .Replace(@"\'", "'")
                        .Replace(@"''", "'");
                }

                return v;
            };
        }

        private void FillBinaries()
        {
            _binaries[ODataQueryLexer.BOP_EQUALS - BoolOpShift] = Expression.Equal;
            _binaries[ODataQueryLexer.BOP_NOTEQUALS - BoolOpShift] = Expression.NotEqual;
            _binaries[ODataQueryLexer.BOP_GREATERTHAN - BoolOpShift] = Expression.GreaterThan;
            _binaries[ODataQueryLexer.BOP_GREATERTHANOREQUAL - BoolOpShift] = Expression.GreaterThanOrEqual;
            _binaries[ODataQueryLexer.BOP_LESSTHAN - BoolOpShift] = Expression.LessThan;
            _binaries[ODataQueryLexer.BOP_LESSTHANOREQUAL - BoolOpShift] = Expression.LessThanOrEqual;
            _binaries[ODataQueryLexer.BOP_OR - BoolOpShift] = Expression.Or;
            _binaries[ODataQueryLexer.BOP_AND - BoolOpShift] = Expression.And;
        }

        public QueryModel Build(string queryString, Type itemType)
        {
            CommonTree tree;

            using (var stringReader = new StringReader(queryString))
            {
                var input = new ANTLRReaderStream(stringReader);
                var lexer = new ODataQueryLexer(input);
                var tokStream = new CommonTokenStream(lexer);
                var parser = new ODataQueryParser(tokStream);

                tree = parser.parse().Tree as CommonTree;
            }

            var result = new QueryModel();

            if (tree != null)
                foreach (var model in IterateClauses(tree))
                {
                    switch (model.Type)
                    {
                        case ODataQueryLexer.FILTER:
                            result.Filter = VisitFilter(model, itemType);
                            break;
                        case ODataQueryLexer.ORDERBY:
                            result.OrderBy = VisitOrderBy(model, itemType).ToArray();
                            break;
                        case ODataQueryLexer.EXPAND:
                            result.Expand = VisitExpand(model, itemType).ToArray();
                            break;
                        case ODataQueryLexer.SELECT:
                            result.Select = VisitSelect(model, itemType);
                            break;
                        case ODataQueryLexer.SKIP:
                            result.Skip = VisitSkip(model);
                            break;
                        case ODataQueryLexer.TOP:
                            result.Top = VisitTop(model);
                            break;
                        case ODataQueryLexer.INLINECOUNT:
                            result.InlineCount = true;
                            break;
                        default:
                            model.Invalid();
                            break;
                    }
                }

            return result;
        }

        public LambdaExpression VisitFilter(CommonTree node, Type itemType)
        {
            var param = Expression.Parameter(itemType, "_");
            
            var body = Visit(node.Child(), new VisitContext(param));

            var filter = Expression.Lambda(body, param);

            Contract.Assert(filter.ReturnType == typeof(bool));

            return filter;
        }

        public IEnumerable<SortDescription> VisitOrderBy(CommonTree node, Type itemType)
        {
            Contract.Assert(node.ChildCount > 0);

            foreach (var child in node.ChildNodes())
            {
                if (child.Type != ODataQueryLexer.ASC && child.Type != ODataQueryLexer.DESC)
                    child.Invalid();
                
                var param = Expression.Parameter(itemType, "_");
                
                var body = Visit(child.Child(), new VisitContext(param));

                var accessor = Expression.Lambda(body, param);

                yield return new SortDescription(accessor, child.Type == ODataQueryLexer.DESC);
            }
        }

        public IEnumerable<LambdaExpression> VisitExpand(CommonTree node, Type itemType)
        {
            Contract.Assert(node.ChildCount > 0);

            foreach (var child in node.ChildNodes())
            {
                if (child.Type != ODataQueryLexer.IDENTIFIER)
                    child.Invalid();

                var param = Expression.Parameter(itemType, "_");

                var body = Visit(child.Child(), new VisitContext(param));

                var accessor = Expression.Lambda(body, param);

                yield return accessor;
            }
        }

        public LambdaExpression VisitSelect(CommonTree node, Type itemType)
        {
            Contract.Assert(node.ChildCount > 0);

            var propertyNames = node.ChildNodes().Select(c => c.Text).OrderBy(_ => _).ToArray();

            var cacheKey = string.Format("{0}:{1}", itemType.Name, string.Join(",", propertyNames));

            if (!ProjectionCache.ContainsKey(cacheKey))
                lock (((ICollection)ProjectionCache).SyncRoot)
                    if (!ProjectionCache.ContainsKey(cacheKey))
                    {
                        var props = propertyNames.Select(n => FindProperty(itemType, n)).ToArray();

                        var targetType = _typeBuilder.Build(cacheKey, itemType, props);

                        var param = Expression.Parameter(itemType, "_");

                        var bindings = targetType
                            .GetProperties()
                            .Join(props, p => p.Name, p => p.Name, (t, s) => new { target = t, source = s })
                            .Select(pair => Expression.Bind(pair.target, Expression.Property(param, pair.source)));

                        var ctor = targetType.GetConstructor(Type.EmptyTypes);

                        Contract.Assert(ctor != null);

                        var projection = Expression.Lambda(
                            Expression.MemberInit(Expression.New(ctor), bindings), 
                            param
                        );

                        ProjectionCache.Add(cacheKey, projection);
                    }
            
            return ProjectionCache[cacheKey];
        }

        private int VisitSkip(CommonTree node)
        {
            Contract.Assert(node.ChildCount == 1);

            var child = node.Children[0];

            Contract.Assert(child != null);
            Contract.Assert(child.Type == ODataQueryLexer.T_INT);

            return Convert.ToInt32(child.Text, CultureInfo.InvariantCulture);
        }

        private int VisitTop(CommonTree node)
        {
            Contract.Assert(node.ChildCount == 1);

            var child = node.Children[0];

            Contract.Assert(child != null);
            Contract.Assert(child.Type == ODataQueryLexer.T_INT);

            return Convert.ToInt32(child.Text, CultureInfo.InvariantCulture);
        }

        private Expression Visit(CommonTree node, VisitContext context)
        {
            Expression[] products = null;

            if (node.ChildCount > 0)
            {
                if (node.Type == ODataQueryLexer.M_ALL || node.Type == ODataQueryLexer.M_ANY)
                {
                    Contract.Assert(node.ChildCount == 2);

                    var collection = Visit(node.Children[0] as CommonTree, context);

                    //context change for inner lambda
                    Contract.Assert(typeof(IEnumerable).IsAssignableFrom(collection.Type) && collection.Type.IsGenericType);

                    var itemType = collection.Type.GetGenericArguments()[0];

                    var lambdaCtx = new VisitContext(Expression.Parameter(itemType));

                    var lambda = Visit(node.Children[1] as CommonTree, lambdaCtx);

                    products = new[] { collection, lambda };
                }
                else
                    products = node.ChildNodes().Select(c => Visit(c, context)).ToArray();
            }

            return VisitNode(node, products, context);
        }

        private Expression VisitNode(CommonTree node, Expression[] childProducts, VisitContext context)
        {
            var visitor = _visitors[node.Type];

            if (visitor == null)
                node.Invalid();

            return visitor(node, childProducts, context);
        }

        private Expression VisitConstant(CommonTree node, Expression[] childProducts, VisitContext context)
        {
            var type = _types[node.Type -TypeShift];

            var converter = _converters[node.Type - TypeShift];

            Contract.Assert(converter != null);

            var value = converter(node.Text, CultureInfo.InvariantCulture);

            return
                type != null
                ? Expression.Constant(value, type)
                : Expression.Constant(value);
        }

        private Expression VisitStringFunction(CommonTree node, Expression[] childProducts, VisitContext context)
        {
            Contract.Assert(childProducts != null && childProducts.Length > 0);

            var method = _methods[node.Type - MethodShift];

            if (method == null)
                throw new QueryParserException(string.Format("Unsupported function {0}", node.Text));

            //special case function signature
            if (node.Type == ODataQueryLexer.M_SUBSTRINGOF)
                childProducts = childProducts.Reverse().ToArray();
            
            var instance = childProducts[0];

            if (childProducts.Length > 1)
            {
                var args = childProducts
                    .Skip(1)
                    .Select(arg => arg.Type.IsAssignableFrom(typeof(string)) ? arg : Expression.Convert(arg, typeof(string)))
                    .ToArray();

                return Expression.Call(instance, method, args);
            }

            return Expression.Call(instance, method);
        }

        private Expression VisitLambda(CommonTree node, Expression[] childProducts, VisitContext context)
        {
            Contract.Assert(childProducts != null && childProducts.Length > 1);
            var body = childProducts[0];

            return Expression.Lambda(body, childProducts.Skip(1).Cast<ParameterExpression>());
        }

        private Expression VisitNot(CommonTree node, Expression[] childProducts, VisitContext context)
        {
            Contract.Assert(childProducts != null && childProducts.Length == 1);

            var product = childProducts[0];

            if (!typeof(bool).IsAssignableFrom(product.Type))
                product = Expression.Convert(product, typeof(bool));

            return Expression.Not(product);
        }

        private Expression VisitIdentifier(CommonTree node, Expression[] childProducts, VisitContext context)
        {
            Contract.Assert(childProducts == null || childProducts.Length == 1);

            var subject = childProducts == null ? context.Param : childProducts[0];

            if (node.Type == ODataQueryLexer.DYNAMICIDENTIFIER)
            {
                var key = node.Text.Trim(new[] { '[', ']' });

                return Expression.Call(subject, "get_Item", null, Expression.Constant(key));
            }
            
            var property = FindProperty(subject.Type, node.Text);

            return Expression.Property(subject, property);
        }

        private Expression VisitAlias(CommonTree node, Expression[] childProducts, VisitContext context)
        {
            return context.Param;
        }

        private Expression VisitEquals(CommonTree node, Expression[] childProducts, VisitContext context)
        {
            Contract.Assert(childProducts != null && childProducts.Length == 2);

            var left = childProducts[0];

            var right = childProducts[1];

            // Nasty workaround to avoid comparison of Aggregate functions to true or false which breaks Entity framework
            if (left.Type == typeof(bool) && right.Type == typeof(bool) && right is ConstantExpression)
            {
                if ((bool)(right as ConstantExpression).Value)
                {
                    return left;
                }

                return Expression.Not(left);
            }

            if (right.Type == typeof(bool) && left.Type == typeof(bool) && left is ConstantExpression)
            {
                if ((bool)(left as ConstantExpression).Value)
                {
                    return right;
                }

                return Expression.Not(right);
            }

            return VisitBinary(node, childProducts, context);
        }

        private Expression VisitNotEquals(CommonTree node, Expression[] childProducts, VisitContext context)
        {
            Contract.Assert(childProducts != null && childProducts.Length == 2);

            var left = childProducts[0];
            var right = childProducts[1];

            NormalizeTypes(ref left, ref right);

            return ApplyWithNullAsValidAlternative(Expression.NotEqual, left, right);
        }

        private Expression VisitBinary(CommonTree node, Expression[] childProducts, VisitContext context)
        {
            Contract.Assert(childProducts != null && childProducts.Length == 2);

            var op = _binaries[node.Type - BoolOpShift];

            Contract.Assert(op != null);

            var left = childProducts[0];
            var right = childProducts[1];

            NormalizeTypes(ref left, ref right);

            return ApplyEnsuringNullablesHaveValues(op, left, right);
        }

        private Expression VisitAggregate(CommonTree node, Expression[] childProducts, VisitContext context)
        {
            Contract.Assert(childProducts != null && childProducts.Length > 0);

            var instance = childProducts[0];

            Contract.Assert(typeof(IEnumerable).IsAssignableFrom(instance.Type) && instance.Type.IsGenericType);

            var itemType = instance.Type.GetGenericArguments()[0];

            var method = _methods[node.Type - MethodShift];

            Contract.Assert(method != null);

            method = method.MakeGenericMethod(itemType);

            return Expression.Call(method, childProducts);
        }

        private Expression VisitAverage(CommonTree node, Expression[] childProducts, VisitContext context)
        {
            return VisitAggregateWithName("Average", childProducts);
        }

        private Expression VisitSum(CommonTree node, Expression[] childProducts, VisitContext context)
        {
            return VisitAggregateWithName("Sum", childProducts);
        }

        private Expression VisitAggregateWithName(string method, Expression[] args)
        {
            Contract.Assert(args != null && args.Length == 1);

            var arg = args[0];

            Contract.Assert(typeof(IEnumerable).IsAssignableFrom(arg.Type));

            return Expression.Call(typeof(Enumerable), method, null, arg);
        }

        private IEnumerable<CommonTree> IterateClauses(CommonTree tree)
        {
            if (!tree.IsNil)
                yield return tree;
            else
                foreach (var child in tree.ChildNodes())
                    yield return child;
        }

        private static Expression ApplyEnsuringNullablesHaveValues(Func<Expression, Expression, Expression> binaryOp, Expression left, Expression right)
        {
            var leftExpressionIsNullable = (Nullable.GetUnderlyingType(left.Type) != null);
            var rightExpressionIsNullable = (Nullable.GetUnderlyingType(right.Type) != null);

            if (leftExpressionIsNullable && !rightExpressionIsNullable)
            {
                return Expression.AndAlso(
                    Expression.NotEqual(left, Expression.Constant(null)),
                    binaryOp(Expression.Property(left, "Value"), right));
            }

            if (rightExpressionIsNullable && !leftExpressionIsNullable)
            {
                return Expression.AndAlso(
                    Expression.NotEqual(right, Expression.Constant(null)),
                    binaryOp(left, Expression.Property(right, "Value")));
            }

            return binaryOp(left, right);
        }

        private static Expression ApplyWithNullAsValidAlternative(Func<Expression, Expression, Expression> binaryOp, Expression left, Expression right)
        {
            var leftExpressionIsNullable = (Nullable.GetUnderlyingType(left.Type) != null);
            var rightExpressionIsNullable = (Nullable.GetUnderlyingType(right.Type) != null);

            if (leftExpressionIsNullable && !rightExpressionIsNullable)
            {
                return Expression.OrElse(
                    Expression.Equal(left, Expression.Constant(null)),
                    binaryOp(Expression.Property(left, "Value"), right));
            }

            if (rightExpressionIsNullable && !leftExpressionIsNullable)
            {
                return Expression.OrElse(
                    Expression.Equal(right, Expression.Constant(null)),
                    binaryOp(left, Expression.Property(right, "Value")));
            }

            return binaryOp(left, right);
        }

        private static void NormalizeTypes(ref Expression left, ref Expression right)
        {
            if (left.Type == right.Type)
                return;

            var rightIsContant = right is ConstantExpression;
            var leftIsConstant = left is ConstantExpression;

            if (rightIsContant && leftIsConstant)
                return;

            if (rightIsContant)
                // If we are comparing to an object try to cast it to the same type as the constant
                if (left.Type == typeof(object))
                    left = CastIfNeeded(left, right.Type);
                else
                    right = CastIfNeeded(right, left.Type);

            if (leftIsConstant)
                // If we are comparing to an object try to cast it to the same type as the constant
                if (right.Type == typeof(object))
                    right = CastIfNeeded(right, left.Type);
                else
                    left = CastIfNeeded(left, right.Type);
        }

        private static Expression CastIfNeeded(Expression expression, Type type)
        {
            //possibly enum convertion code should be here
            return !type.IsAssignableFrom(expression.Type) ? Expression.Convert(expression, type) : expression;
        }

        private PropertyInfo FindProperty(Type type, string name)
        {
            Contract.Assert(type != null);
            Contract.Assert(!string.IsNullOrWhiteSpace(name));

            var cached = PropertyCache.GetOrAdd(
                type,
                key => new Dictionary<string, PropertyInfo>(
                    _typeInfoProvider.GetProperties(key).ToDictionary(_typeInfoProvider.GetPropertyName, p => p),
                    StringComparer.Create(CultureInfo.InvariantCulture, true)
                    )
                );

            PropertyInfo result;

            if (!cached.TryGetValue(name, out result))
                throw new QueryParserException(string.Format("Property {0} doesn't exist or not supported for querying", name));

            return result;
        }
    }
}
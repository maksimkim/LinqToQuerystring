namespace LinqToQuerystring
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Antlr.Runtime.Tree;
    
    public class QueryBuilder
    {
        private readonly bool _forceDynamicProperties;

        private readonly Func<CommonTree, Expression[], Expression, Expression>[] _visitors = new Func<CommonTree, Expression[], Expression, Expression>[65];

        private readonly Func<string, IFormatProvider, object>[] _converters = new Func<string, IFormatProvider, object>[65];

        private readonly Type[] _types = new Type[65];

        private readonly Func<Expression, Expression, Expression>[] _binaries = new Func<Expression, Expression, Expression>[65];

        private readonly MethodInfo[] _methods = new MethodInfo[65];

        public QueryBuilder(bool forceDynamicProperties)
        {
            _forceDynamicProperties = forceDynamicProperties;

            FillVisitors();
            FillTypes();
            FillConverters();
            FillBinaries();
            FillMethods();
        }

        private void FillVisitors()
        {
            _visitors[LinqToQuerystringLexer.ALL] = 
                _visitors[LinqToQuerystringLexer.ANY] = 
                    _visitors[LinqToQuerystringLexer.COUNT] =
                        _visitors[LinqToQuerystringLexer.MAX] =
                            _visitors[LinqToQuerystringLexer.MIN] = VisitAggregate;

            _visitors[LinqToQuerystringLexer.SUM] = VisitSum;
            _visitors[LinqToQuerystringLexer.AVERAGE] = VisitAverage;

            _visitors[LinqToQuerystringLexer.NOT] = VisitNot;

            _visitors[LinqToQuerystringLexer.EQUALS] = VisitEquals;
            _visitors[LinqToQuerystringLexer.NOTEQUALS] = VisitNotEquals;
            _visitors[LinqToQuerystringLexer.OR] = 
                _visitors[LinqToQuerystringLexer.AND] = 
                    _visitors[LinqToQuerystringLexer.GREATERTHAN] = 
                        _visitors[LinqToQuerystringLexer.GREATERTHANOREQUAL] = 
                            _visitors[LinqToQuerystringLexer.LESSTHAN] = 
                                _visitors[LinqToQuerystringLexer.LESSTHANOREQUAL] = VisitBinary;
            
            _visitors[LinqToQuerystringLexer.BOOL] =
                _visitors[LinqToQuerystringLexer.BYTE] =
                    _visitors[LinqToQuerystringLexer.DATETIME] =
                        _visitors[LinqToQuerystringLexer.DOUBLE] =
                            _visitors[LinqToQuerystringLexer.GUID] =
                                _visitors[LinqToQuerystringLexer.INT] =
                                    _visitors[LinqToQuerystringLexer.LONG] =
                                        _visitors[LinqToQuerystringLexer.SINGLE] =
                                            _visitors[LinqToQuerystringLexer.STRING] =
                                                _visitors[LinqToQuerystringLexer.NULL] = VisitConstant;

            _visitors[LinqToQuerystringLexer.IDENTIFIER] = VisitIdentifier;
            _visitors[LinqToQuerystringLexer.DYNAMICIDENTIFIER] = VisitDynamicIdentifier;
            _visitors[LinqToQuerystringLexer.ALIAS] = VisitAlias;
            _visitors[LinqToQuerystringLexer.LAMBDA] = VisitLambda;
            
            _visitors[LinqToQuerystringLexer.STARTSWITH] =
                _visitors[LinqToQuerystringLexer.ENDSWITH] = 
                    _visitors[LinqToQuerystringLexer.INDEXOF] = 
                        _visitors[LinqToQuerystringLexer.SUBSTRINGOF] =
                            _visitors[LinqToQuerystringLexer.TOLOWER] =
                                _visitors[LinqToQuerystringLexer.TOUPPER] =
                                    _visitors[LinqToQuerystringLexer.LENGTH] =
                                        _visitors[LinqToQuerystringLexer.TRIM] = VisitStringFunction;

        }

        private void FillMethods()
        {
            _methods[LinqToQuerystringLexer.STARTSWITH] = typeof(string).GetMethod("StartsWith", new[] { typeof(string) });
            _methods[LinqToQuerystringLexer.ENDSWITH] = typeof(string).GetMethod("EndsWith", new[] { typeof(string) });
            _methods[LinqToQuerystringLexer.SUBSTRINGOF] = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            _methods[LinqToQuerystringLexer.INDEXOF] = typeof(string).GetMethod("IndexOf", new[] { typeof(string) });

            _methods[LinqToQuerystringLexer.TOLOWER] = typeof(string).GetMethod("ToLowerInvariant", Type.EmptyTypes);
            _methods[LinqToQuerystringLexer.TOUPPER] = typeof(string).GetMethod("ToUpperInvariant", Type.EmptyTypes);
            _methods[LinqToQuerystringLexer.LENGTH] = typeof(string).GetMethod("Length", Type.EmptyTypes);
            _methods[LinqToQuerystringLexer.TRIM] = typeof(string).GetMethod("Trim", Type.EmptyTypes);

            _methods[LinqToQuerystringLexer.MAX] = (((Expression<Func<IEnumerable<object>, object>>)(_ => _.Max())).Body as MethodCallExpression).Method.GetGenericMethodDefinition();
            _methods[LinqToQuerystringLexer.MIN] = (((Expression<Func<IEnumerable<object>, object>>)(_ => _.Min())).Body as MethodCallExpression).Method.GetGenericMethodDefinition();
            _methods[LinqToQuerystringLexer.COUNT] = (((Expression<Func<IEnumerable<object>, int>>)(_ => _.Count())).Body as MethodCallExpression).Method.GetGenericMethodDefinition();

            _methods[LinqToQuerystringLexer.ALL] = (((Expression<Func<IEnumerable<object>, bool>>)(_ => _.All(i => true))).Body as MethodCallExpression).Method.GetGenericMethodDefinition();
            _methods[LinqToQuerystringLexer.ANY] = (((Expression<Func<IEnumerable<object>, bool>>)(_ => _.Any(i => true))).Body as MethodCallExpression).Method.GetGenericMethodDefinition();
        }

        private void FillTypes()
        {
            _types[LinqToQuerystringLexer.BOOL] = typeof(bool);
            _types[LinqToQuerystringLexer.BYTE] = typeof(byte);
            _types[LinqToQuerystringLexer.DATETIME] = typeof(DateTime);
            _types[LinqToQuerystringLexer.DOUBLE] = typeof(double);
            _types[LinqToQuerystringLexer.GUID] = typeof(Guid);
            _types[LinqToQuerystringLexer.INT] = typeof(int);
            _types[LinqToQuerystringLexer.LONG] = typeof(long);
            _types[LinqToQuerystringLexer.SINGLE] = typeof(float);
            _types[LinqToQuerystringLexer.STRING] = typeof(string);
        }

        private void FillConverters()
        {
            _converters[LinqToQuerystringLexer.NULL] = (v, p) => null;
            _converters[LinqToQuerystringLexer.BOOL] = (v, p) => Convert.ToBoolean(v, p);
            _converters[LinqToQuerystringLexer.DOUBLE] = (v, p) => Convert.ToDouble(v, p);
            _converters[LinqToQuerystringLexer.INT] = (v, p) => Convert.ToInt32(v, p);
            
            _converters[LinqToQuerystringLexer.BYTE] = (v, p) => Convert.ToByte(v.Replace("0x", string.Empty), 16);
            _converters[LinqToQuerystringLexer.GUID] = (v, p) => new Guid(v.Replace("guid'", string.Empty).Replace("'", string.Empty));
            _converters[LinqToQuerystringLexer.LONG] = (v, p) => Convert.ToInt64(v.Replace("L", string.Empty), p);
            _converters[LinqToQuerystringLexer.SINGLE] = (v, p) => Convert.ToSingle(v.Replace("f", string.Empty), p);
            
            _converters[LinqToQuerystringLexer.DATETIME] = (v, p) =>
            {
                v = v.Replace("datetime'", string.Empty).Replace("'", string.Empty).Replace(".", ":");
                return DateTime.Parse(v, null, DateTimeStyles.RoundtripKind);
            };
            
            _converters[LinqToQuerystringLexer.STRING] = (v, p) =>
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
            _binaries[LinqToQuerystringLexer.EQUALS] = Expression.Equal;
            _binaries[LinqToQuerystringLexer.NOTEQUALS] = Expression.NotEqual;
            _binaries[LinqToQuerystringLexer.GREATERTHAN] = Expression.GreaterThan;
            _binaries[LinqToQuerystringLexer.GREATERTHANOREQUAL] = Expression.GreaterThanOrEqual;
            _binaries[LinqToQuerystringLexer.LESSTHAN] = Expression.LessThan;
            _binaries[LinqToQuerystringLexer.LESSTHANOREQUAL] = Expression.LessThanOrEqual;
            _binaries[LinqToQuerystringLexer.OR] = Expression.Or;
            _binaries[LinqToQuerystringLexer.AND] = Expression.And;
        }

        public ODataQuery BuildQuery(CommonTree tree, Type itemType)
        {
            var result = new ODataQuery();

            foreach (var node in IterateClauses(tree))
            {
                switch (node.Type)
                {
                    case LinqToQuerystringLexer.FILTER:
                        result.Filter = VisitFilter(node, itemType);
                        break;
                    case LinqToQuerystringLexer.ORDERBY:
                        result.OrderBy = VisitOrderBy(node, itemType).ToArray();
                        break;
                    case LinqToQuerystringLexer.EXPAND:
                        result.Expand = VisitExpand(node, itemType).ToArray();
                        break;
                    case LinqToQuerystringLexer.SELECT:
                        result.Select = VisitSelect(node, itemType);
                        break;
                    case LinqToQuerystringLexer.SKIP:
                        result.Skip = VisitSkip(node);
                        break;
                    case LinqToQuerystringLexer.TOP:
                        result.Top = VisitTop(node);
                        break;
                    case LinqToQuerystringLexer.INLINECOUNT:
                        result.InlineCount = true;
                        break;
                    default:
                        node.Invalid();
                        break;
                }
            }

            return result;
        }

        private LambdaExpression VisitFilter(CommonTree node, Type itemType)
        {
            var param = Expression.Parameter(itemType, "_");
            
            var body = Visit(node.Child(), param);

            var filter = Expression.Lambda(body, param);

            Contract.Assert(filter.ReturnType == typeof(bool));

            return filter;
        }

        private IEnumerable<SortDescription> VisitOrderBy(CommonTree node, Type itemType)
        {
            Contract.Assert(node.ChildCount > 0);

            foreach (var child in node.TreeChildren())
            {
                if (child.Type != LinqToQuerystringLexer.ASC && child.Type != LinqToQuerystringLexer.DESC)
                    child.Invalid();
                
                var param = Expression.Parameter(itemType, "_");
                
                var body = Visit(child.Child(), param);

                var accessor = Expression.Lambda(body, param);

                yield return new SortDescription(accessor, child.Type == LinqToQuerystringLexer.DESC);
            }
        }

        private IEnumerable<LambdaExpression> VisitExpand(CommonTree node, Type itemType)
        {
            Contract.Assert(node.ChildCount > 0);

            foreach (var child in node.TreeChildren())
            {
                if (child.Type != LinqToQuerystringLexer.IDENTIFIER)
                    child.Invalid();

                var param = Expression.Parameter(itemType, "_");

                var body = Visit(child.Child(), param);

                var accessor = Expression.Lambda(body, param);

                yield return accessor;
            }
        }

        private LambdaExpression VisitSelect(CommonTree node, Type itemType)
        {
            Contract.Assert(node.ChildCount > 0);

            var parameter = Expression.Parameter(itemType, "o");

            var addMethod = typeof(Dictionary<string, object>).GetMethod("Add");

            var elements = node.TreeChildren().Select(
                child => Expression.ElementInit(
                    addMethod, Expression.Constant(child.Text),
                    Expression.Convert(Visit(child, parameter), typeof(object))
                )
            );

            var newDictionary = Expression.New(typeof(Dictionary<string, object>));

            var init = Expression.ListInit(newDictionary, elements);

            var lambda = Expression.Lambda(init, new[] { parameter });

            return lambda;
        }

        private int VisitSkip(CommonTree node)
        {
            Contract.Assert(node.ChildCount == 1);

            var child = node.Children[0];

            Contract.Assert(child != null);
            Contract.Assert(child.Type == LinqToQuerystringLexer.INT);

            return Convert.ToInt32(child.Text, CultureInfo.InvariantCulture);
        }

        private int VisitTop(CommonTree node)
        {
            Contract.Assert(node.ChildCount == 1);

            var child = node.Children[0];

            Contract.Assert(child != null);
            Contract.Assert(child.Type == LinqToQuerystringLexer.INT);

            return Convert.ToInt32(child.Text, CultureInfo.InvariantCulture);
        }

        public Expression Visit(CommonTree node, Expression param)
        {
            Expression[] products = null;

            if (node.ChildCount > 0)
            {
                if (node.Type == LinqToQuerystringLexer.ALL || node.Type == LinqToQuerystringLexer.ANY)
                {
                    Contract.Assert(node.ChildCount == 2);

                    var collection = Visit(node.Children[0] as CommonTree, param);

                    Contract.Assert(typeof(IEnumerable).IsAssignableFrom(collection.Type) && collection.Type.IsGenericType);

                    var itemType = collection.Type.GetGenericArguments()[0];

                    var lambda = Visit(node.Children[1] as CommonTree, Expression.Parameter(itemType));

                    products = new[] { collection, lambda };
                }
                else
                    products = node.TreeChildren().Select(c => Visit(c, param)).ToArray();
            }

            return VisitNode(node, products, param);
        }

        private Expression VisitNode(CommonTree node, Expression[] childProducts, Expression param)
        {
            var visitor = _visitors[node.Type];

            if (visitor == null)
                node.Invalid();

            return visitor(node, childProducts, param);
        }

        private Expression VisitConstant(CommonTree node, Expression[] childProducts, Expression param)
        {
            var type = _types[node.Type];

            var converter = _converters[node.Type];

            Contract.Assert(converter != null);

            var value = converter(node.Text, CultureInfo.InvariantCulture);

            return
                type != null
                ? Expression.Constant(value, type)
                : Expression.Constant(value);
        }

        private Expression VisitStringFunction(CommonTree node, Expression[] childProducts, Expression param)
        {
            Contract.Assert(childProducts != null && childProducts.Length > 0);

            var method = _methods[node.Type];

            Contract.Assert(method != null);
            
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

        private Expression VisitLambda(CommonTree node, Expression[] childProducts, Expression param)
        {
            Contract.Assert(childProducts != null && childProducts.Length > 1);
            var body = childProducts[0];

            return Expression.Lambda(body, childProducts.Skip(1).Cast<ParameterExpression>());
        }

        private Expression VisitNot(CommonTree node, Expression[] childProducts, Expression param)
        {
            Contract.Assert(childProducts != null && childProducts.Length == 1);

            var product = childProducts[0];

            if (!typeof(bool).IsAssignableFrom(product.Type))
                product = Expression.Convert(product, typeof(bool));

            return Expression.Not(product);
        }

        private Expression VisitIdentifier(CommonTree node, Expression[] childProducts, Expression param)
        {
            if (_forceDynamicProperties)
                return VisitDynamicIdentifier(node, childProducts, param);
            
            Contract.Assert(childProducts == null || childProducts.Length == 1);

            var subject = childProducts == null ? param : childProducts[0];

            return Expression.Property(subject, node.Text);
        }

        private Expression VisitDynamicIdentifier(CommonTree node, Expression[] childProducts, Expression param)
        {
            Contract.Assert(childProducts == null || childProducts.Length == 1);

            var subject = childProducts == null ? param : childProducts[0];

            var key = node.Text.Trim(new[] { '[', ']' });

            var property = Expression.Call(subject, "get_Item", null, Expression.Constant(key));

            return property;
        }

        private Expression VisitAlias(CommonTree node, Expression[] childProducts, Expression param)
        {
            return param;
        }

        private Expression VisitEquals(CommonTree node, Expression[] childProducts, Expression param)
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

            return VisitBinary(node, childProducts, param);
        }

        private Expression VisitNotEquals(CommonTree node, Expression[] childProducts, Expression param)
        {
            Contract.Assert(childProducts != null && childProducts.Length == 2);

            var left = childProducts[0];
            var right = childProducts[1];

            NormalizeTypes(ref left, ref right);

            return ApplyWithNullAsValidAlternative(Expression.NotEqual, left, right);
        }

        private Expression VisitBinary(CommonTree node, Expression[] childProducts, Expression param)
        {
            Contract.Assert(childProducts != null && childProducts.Length == 2);

            var op = _binaries[node.Type];

            Contract.Assert(op != null);

            var left = childProducts[0];
            var right = childProducts[1];

            NormalizeTypes(ref left, ref right);

            return ApplyEnsuringNullablesHaveValues(op, left, right);
        }

        private Expression VisitAggregate(CommonTree node, Expression[] childProducts, Expression param)
        {
            Contract.Assert(childProducts != null && childProducts.Length > 0);

            var instance = childProducts[0];

            Contract.Assert(typeof(IEnumerable).IsAssignableFrom(instance.Type) && instance.Type.IsGenericType);

            var itemType = instance.Type.GetGenericArguments()[0];

            var method = _methods[node.Type];

            Contract.Assert(method != null);

            method = method.MakeGenericMethod(itemType);

            return Expression.Call(method, childProducts);
        }

        private Expression VisitAverage(CommonTree node, Expression[] childProducts, Expression param)
        {
            return VisitAggregateWithName("Average", childProducts);
        }

        private Expression VisitSum(CommonTree node, Expression[] childProducts, Expression param)
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
                foreach (var child in tree.TreeChildren())
                    yield return child;
        }

        protected static Expression ApplyEnsuringNullablesHaveValues(Func<Expression, Expression, Expression> binaryOp, Expression left, Expression right)
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

        protected static Expression ApplyWithNullAsValidAlternative(Func<Expression, Expression, Expression> binaryOp, Expression left, Expression right)
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

        protected static void NormalizeTypes(ref Expression left, ref Expression right)
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

        protected static Expression CastIfNeeded(Expression expression, Type type)
        {
            //possibly enum convertion code should be here
            return !type.IsAssignableFrom(expression.Type) ? Expression.Convert(expression, type) : expression;
        }
    }
}
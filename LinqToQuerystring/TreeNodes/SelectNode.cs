namespace LinqToQuerystring.TreeNodes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    using Antlr.Runtime;

    using LinqToQuerystring.TreeNodes.Base;

    public class SelectNode : TreeNode
    {
        public SelectNode(IToken payload, TreeNodeFactory treeNodeFactory)
            : base(payload, treeNodeFactory)
        {
        }

        public LambdaExpression BuildProjection(Type elementType)
        {
            var parameter = Expression.Parameter(elementType, "o");

            var addMethod = typeof(Dictionary<string, object>).GetMethod("Add");

            var elements = this.ChildNodes.Select(
                o => Expression.ElementInit(
                    addMethod, Expression.Constant(o.Text), 
                    Expression.Convert(o.BuildLinqExpression(parameter), typeof(object))
                )
            );

            var newDictionary = Expression.New(typeof(Dictionary<string, object>));

            var init = Expression.ListInit(newDictionary, elements);

            var lambda = Expression.Lambda(init, new[] { parameter });

            return lambda;
        }


        public override Expression BuildLinqExpression(Expression item = null)
        {
            throw new NotSupportedException("Select is just a placeholder and should be handled differently in Extensions.cs");
        }

        public override int CompareTo(TreeNode other)
        {
            // Select clause should always be last apart from inlinecount
            if (other is InlineCountNode)
            {
                return -1;
            }

            return 1;
        }
    }
}
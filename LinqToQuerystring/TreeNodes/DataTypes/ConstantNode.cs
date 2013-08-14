namespace LinqToQuerystring.TreeNodes.DataTypes
{
    using System.Linq.Expressions;
    using Antlr.Runtime;
    using Base;

    public abstract class ConstantNode<T> : TreeNode
    {
        private bool _init;

        private T _val;

        public T Value
        {
            get
            {
                if (!_init)
                {
                    _val = ParseValue(payload.Text);
                    _init = true;
                }
                    
                return _val;
            }
        }

        protected ConstantNode(IToken payload, TreeNodeFactory treeNodeFactory)
            : base(payload, treeNodeFactory)
        {
            
        }

        public override Expression BuildLinqExpression(Expression item = null)
        {
            return Expression.Constant(Value, typeof(T));
        }

        protected abstract T ParseValue(string text);
    }
}
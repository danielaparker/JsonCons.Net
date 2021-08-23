using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
        
namespace JsonCons.JmesPath
{

    static class JsonConstants
    {
        static JsonConstants()
        {
            True = new TrueValue();
            False = new FalseValue();
            Null = new NullValue();
        }

        internal static IValue True {get;}
        internal static IValue False {get;}
        internal static IValue Null {get;}
    }

    interface IExpression
    {
         bool TryEvaluate(DynamicResources resources,
                          IValue current, 
                          out IValue value);

         int PrecedenceLevel {get;} 

         bool IsProjection {get;} 

         bool IsRightAssociative {get;}

        void AddExpression(IExpression expr);
    }

    // BaseExpression
    abstract class BaseExpression : IExpression
    {
        public int PrecedenceLevel {get;} 

        public bool IsRightAssociative {get;} 

        public bool IsProjection {get;} 

        internal BaseExpression(Operator oper,
                                bool isProjection)
        {
            PrecedenceLevel = OperatorTable.PrecedenceLevel(oper);
            IsRightAssociative = OperatorTable.IsRightAssociative(oper);
            IsProjection = isProjection;
        }

        public abstract bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value);

        public virtual void AddExpression(IExpression expressions)
        {
        }

        public override string ToString()
        {
            return "ToString not implemented";
        }
    }  

    sealed class IdentifierSelector : BaseExpression
    {
        string _identifier;
    
        internal IdentifierSelector(string name)
            : base(Operator.Default, false)
        {
            _identifier = name;
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            if (current.Type == JmesPathType.Object && current.TryGetProperty(_identifier, out value))
            {
                return true;
            }
            else 
            {
                value = JsonConstants.Null;
                return true;
            }
        }

        public override string ToString()
        {
            return $"IdentifierSelector {_identifier}";
        }
    };

    sealed class CurrentNode : BaseExpression
    {
        internal CurrentNode()
            : base(Operator.Default, false)
        {
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            value = current;
            return true;
        }

        public override string ToString()
        {
            return "CurrentNode";
        }
    };

    sealed class IndexSelector : BaseExpression
    {
        Int32 _index;
        internal IndexSelector(Int32 index)
            : base(Operator.Default, false)
        {
            _index = index;
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            if (current.Type != JmesPathType.Array)
            {
                value = JsonConstants.Null;
                return true;
            }
            Int32 slen = current.GetArrayLength();
            if (_index >= 0 && _index < slen)
            {
                value = current[_index];
            }
            else if ((slen + _index) >= 0 && (slen+_index) < slen)
            {
                Int32 index = slen + _index;
                value = current[index];
            }
            else
            {
                value = JsonConstants.Null;
            }
            return true;
        }

        public override string ToString()
        {
            return $"Index Selector {_index}";
        }
    };

    abstract class Projection : BaseExpression
    {
        List<IExpression> _expressions;

        internal Projection(Operator oper = Operator.Projection)
            : base(oper, true)
        {
            _expressions = new List<IExpression>();
        }

        public override void AddExpression(IExpression expr)
        {
            if (_expressions.Count != 0 && _expressions[_expressions.Count-1].IsProjection && 
                (expr.PrecedenceLevel < _expressions[_expressions.Count-1].PrecedenceLevel ||
                 (expr.PrecedenceLevel == _expressions[_expressions.Count-1].PrecedenceLevel && expr.IsRightAssociative)))
            {
                _expressions[_expressions.Count-1].AddExpression(expr);
            }
            else
            {
                _expressions.Add(expr);
            }
        }
        internal bool TryApplyExpressions(DynamicResources resources, IValue current, out IValue value)
        {
            value = current;
            foreach (var expression in _expressions)
            {
                if (!expression.TryEvaluate(resources, value, out value))
                {
                    return false;
                }
            }
            return true;
        }
    };

    sealed class ObjectProjection : Projection
    {
        internal static ObjectProjection Instance {get;} = new ObjectProjection();

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            if (current.Type != JmesPathType.Object)
            {
                value = JsonConstants.Null;
                return true;
            }

            var result = new List<IValue>();
            value = new ArrayValue(result);
            foreach (var item in current.EnumerateObject())
            {
                if (item.Value.Type != JmesPathType.Null)
                {
                    IValue val;
                    if (!this.TryApplyExpressions(resources, item.Value, out val))
                    {
                        return false;
                    }
                    if (val.Type != JmesPathType.Null)
                    {
                        result.Add(val);
                    }
                }
            }
            return true;
        }

        public override string ToString()
        {
            return "ObjectProjection";
        }
    };
    sealed class ListProjection : Projection
    {    
        internal static ListProjection Instance {get;} = new ListProjection();

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            if (current.Type != JmesPathType.Array)
            {
                value = JsonConstants.Null;
                return true;
            }

            var result = new List<IValue>();
            foreach (var item in current.EnumerateArray())
            {
                if (item.Type != JmesPathType.Null)
                {
                    IValue val;
                    if (!this.TryApplyExpressions(resources, item, out val))
                    {
                        value = JsonConstants.Null;
                        return false;
                    }
                    if (val.Type != JmesPathType.Null)
                    {
                        result.Add(val);
                    }
                }
            }
            value = new ArrayValue(result);
            return true;
        }

        public override string ToString()
        {
            return "ListProjection";
        }
    };

    sealed class FlattenProjection : Projection
    {
        internal static FlattenProjection Instance {get;} = new FlattenProjection();

        internal FlattenProjection()
            : base(Operator.FlattenProjection)
        {
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            if (current.Type != JmesPathType.Array)
            {
                value = JsonConstants.Null;
                return true;
            }

            var result = new List<IValue>();
            foreach (var item in current.EnumerateArray())
            {
                if (item.Type == JmesPathType.Array)
                {
                    foreach (var elem in item.EnumerateArray())
                    {
                        if (elem.Type != JmesPathType.Null)
                        {
                            IValue val;
                            if (!this.TryApplyExpressions(resources, elem, out val))
                            {
                                value = JsonConstants.Null;
                                return false;
                            }
                            if (val.Type != JmesPathType.Null)
                            {
                                result.Add(val);
                            }
                        }
                    }
                }
                else
                {
                    if (item.Type != JmesPathType.Null)
                    {
                        IValue val;
                        if (!this.TryApplyExpressions(resources, item, out val))
                        {
                            value = JsonConstants.Null;
                            return false;
                        }
                        if (val.Type != JmesPathType.Null)
                        {
                            result.Add(val);
                        }
                    }
                }
            }

            value = new ArrayValue(result);
            return true;
        }

        public override string ToString()
        {
            return "FlattenProjection";
        }
    };

    sealed class SliceProjection : Projection
    {
        Slice _slice;
    
        internal SliceProjection(Slice s)
        {
            _slice = s;
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            if (current.Type != JmesPathType.Array)
            {
                value = JsonConstants.Null;
                return true;
            }

            var start = _slice.GetStart(current.GetArrayLength());
            var end = _slice.GetStop(current.GetArrayLength());
            var step = _slice.Step;

            if (step == 0)
            {
                value = JsonConstants.Null;
                return false;;
            }

            var result = new List<IValue>();
            if (step > 0)
            {
                if (start < 0)
                {
                    start = 0;
                }
                if (end > current.GetArrayLength())
                {
                    end = current.GetArrayLength();
                }
                for (Int32 i = start; i < end; i += step)
                {
                    IValue val;
                    if (!this.TryApplyExpressions(resources, current[i], out val))
                    {
                        value = JsonConstants.Null;
                        return false;
                    }
                    if (val.Type != JmesPathType.Null)
                    {
                        result.Add(val);
                    }
                }
            }
            else
            {
                if (start >= current.GetArrayLength())
                {
                    start = current.GetArrayLength() - 1;
                }
                if (end < -1)
                {
                    end = -1;
                }
                for (Int32 i = start; i > end; i += step)
                {
                    IValue val;
                    if (!this.TryApplyExpressions(resources, current[i], out val))
                    {
                        value = JsonConstants.Null;
                        return false;
                    }
                    if (val.Type != JmesPathType.Null)
                    {
                        result.Add(val);
                    }
                }
            }

            value = new ArrayValue(result);
            return true;
        }

        public override string ToString()
        {
            return "SliceProjection";
        }
    };

    sealed class FilterExpression : Projection
    {
        readonly Expression _expr;
    
        internal FilterExpression(Expression expr)
        {
            _expr = expr;
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            if (current.Type != JmesPathType.Array)
            {
                value = JsonConstants.Null;
                return true;
            }
            var result = new List<IValue>();

            foreach (var item in current.EnumerateArray())
            {
                IValue test;
                if (!_expr.TryEvaluate(resources, item, out test))
                {
                    value = JsonConstants.Null;
                    return false;
                }
                if (Expression.IsTrue(test))
                {
                    IValue val;
                    if (!this.TryApplyExpressions(resources, item, out val))
                    {
                        value = JsonConstants.Null;
                        return false;
                    }
                    if (val.Type != JmesPathType.Null)
                    {
                        result.Add(val);
                    }
                }
            }
            value = new ArrayValue(result);
            return true;
        }

        public override string ToString()
        {
            return "FilterExpression";
        }
    };

    sealed class MultiSelectList : BaseExpression
    {
        IList<Expression> _expressions;
    
        internal MultiSelectList(IList<Expression> expressions)
            : base(Operator.Default, false)
        {
            _expressions = expressions;
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            if (current.Type == JmesPathType.Null)
            {
                value = JsonConstants.Null;
                return true;
            }
            var result = new List<IValue>();

            foreach (var expr in _expressions)
            {
                IValue val;
                if (!expr.TryEvaluate(resources, current, out val))
                {
                    value = JsonConstants.Null;
                    return false;
                }
                result.Add(val);
            }
            value = new ArrayValue(result);
            return true;
        }

        public override string ToString()
        {
            return "MultiSelectList";
        }
    };

    struct KeyExpressionPair
    {
        internal string Key {get;}
        internal Expression Expression {get;}

        internal KeyExpressionPair(string key, Expression expression) 
        {
            Key = key;
            Expression = expression;
        }
    };

    sealed class MultiSelectHash : BaseExpression
    {
    
        IList<KeyExpressionPair> _keyExprPairs;

        internal MultiSelectHash(IList<KeyExpressionPair> keyExprPairs)
            : base(Operator.Default, false)
        {
            _keyExprPairs = keyExprPairs;
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            if (current.Type == JmesPathType.Null)
            {
                value = JsonConstants.Null;
                return true;
            }
            var result = new Dictionary<string,IValue>();
            foreach (var item in _keyExprPairs)
            {
                IValue val;
                if (!item.Expression.TryEvaluate(resources, current, out val))
                {
                    value = JsonConstants.Null;
                    return false;
                }
                result.Add(item.Key, val);
            }

            value = new ObjectValue(result);
            return true;
        }

        public override string ToString()
        {
            return "MultiSelectHash";
        }
    }

    sealed class FunctionExpression : BaseExpression
    {
        Expression _expr;

        internal FunctionExpression(Expression expr)
            : base(Operator.Default, false)
        {
            _expr = expr;
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            IValue val;
            if (!_expr.TryEvaluate(resources, current, out val))
            {
                value = JsonConstants.Null;
                return true;
            }
            value = val;
            return true;
        }

        public override string ToString()
        {
            return "FunctionExpression";
        }
    }

    class Expression
    {
        Token[] _tokens;

        internal Expression(Token[] tokens)
        {
            _tokens = tokens;
        }

        public  bool TryEvaluate(DynamicResources resources,
                                 IValue current, 
                                 out IValue result)
        {
            Stack<IValue> stack = new Stack<IValue>();
            IList<IValue> argStack = new List<IValue>();

            IValue root_ptr = current;

            for (int i = 0; i < _tokens.Length; ++i)
            {
                var token = _tokens[i];
                switch (token.TokenKind)
                {
                    case JmesPathTokenKind.Literal:
                    {
                        stack.Push(token.GetValue());
                        break;
                    }
                    case JmesPathTokenKind.BeginExpressionType:
                    {
                        Debug.Assert(i+1 < _tokens.Length);
                        token = _tokens[i+1];
                        Debug.Assert(token.TokenKind == JmesPathTokenKind.Expression);
                        Debug.Assert(stack.Count != 0);
                        stack.Pop();
                        stack.Push(new ExpressionValue(token.GetExpression()));
                        break;
                    }
                    case JmesPathTokenKind.Pipe:
                    {
                        Debug.Assert(stack.Count != 0);
                        root_ptr = stack.Peek();
                        break;
                    }
                    case JmesPathTokenKind.CurrentNode:
                        stack.Push(root_ptr);
                        break;
                    case JmesPathTokenKind.Expression:
                    {
                        Debug.Assert(stack.Count != 0);
                        var ptr = stack.Pop();
                        IValue val;
                        if (!token.GetExpression().TryEvaluate(resources, ptr, out val))
                        {
                            result = JsonConstants.Null;
                            return false;
                        }
                        stack.Push(val);
                        break;
                    }
                    case JmesPathTokenKind.UnaryOperator:
                    {
                        Debug.Assert(stack.Count >= 1);
                        var rhs = stack.Pop();
                        IValue val;
                        if (!token.GetUnaryOperator().TryEvaluate(rhs, out val))
                        {
                            result = JsonConstants.Null;
                            return false;
                        }
                        stack.Push(val);
                        break;
                    }
                    case JmesPathTokenKind.BinaryOperator:
                    {
                        Debug.Assert(stack.Count >= 2);
                        var rhs = stack.Pop();
                        var lhs = stack.Pop();
                        IValue val;
                        if (!token.GetBinaryOperator().TryEvaluate(lhs, rhs, out val))
                        {
                            result = JsonConstants.Null;
                            return false;
                        }
                        stack.Push(val);
                        break;
                    }
                    case JmesPathTokenKind.Argument:
                    {
                        Debug.Assert(stack.Count != 0);
                        argStack.Add(stack.Pop());
                        break;
                    }
                    case JmesPathTokenKind.Function:
                    {
                        if (token.GetFunction().Arity != null && token.GetFunction().Arity != argStack.Count())
                        {
                            // airty error should never happen here
                            result = JsonConstants.Null;
                            return false;
                        }

                        IValue val;
                        if (!token.GetFunction().TryEvaluate(argStack, out val))
                        {
                            result = JsonConstants.Null;
                            return false;
                        }
                        argStack.Clear();
                        stack.Push(val);
                        break;
                    }
                    default:
                        break;
                }
            }
            Debug.Assert(stack.Count == 1);
            result = stack.Peek();
            return true;
        }

        internal static bool IsFalse(IValue val)
        {
            var comparer = ValueEqualityComparer.Instance;
            switch (val.Type)
            {
                case JmesPathType.False:
                    return true;
                case JmesPathType.Null:
                    return true;
                case JmesPathType.Array:
                    return val.GetArrayLength() == 0;
                case JmesPathType.Object:
                    return val.EnumerateObject().MoveNext() == false;
                case JmesPathType.String:
                    return val.GetString().Length == 0;
                case JmesPathType.Number:
                    return false;
                default:
                    return false;
            }
        }

        internal static bool IsTrue(IValue val)
        {
            return !IsFalse(val);
        }
    }

} // namespace JsonCons.JmesPath


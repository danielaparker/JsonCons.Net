using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
        
namespace JsonCons.JmesPath
{
    interface IBinaryOperator 
    {
        int PrecedenceLevel {get;}
        bool IsRightAssociative {get;}
        bool TryEvaluate(IValue lhs, IValue rhs, out IValue result);
    };

    abstract class BinaryOperator : IBinaryOperator
    {
        internal BinaryOperator(Operator oper)
        {
            PrecedenceLevel = OperatorTable.PrecedenceLevel(oper);
        }

        public int PrecedenceLevel {get;} 

        public bool IsRightAssociative {get;} = false;

        public abstract bool TryEvaluate(IValue lhs, IValue rhs, out IValue result);
    };

    sealed class OrOperator : BinaryOperator
    {
        internal static OrOperator Instance { get; } = new OrOperator();

        internal OrOperator()
            : base(Operator.Or)
        {
        }

        public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result)
        {
            if (lhs.Type == JmesPathType.Null && rhs.Type == JmesPathType.Null)
            {
                result = lhs;
                return true;
            }
            result = Expression.IsTrue(lhs) ? lhs : rhs;
            return true;
        }

        public override string ToString()
        {
            return "OrOperator";
        }
    };

    sealed class AndOperator : BinaryOperator
    {
        internal static AndOperator Instance { get; } = new AndOperator();

        internal AndOperator()
            : base(Operator.And)
        {
        }

        public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result)
        {
            result = Expression.IsTrue(lhs) ? rhs : lhs;
            return true;
        }

        public override string ToString()
        {
            return "AndOperator";
        }
    };

    sealed class EqOperator : BinaryOperator
    {
        internal static EqOperator Instance { get; } = new EqOperator();

        internal EqOperator()
            : base(Operator.Eq)
        {
        }

        public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result) 
        {
            var comparer = ValueEqualityComparer.Instance;
            if (comparer.Equals(lhs, rhs))
            {
                result = JsonConstants.True;
            }
            else
            {
                result = JsonConstants.False;
            }
            return true;
        }

        public override string ToString()
        {
            return "EqOperator";
        }
    };

    sealed class NeOperator : BinaryOperator
    {
        internal static NeOperator Instance { get; } = new NeOperator();

        internal NeOperator()
            : base(Operator.Ne)
        {
        }

        public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result) 
        {
            IValue value;
            if (!EqOperator.Instance.TryEvaluate(lhs, rhs, out value))
            {
                result = JsonConstants.Null;
                return false;
            }
                
            if (Expression.IsFalse(value)) 
            {
                result = JsonConstants.True;
            }
            else
            {
                result =  JsonConstants.False;
            }
            return true;
        }

        public override string ToString()
        {
            return "NeOperator";
        }
    };

    sealed class LtOperator : BinaryOperator
    {
        internal static LtOperator Instance { get; } = new LtOperator();

        internal LtOperator()
            : base(Operator.Lt)
        {
        }

        public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result) 
        {
            if (lhs.Type == JmesPathType.Number && rhs.Type == JmesPathType.Number)
            {
                Decimal dec1;
                Decimal dec2;
                double val1;
                double val2;
                if (lhs.TryGetDecimal(out dec1) && rhs.TryGetDecimal(out dec2))
                {
                    result = dec1 < dec2 ? JsonConstants.True : JsonConstants.False;
                }
                else if (lhs.TryGetDouble(out val1) && rhs.TryGetDouble(out val2))
                {
                    result = val1 < val2 ? JsonConstants.True : JsonConstants.False;
                }
                else
                {
                    result = JsonConstants.Null;
                }
            }
            else if (lhs.Type == JmesPathType.String && rhs.Type == JmesPathType.String)
            {
                result = String.CompareOrdinal(lhs.GetString(), rhs.GetString()) < 0 ? JsonConstants.True : JsonConstants.False;
            }
            else
            {
                result = JsonConstants.Null;
            }
            return true;
        }

        public override string ToString()
        {
            return "LtOperator";
        }
    };

    sealed class LteOperator : BinaryOperator
    {
        internal static LteOperator Instance { get; } = new LteOperator();

        internal LteOperator()
            : base(Operator.Lte)
        {
        }

        public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result) 
        {
            if (lhs.Type == JmesPathType.Number && rhs.Type == JmesPathType.Number)
            {
                Decimal dec1;
                Decimal dec2;
                double val1;
                double val2;
                if (lhs.TryGetDecimal(out dec1) && rhs.TryGetDecimal(out dec2))
                {
                    result = dec1 <= dec2 ? JsonConstants.True : JsonConstants.False;
                }
                else if (lhs.TryGetDouble(out val1) && rhs.TryGetDouble(out val2))
                {
                    result = val1 <= val2 ? JsonConstants.True : JsonConstants.False;
                }
                else
                {
                    result = JsonConstants.Null;
                }
            }
            else if (lhs.Type == JmesPathType.String && rhs.Type == JmesPathType.String)
            {
                result = String.CompareOrdinal(lhs.GetString(), rhs.GetString()) <= 0 ? JsonConstants.True : JsonConstants.False;
            }
            else
            {
                result = JsonConstants.Null;
            }
            return true;
        }


        public override string ToString()
        {
            return "LteOperator";
        }
    };

    sealed class GtOperator : BinaryOperator
    {
        internal static GtOperator Instance { get; } = new GtOperator();

        internal GtOperator()
            : base(Operator.Gt)
        {
        }

        public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result)
        {
            if (lhs.Type == JmesPathType.Number && rhs.Type == JmesPathType.Number)
            {
                Decimal dec1;
                Decimal dec2;
                double val1;
                double val2;
                if (lhs.TryGetDecimal(out dec1) && rhs.TryGetDecimal(out dec2))
                {
                    result = dec1 > dec2 ? JsonConstants.True : JsonConstants.False;
                }
                else if (lhs.TryGetDouble(out val1) && rhs.TryGetDouble(out val2))
                {
                    result = val1 > val2 ? JsonConstants.True : JsonConstants.False;
                }
                else
                {
                    result = JsonConstants.Null;
                }
            }
            else if (lhs.Type == JmesPathType.String && rhs.Type == JmesPathType.String)
            {
                result = String.CompareOrdinal(lhs.GetString(), rhs.GetString()) > 0 ? JsonConstants.True : JsonConstants.False;
            }
            else
            {
                result = JsonConstants.Null;
            }
            return true;
        }

        public override string ToString()
        {
            return "GtOperator";
        }
    };

    sealed class GteOperator : BinaryOperator
    {
        internal static GteOperator Instance { get; } = new GteOperator();

        internal GteOperator()
            : base(Operator.Gte)
        {
        }

        public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result)
        {
            if (lhs.Type == JmesPathType.Number && rhs.Type == JmesPathType.Number)
            {
                Decimal dec1;
                Decimal dec2;
                double val1;
                double val2;
                if (lhs.TryGetDecimal(out dec1) && rhs.TryGetDecimal(out dec2))
                {
                    result = dec1 >= dec2 ? JsonConstants.True : JsonConstants.False;
                }
                else if (lhs.TryGetDouble(out val1) && rhs.TryGetDouble(out val2))
                {
                    result = val1 >= val2 ? JsonConstants.True : JsonConstants.False;
                }
                else
                {
                    result = JsonConstants.Null;
                }
            }
            else if (lhs.Type == JmesPathType.String && rhs.Type == JmesPathType.String)
            {
                result = String.CompareOrdinal(lhs.GetString(), rhs.GetString()) >= 0 ? JsonConstants.True : JsonConstants.False;
            }
            else
            {
                result = JsonConstants.Null;
            }
            return true;
        }

        public override string ToString()
        {
            return "GteOperator";
        }
    };

} // namespace JsonCons.JmesPath


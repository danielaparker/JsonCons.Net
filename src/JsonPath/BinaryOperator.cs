using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using NUnit.Framework;
        
namespace JsonCons.JsonPathLib
{
    interface IBinaryOperator 
    {
        int PrecedenceLevel {get;}
        bool IsRightAssociative {get;}
        IJsonValue Evaluate(IJsonValue lhs, IJsonValue rhs);
    };

    abstract class BinaryOperator : IBinaryOperator
    {
        internal BinaryOperator(int precedenceLevel,
                                bool isRightAssociative = false)
        {
            PrecedenceLevel = precedenceLevel;
            IsRightAssociative = isRightAssociative;
        }

        public int PrecedenceLevel {get;} 

        public bool IsRightAssociative {get;} 

        public abstract IJsonValue Evaluate(IJsonValue lhs, IJsonValue rhs);
    };

    class OrOperator : BinaryOperator
    {
        internal static OrOperator Instance { get; } = new OrOperator();

        internal OrOperator()
            : base(9)
        {
        }

        public override IJsonValue Evaluate(IJsonValue lhs, IJsonValue rhs)
        {
            if (lhs.ValueKind == JsonValueKind.Null && rhs.ValueKind == JsonValueKind.Null)
            {
                return JsonConstants.Null;
            }
            if (!Expression.IsFalse(lhs))
            {
                return lhs;
            }
            else
            {
                return rhs;
            }
        }

        public override string ToString()
        {
            return "OrOperator";
        }
    };

    class AndOperator : BinaryOperator
    {
        internal static AndOperator Instance { get; } = new AndOperator();

        internal AndOperator()
            : base(8)
        {
        }

        public override IJsonValue Evaluate(IJsonValue lhs, IJsonValue rhs)
        {
            if (Expression.IsTrue(lhs))
            {
                return rhs;
            }
            else
            {
                return lhs;
            }
        }

        public override string ToString()
        {
            return "AndOperator";
        }
    };

    class EqOperator : BinaryOperator
    {
        internal static EqOperator Instance { get; } = new EqOperator();

        internal EqOperator()
            : base(6)
        {
        }

        public override IJsonValue Evaluate(IJsonValue lhs, IJsonValue rhs) 
        {
            var comparer = JsonValueEqualityComparer.Instance;
            return comparer.Equals(lhs, rhs) ? JsonConstants.True : JsonConstants.False;
        }

        public override string ToString()
        {
            return "EqOperator";
        }
    };

    class NeOperator : BinaryOperator
    {
        internal static NeOperator Instance { get; } = new NeOperator();

        internal NeOperator()
            : base(6)
        {
        }

        public override IJsonValue Evaluate(IJsonValue lhs, IJsonValue rhs) 
        {
            return Expression.IsFalse(EqOperator.Instance.Evaluate(lhs, rhs)) ? JsonConstants.True : JsonConstants.False;
        }

        public override string ToString()
        {
            return "NeOperator";
        }
    };

    class LtOperator : BinaryOperator
    {
        internal static LtOperator Instance { get; } = new LtOperator();

        internal LtOperator()
            : base(5)
        {
        }

        public override IJsonValue Evaluate(IJsonValue lhs, IJsonValue rhs) 
        {
            if (lhs.ValueKind == JsonValueKind.Number && rhs.ValueKind == JsonValueKind.Number)
            {
                Decimal dec1;
                Decimal dec2;
                double val1;
                double val2;
                if (lhs.TryGetDecimal(out dec1) && rhs.TryGetDecimal(out dec2))
                {
                    return dec1 < dec2 ? JsonConstants.True : JsonConstants.False;
                }
                else if (lhs.TryGetDouble(out val1) && rhs.TryGetDouble(out val2))
                {
                    return val1 < val2 ? JsonConstants.True : JsonConstants.False;
                }
                else
                {
                    return JsonConstants.Null;
                }
            }
            else if (lhs.ValueKind == JsonValueKind.String && rhs.ValueKind == JsonValueKind.String)
            {
                return String.CompareOrdinal(lhs.GetString(), rhs.GetString()) < 0 ? JsonConstants.True : JsonConstants.False;
            }
            else
            {
                return JsonConstants.Null;
            }
        }

        public override string ToString()
        {
            return "LtOperator";
        }
    };

    class LteOperator : BinaryOperator
    {
        internal static LteOperator Instance { get; } = new LteOperator();

        internal LteOperator()
            : base(5)
        {
        }

        public override IJsonValue Evaluate(IJsonValue lhs, IJsonValue rhs) 
        {
            var val = LtOperator.Instance.Evaluate(rhs, lhs);
            return Expression.IsFalse(val) ? JsonConstants.True : JsonConstants.False;
        }


        public override string ToString()
        {
            return "LteOperator";
        }
    };

    class GtOperator : BinaryOperator
    {
        internal static GtOperator Instance { get; } = new GtOperator();

        internal GtOperator()
            : base(5)
        {
        }

        public override IJsonValue Evaluate(IJsonValue lhs, IJsonValue rhs)
        {
            var val = LtOperator.Instance.Evaluate(rhs,lhs);
            return Expression.IsTrue(val) ? JsonConstants.True : JsonConstants.False;
        }

        public override string ToString()
        {
            return "GtOperator";
        }
    };

    class GteOperator : BinaryOperator
    {
        internal static GteOperator Instance { get; } = new GteOperator();

        internal GteOperator()
            : base(5)
        {
        }

        public override IJsonValue Evaluate(IJsonValue lhs, IJsonValue rhs)
        {
            var val = LtOperator.Instance.Evaluate(lhs, rhs);
            return Expression.IsFalse(val) ? JsonConstants.True : JsonConstants.False;
        }

        public override string ToString()
        {
            return "GteOperator";
        }
    };

    class PlusOperator : BinaryOperator
    {
        internal static PlusOperator Instance { get; } = new PlusOperator();

        internal PlusOperator()
            : base(4)
        {
        }

        public override IJsonValue Evaluate(IJsonValue lhs, IJsonValue rhs)
        {
            if (!(lhs.ValueKind == JsonValueKind.Number && rhs.ValueKind == JsonValueKind.Number))
            {
                return JsonConstants.Null;
            }

            Decimal decVal1;
            double dblVal1;
            Decimal decVal2;
            double dblVal2;

            if (lhs.TryGetDecimal(out decVal1) && rhs.TryGetDecimal(out decVal2))
            {
                Decimal val = decVal1 + decVal2;
                return new DecimalJsonValue(val);
            }
            else if (lhs.TryGetDouble(out dblVal1) && rhs.TryGetDouble(out dblVal2))
            {
                double val = dblVal1 + dblVal2;
                return new DoubleJsonValue(val);
            }
            else
            {
                return JsonConstants.Null;
            }
        }

        public override string ToString()
        {
            return "PlusOperator";
        }
    };

    class MinusOperator : BinaryOperator
    {
        internal static MinusOperator Instance { get; } = new MinusOperator();

        internal MinusOperator()
            : base(4)
        {
        }

        public override IJsonValue Evaluate(IJsonValue lhs, IJsonValue rhs)
        {
            if (!(lhs.ValueKind == JsonValueKind.Number && rhs.ValueKind == JsonValueKind.Number))
            {
                return JsonConstants.Null;
            }

            Decimal decVal1;
            double dblVal1;
            Decimal decVal2;
            double dblVal2;

            if (lhs.TryGetDecimal(out decVal1) && rhs.TryGetDecimal(out decVal2))
            {
                Decimal val = decVal1 - decVal2;
                return new DecimalJsonValue(val);
            }
            else if (lhs.TryGetDouble(out dblVal1) && rhs.TryGetDouble(out dblVal2))
            {
                double val = dblVal1 - dblVal2;
                return new DoubleJsonValue(val);
            }
            else
            {
                return JsonConstants.Null;
            }
        }

        public override string ToString()
        {
            return "MinusOperator";
        }
    };

    class MultOperator : BinaryOperator
    {
        internal static MultOperator Instance { get; } = new MultOperator();

        internal MultOperator()
            : base(3)
        {
        }

        public override IJsonValue Evaluate(IJsonValue lhs, IJsonValue rhs)
        {
            if (!(lhs.ValueKind == JsonValueKind.Number && rhs.ValueKind == JsonValueKind.Number))
            {
                return JsonConstants.Null;
            }

            Decimal decVal1;
            double dblVal1;
            Decimal decVal2;
            double dblVal2;

            if (lhs.TryGetDecimal(out decVal1) && rhs.TryGetDecimal(out decVal2))
            {
                Decimal val = decVal1 * decVal2;
                return new DecimalJsonValue(val);
            }
            else if (lhs.TryGetDouble(out dblVal1) && rhs.TryGetDouble(out dblVal2))
            {
                double val = dblVal1 * dblVal2;
                return new DoubleJsonValue(val);
            }
            else
            {
                return JsonConstants.Null;
            }
        }

        public override string ToString()
        {
            return "MultOperator";
        }
    };

    class DivOperator : BinaryOperator
    {
        internal static DivOperator Instance { get; } = new DivOperator();

        internal DivOperator()
            : base(3)
        {
        }

        public override IJsonValue Evaluate(IJsonValue lhs, IJsonValue rhs)
        {
            if (!(lhs.ValueKind == JsonValueKind.Number && rhs.ValueKind == JsonValueKind.Number))
            {
                return JsonConstants.Null;
            }

            Decimal decVal1;
            double dblVal1;
            Decimal decVal2;
            double dblVal2;

            if (lhs.TryGetDecimal(out decVal1) && rhs.TryGetDecimal(out decVal2))
            {
                if (decVal2 == 0)
                {
                    return JsonConstants.Null;
                }
                Decimal val = decVal1 / decVal2;
                return new DecimalJsonValue(val);
            }
            else if (lhs.TryGetDouble(out dblVal1) && rhs.TryGetDouble(out dblVal2))
            {
                if (dblVal2 == 0)
                {
                    return JsonConstants.Null;
                }
                double val = dblVal1 / dblVal2;
                return new DoubleJsonValue(val);
            }
            else
            {
                return JsonConstants.Null;
            }
        }

        public override string ToString()
        {
            return "DivOperator";
        }
    };


    class ModulusOperator : BinaryOperator
    {
        internal static ModulusOperator Instance { get; } = new ModulusOperator();

        internal ModulusOperator()
            : base(3)
        {
        }

        public override IJsonValue Evaluate(IJsonValue lhs, IJsonValue rhs)
        {
            if (!(lhs.ValueKind == JsonValueKind.Number && rhs.ValueKind == JsonValueKind.Number))
            {
                return JsonConstants.Null;
            }

            Decimal decVal1;
            double dblVal1;
            Decimal decVal2;
            double dblVal2;

            if (lhs.TryGetDecimal(out decVal1) && rhs.TryGetDecimal(out decVal2))
            {
                if (decVal2 == 0)
                {
                    return JsonConstants.Null;
                }
                Decimal val = decVal1 % decVal2;
                return new DecimalJsonValue(val);
            }
            else if (lhs.TryGetDouble(out dblVal1) && rhs.TryGetDouble(out dblVal2))
            {
                if (dblVal2 == 0)
                {
                    return JsonConstants.Null;
                }
                double val = dblVal1 % dblVal2;
                return new DoubleJsonValue(val);
            }
            else
            {
                return JsonConstants.Null;
            }
        }


        public override string ToString()
        {
            return "ModulusOperator";
        }
    };

} // namespace JsonCons.JsonPathLib


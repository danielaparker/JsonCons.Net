using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
        
namespace JsonCons.JmesPath
{
    enum Operator
    {
        Default, // Identifier, CurrentNode, Index, MultiSelectList, MultiSelectHash, FunctionExpression
        Projection,
        FlattenProjection, // FlattenProjection
        Or,
        And,
        Eq,
        Ne,
        Lt,
        Lte,
        Gt,
        Gte,
        Not
    }

    static class OperatorTable
    {
        internal static int PrecedenceLevel(Operator oper)
        {
            switch (oper)
            {
                case Operator.Projection:
                    return 1;
                case Operator.FlattenProjection:
                    return 1;
                case Operator.Or:
                    return 2;
                case Operator.And:
                    return 3;
                case Operator.Eq:
                case Operator.Ne:
                    return 4;
                case Operator.Lt:
                case Operator.Lte:
                case Operator.Gt:
                case Operator.Gte:
                    return 5;
                case Operator.Not:
                    return 6;
                default:
                    return 6;
            }
        }

        internal static bool IsRightAssociative(Operator oper)
        {
            switch (oper)
            {
                case Operator.Not:
                    return true;
                case Operator.Projection:
                    return true;
                case Operator.FlattenProjection:
                    return false;
                case Operator.Or:
                case Operator.And:
                case Operator.Eq:
                case Operator.Ne:
                case Operator.Lt:
                case Operator.Lte:
                case Operator.Gt:
                case Operator.Gte:
                    return false;
                default:
                    return false;
            }
        }
    }

} // namespace JsonCons.JmesPath


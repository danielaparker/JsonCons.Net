using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
        
namespace JsonCons.JsonPathLib
{
    interface IExpression {

    };

    class Expression : IExpression
    {
        IReadOnlyList<Token> _tokens;

        public Expression(IReadOnlyList<Token> tokens)
        {
            _tokens = tokens;
        }
    };

} // namespace JsonCons.JsonPathLib


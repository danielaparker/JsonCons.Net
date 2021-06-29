using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
        
namespace JsonCons.JsonPathLib
{
    public enum ResultOptions {Path=1, NoDups=Path|2, Sort=Path|4};

    public class JsonPath
    {
        readonly PathExpression _expr;

        internal JsonPath(ISelector selector)
        {
            _expr = new PathExpression(selector);
        }

        public IReadOnlyList<JsonElement> Select(JsonElement root, ResultOptions options = 0)
        {
            return _expr.Select(root, options);
        }

        public static JsonPath Compile(string expr)
        {

            var compiler = new JsonPathCompiler(expr);
            return compiler.Compile();
        }

    }

} // namespace JsonCons.JsonPathLib

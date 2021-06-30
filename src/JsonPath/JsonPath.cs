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

    public static class JsonPath
    {
        static readonly JsonElement NullJsonElement = JsonDocument.Parse("null").RootElement;

        public static JsonElement Select(JsonElement root, NormalizedPath path)
        {
            JsonElement current = root;
            foreach (var pathNode in path)
            {
                if (pathNode.NodeKind == PathNodeKind.Index)
                {
                    if (current.ValueKind != JsonValueKind.Array || pathNode.GetIndex() >= current.GetArrayLength())
                    {
                        return NullJsonElement; 
                    }
                    current = current[pathNode.GetIndex()];
                }
                else if (pathNode.NodeKind == PathNodeKind.Name)
                {
                    if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(pathNode.GetName(), out current))
                    {
                        return NullJsonElement;
                    }
                }
            }
            return current;
        }

        public static IJsonValue Select(IJsonValue root, NormalizedPath path)
        {
            IJsonValue current = root;
            foreach (var pathNode in path)
            {
                if (pathNode.NodeKind == PathNodeKind.Index)
                {
                    if (current.ValueKind != JsonValueKind.Array || pathNode.GetIndex() >= current.GetArrayLength())
                    {
                        return new NullJsonValue(); 
                    }
                    current = current[pathNode.GetIndex()];
                }
                else if (pathNode.NodeKind == PathNodeKind.Name)
                {
                    if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(pathNode.GetName(), out current))
                    {
                        return new NullJsonValue();
                    }
                }
            }
            return current;
        }

        public static JsonPathExpression Compile(string expr)
        {

            var compiler = new JsonPathCompiler(expr);
            return compiler.Compile();
        }

    }

    public class JsonPathExpression
    {
        readonly PathExpression _expr;
        ResultOptions _requiredOptions;

        internal JsonPathExpression(ISelector selector, bool pathsRequired)
        {
            _expr = new PathExpression(selector);
            if (pathsRequired)
            {
                _requiredOptions = ResultOptions.Path;
            }
        }

        public IReadOnlyList<JsonElement> Select(JsonElement root, ResultOptions options = 0)
        {
            return _expr.Select(root, options | _requiredOptions);
        }

        public IReadOnlyList<NormalizedPath> SelectPaths(JsonElement root, ResultOptions options = 0)
        {
            return _expr.SelectPaths(root, options | _requiredOptions);
        }
    }

} // namespace JsonCons.JsonPathLib

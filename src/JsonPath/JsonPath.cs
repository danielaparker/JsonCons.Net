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
        public static bool TrySelect(JsonElement root, NormalizedPath path, out JsonElement element)
        {
            element = root;
            foreach (var pathNode in path)
            {
                if (pathNode.NodeKind == PathNodeKind.Index)
                {
                    if (element.ValueKind != JsonValueKind.Array || pathNode.GetIndex() >= element.GetArrayLength())
                    {
                        return false; 
                    }
                    element = element[pathNode.GetIndex()];
                }
                else if (pathNode.NodeKind == PathNodeKind.Name)
                {
                    if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(pathNode.GetName(), out element))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static bool TrySelect(IJsonValue root, NormalizedPath path, out IJsonValue value)
        {
            value = root;
            foreach (var pathNode in path)
            {
                if (pathNode.NodeKind == PathNodeKind.Index)
                {
                    if (value.ValueKind != JsonValueKind.Array || pathNode.GetIndex() >= value.GetArrayLength())
                    {
                        return false; 
                    }
                    value = value[pathNode.GetIndex()];
                }
                else if (pathNode.NodeKind == PathNodeKind.Name)
                {
                    if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(pathNode.GetName(), out value))
                    {
                        return false;
                    }
                }
            }
            return false;
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

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
        
namespace JsonCons.JsonPathLib
{
    sealed class DynamicResources 
    {
        Dictionary<Int32,IOperand> _cache = new Dictionary<Int32,IOperand>();

        internal bool IsCached(Int32 id)
        {
            return _cache.ContainsKey(id);
        }

        internal void AddToCache(Int32 id, IOperand value) 
        {
            _cache.Add(id, value);
        }

        internal bool TryRetrieveFromCache(Int32 id, out IOperand result) 
        {
            return _cache.TryGetValue(id, out result);
        }
    };

    public enum ResultOptions {Path=1, NoDups=Path|2, Sort=Path|4};

    public static class JsonPath
    {
        public static IList<JsonElement> Select(JsonElement root, string path, ResultOptions options = 0)
        {
            using (var expr = JsonPathExpression.Compile(path))
            {
                return expr.Select(root, options);
            }
        }

        public static IList<JsonElement> Select(JsonElement root, JsonPathExpression expr, ResultOptions options = 0)
        {
            return expr.Select(root, options);
        }

        public static IList<NormalizedPath> SelectPaths(JsonElement root, string path, ResultOptions options = ResultOptions.Path)
        {
            using (var expr = JsonPathExpression.Compile(path))
            {
                return expr.SelectPaths(root, options);
            }
        }

        public static IList<NormalizedPath> SelectPaths(JsonElement root, JsonPathExpression expr, ResultOptions options = ResultOptions.Path)
        {
            return expr.SelectPaths(root, options);
        }

        public static IList<JsonPathNode> SelectNodes(JsonElement root, string path, ResultOptions options = ResultOptions.Path)
        {
            using (var expr = JsonPathExpression.Compile(path))
            {
                return expr.SelectNodes(root, options);
            }
        }

        public static IList<JsonPathNode> SelectNodes(JsonElement root, JsonPathExpression expr, ResultOptions options = ResultOptions.Path)
        {
            return expr.SelectNodes(root, options);
        }

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
    }

} // namespace JsonCons.JsonPathLib

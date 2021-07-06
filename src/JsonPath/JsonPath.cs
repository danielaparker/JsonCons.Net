using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
        
namespace JsonCons.JsonPathLib
{
    class DynamicResources 
    {
        Dictionary<Int32,IJsonValue> _cache = new Dictionary<Int32,IJsonValue>();

        internal bool IsCached(Int32 id)
        {
            return _cache.ContainsKey(id);
        }

        internal void AddToCache(Int32 id, IJsonValue value) 
        {
            _cache.Add(id, value);
        }

        internal bool TryRetrieveFromCache(Int32 id, out IJsonValue result) 
        {
            return _cache.TryGetValue(id, out result);
        }
    };

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

        internal static bool TrySelect(IJsonValue root, NormalizedPath path, out IJsonValue element)
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

        public static JsonPathExpression Compile(string expr)
        {

            var compiler = new JsonPathCompiler(expr);
            return compiler.Compile();
        }

    }

    public class JsonPathExpression : IDisposable
    {
        private bool _disposed = false;
        StaticResources _resources;
        readonly PathExpression _expr;
        ResultOptions _requiredOptions;

        internal JsonPathExpression(StaticResources resources, ISelector selector, bool pathsRequired)
        {
            _resources = resources;
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    _resources.Dispose();
                }
                _disposed = true;
            }
        }

        ~JsonPathExpression()
        {
            Dispose(false);
        }
    }

} // namespace JsonCons.JsonPathLib

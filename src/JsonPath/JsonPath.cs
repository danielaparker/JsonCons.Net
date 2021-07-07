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


    /// <summary>
    ///   Represents a JsonPath expression.
    /// </summary>
    /// <remarks>
    ///   A JsonPath object may own references to some <see cref="JsonDocument"/> objects. 
    ///   It should be disposed to ensure that these objects are properly disposed.
    /// </remarks>

    public sealed class JsonPath : IDisposable
    {
        private bool _disposed = false;
        readonly StaticResources _resources;
        readonly PathExpression _expr;
        readonly ResultOptions _requiredOptions;

        internal JsonPath(StaticResources resources, ISelector selector, bool pathsRequired)
        {
            _resources = resources;
            _expr = new PathExpression(selector);
            if (pathsRequired)
            {
                _requiredOptions = ResultOptions.Path;
            }
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

        public static JsonPath Parse(string expr)
        {
            var parser = new JsonPathParser(expr);
            return parser.Parse();
        }

        public IReadOnlyList<JsonElement> Select(JsonElement root, ResultOptions options = 0)
        {
            return _expr.Select(root, options | _requiredOptions);
        }

        public IReadOnlyList<NormalizedPath> SelectPaths(JsonElement root, ResultOptions options = 0)
        {
            return _expr.SelectPaths(root, options | _requiredOptions);
        }

        public IReadOnlyList<JsonPathNode> SelectNodes(JsonElement root, ResultOptions options = 0)
        {
            return _expr.SelectNodes(root, options | _requiredOptions);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
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

        ~JsonPath()
        {
            Dispose(false);
        }
    }

} // namespace JsonCons.JsonPathLib

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
        Dictionary<Int32,IValue> _cache = new Dictionary<Int32,IValue>();

        internal bool IsCached(Int32 id)
        {
            return _cache.ContainsKey(id);
        }

        internal void AddToCache(Int32 id, IValue value) 
        {
            _cache.Add(id, value);
        }

        internal bool TryRetrieveFromCache(Int32 id, out IValue result) 
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
        readonly StaticResources _resources;
        readonly ISelector _selector;
        readonly ResultOptions _requiredOptions;

        public static JsonPath Parse(string expr)
        {
            var compiler = new JsonPathParser(expr);
            return compiler.Parse();
        }

        internal JsonPath(StaticResources resources, 
                                    ISelector selector, 
                                    bool pathsRequired)
        {
            _resources = resources;
            _selector = selector;
            if (pathsRequired)
            {
                _requiredOptions = ResultOptions.Path;
            }
        }

        public IList<JsonElement> Select(JsonElement root, ResultOptions options = 0)
        {
            options |= _requiredOptions;

            var resources = new DynamicResources();
            PathNode pathStem = new PathNode("$");
            var values = new List<JsonElement>();

            if ((options & ResultOptions.Sort | options & ResultOptions.NoDups) != 0)
            {
                var nodes = new List<JsonPathNode>();
                INodeAccumulator accumulator = new NodeAccumulator(nodes);
                _selector.Select(resources, 
                                 new JsonElementValue(root), 
                                 pathStem, 
                                 new JsonElementValue(root), 
                                 accumulator, 
                                 options);

                if (nodes.Count > 1)
                {
                    if ((options & ResultOptions.Sort) == ResultOptions.Sort)
                    {
                        nodes.Sort();
                    }
                    if ((options & ResultOptions.NoDups) == ResultOptions.NoDups)
                    {
                        var index = new HashSet<JsonPathNode>(nodes);
                        foreach (var node in nodes)
                        {
                            if (index.Contains(node))
                            {
                                values.Add(node.Value);
                                index.Remove(node);
                            }
                        }
                    }
                    else
                    {
                        foreach (var node in nodes)
                        {
                            values.Add(node.Value);
                        }
                    }
                }
                else
                {
                    foreach (var node in nodes)
                    {
                        values.Add(node.Value);
                    }
                }
            }
            else
            {
                INodeAccumulator accumulator = new JsonElementAccumulator(values);            
                _selector.Select(resources, 
                                 new JsonElementValue(root), 
                                 pathStem, 
                                 new JsonElementValue(root), 
                                 accumulator, 
                                 options);
            }

            return values;
        }

        /// <summary>
        /// Selects paths to elements in root that match the JSONPath expression. 
        /// </summary>
        /// <param name="root">The root.</param>
        /// <param name="options">The result options.</param>
        /// <returns>A list of paths to elements in root that match the provided JSONPath expression</returns>
        
        public IList<NormalizedPath> SelectPaths(JsonElement root, ResultOptions options = ResultOptions.Path)
        {
            options |= _requiredOptions;

            var resources = new DynamicResources();

            PathNode pathStem = new PathNode("$");
            var paths = new List<NormalizedPath>();
            INodeAccumulator accumulator = new PathAccumulator(paths);
            _selector.Select(resources, 
                             new JsonElementValue(root), 
                             pathStem, 
                             new JsonElementValue(root), 
                             accumulator, 
                             options | ResultOptions.Path);

            if ((options & ResultOptions.Sort | options & ResultOptions.NoDups) != 0)
            {
                if (paths.Count > 1)
                {
                    if ((options & ResultOptions.Sort) == ResultOptions.Sort)
                    {
                        paths.Sort();
                    }
                    if ((options & ResultOptions.NoDups) == ResultOptions.NoDups)
                    {
                        var temp = new List<NormalizedPath>();
                        var index = new HashSet<NormalizedPath>(paths);
                        foreach (var path in paths)
                        {
                            if (index.Contains(path))
                            {
                                temp.Add(path);
                                index.Remove(path);
                            }
                        }
                        paths = temp;
                    }
                }
            }

            return paths;
        }

        public IList<JsonPathNode> SelectNodes(JsonElement root, ResultOptions options = ResultOptions.Path)
        {
            options |= _requiredOptions;

            var resources = new DynamicResources();

            PathNode pathStem = new PathNode("$");
            var nodes = new List<JsonPathNode>();
            var accumulator = new NodeAccumulator(nodes);
            _selector.Select(resources, 
                             new JsonElementValue(root), 
                             pathStem, 
                             new JsonElementValue(root), 
                             accumulator, 
                             options | ResultOptions.Path);

            if ((options & ResultOptions.Sort | options & ResultOptions.NoDups) != 0)
            {
                if (nodes.Count > 1)
                {
                    if ((options & ResultOptions.Sort) == ResultOptions.Sort)
                    {
                        nodes.Sort();
                    }
                    if ((options & ResultOptions.NoDups) == ResultOptions.NoDups)
                    {
                        var temp = new List<JsonPathNode>();
                        var index = new HashSet<JsonPathNode>(nodes);
                        foreach (var path in nodes)
                        {
                            if (index.Contains(path))
                            {
                                temp.Add(path);
                                index.Remove(path);
                            }
                        }
                        nodes = temp;
                    }
                }
            }

            return nodes;
        }

        public void Dispose()
        {
            _resources.Dispose();
        }

        public static IList<JsonElement> Select(JsonElement root, string path, ResultOptions options = 0)
        {
            using (var expr = JsonPath.Parse(path))
            {
                return expr.Select(root, options);
            }
        }

        public static IList<NormalizedPath> SelectPaths(JsonElement root, string path, ResultOptions options = ResultOptions.Path)
        {
            using (var expr = JsonPath.Parse(path))
            {
                return expr.SelectPaths(root, options);
            }
        }

        public static IList<JsonPathNode> SelectNodes(JsonElement root, string path, ResultOptions options = ResultOptions.Path)
        {
            using (var expr = JsonPath.Parse(path))
            {
                return expr.SelectNodes(root, options);
            }
        }
    }

} // namespace JsonCons.JsonPathLib

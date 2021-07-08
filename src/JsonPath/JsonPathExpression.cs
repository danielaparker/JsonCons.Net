using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
        
namespace JsonCons.JsonPathLib
{
    /// <summary>
    ///   Represents the compiled form of a JsonPath expression.
    /// </summary>
    /// <remarks>
    ///   A JsonPathExpression object may own references to some <see cref="JsonDocument"/> objects. 
    ///   It should be disposed to ensure that these objects are properly disposed.
    /// </remarks>

    public sealed class JsonPathExpression : IDisposable
    {
        readonly StaticResources _resources;
        readonly ISelector _selector;
        readonly ResultOptions _requiredOptions;

        public static JsonPathExpression Compile(string expr)
        {
            var compiler = new JsonPathCompiler(expr);
            return compiler.Compile();
        }

        internal JsonPathExpression(StaticResources resources, 
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

        public IList<JsonElement> Select(JsonElement root, ResultOptions options)
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
                                 new JsonElementJsonValue(root), 
                                 pathStem, 
                                 new JsonElementJsonValue(root), 
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
                                 new JsonElementJsonValue(root), 
                                 pathStem, 
                                 new JsonElementJsonValue(root), 
                                 accumulator, 
                                 options);
            }

            return values;
        }

        public IList<NormalizedPath> SelectPaths(JsonElement root, ResultOptions options)
        {
            options |= _requiredOptions;

            var resources = new DynamicResources();

            PathNode pathStem = new PathNode("$");
            var paths = new List<NormalizedPath>();
            INodeAccumulator accumulator = new PathAccumulator(paths);
            _selector.Select(resources, 
                             new JsonElementJsonValue(root), 
                             pathStem, 
                             new JsonElementJsonValue(root), 
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

        public IList<JsonPathNode> SelectNodes(JsonElement root, ResultOptions options)
        {
            options |= _requiredOptions;

            var resources = new DynamicResources();

            PathNode pathStem = new PathNode("$");
            var nodes = new List<JsonPathNode>();
            var accumulator = new NodeAccumulator(nodes);
            _selector.Select(resources, 
                             new JsonElementJsonValue(root), 
                             pathStem, 
                             new JsonElementJsonValue(root), 
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
    }

} // namespace JsonCons.JsonPathLib

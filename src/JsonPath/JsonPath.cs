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

    /// <summary>
    /// Defines the various ways a JsonPath query can deal with duplicate
	 /// paths and order of results.
    ///
    /// This enumeration has a FlagsAttribute attribute that allows a bitwise combination of its member values.
    /// </summary>
    
	 [Flags]
    public enum ResultOptions {
        /// <summary>
        /// This bit indicates that paths are required and is automatically set when required, e.g.
        /// if NoDups is set.
        /// </summary>
        Path=1, 
        /// <summary>
        /// Remove items from results that correspond to the same path.
        /// </summary>
        NoDups=Path|2, 
        /// <summary>
        /// Sort results by path.
        /// </summary>
        Sort=Path|4
    };

    /// <summary>
    ///   Represents a JsonPath expression.
    /// </summary>

    public sealed class JsonPath
    {
        readonly ISelector _selector;
        readonly ResultOptions _requiredOptions;

        /// <summary>
        /// Parses a JSONPath string into a JsonPath, for "parse once, use many times".
        /// A JsonPath instance is thread safe and has no mutable state.
        /// </summary>
        /// <param name="pathStr">A JSONPath string.</param>
        /// <returns>A JsonPath.</returns>
        /// <exception cref="JsonPathParseException">
        ///   The <paramref name="pathStr"/> parameter is not a valid JSONPath expression.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="pathStr"/> is <see langword="null"/>.
        /// </exception>
        public static JsonPath Parse(string pathStr)
        {
            if (pathStr == null)
            {
                throw new ArgumentNullException(nameof(pathStr));
            }
            var compiler = new JsonPathParser(pathStr);
            return compiler.Parse();
        }

        internal JsonPath(ISelector selector, 
                          bool pathsRequired)
        {
            _selector = selector;
            if (pathsRequired)
            {
                _requiredOptions = ResultOptions.Path;
            }
        }

        /// <summary>
        /// Selects values within the root value that match this JSONPath expression. 
        /// </summary>
        /// <param name="root">The root value.</param>
        /// <param name="options">Options for handling duplicate paths and order of results.</param>
        /// <returns>A list of values within the root value that match this JSONPath expression</returns>

        public IList<JsonElement> Select(JsonElement root, ResultOptions options = 0)
        {
            options |= _requiredOptions;

            var resources = new DynamicResources();
            PathComponent pathStem = new PathComponent("$");
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
        /// Selects paths identifying the values within the root value that match this JSONPath expression. 
        /// </summary>
        /// <param name="root">The root value.</param>
        /// <param name="options">Options for handling duplicate paths and order of results.</param>
        /// <returns>A list of <see cref="NormalizedPath"/> identifying the values within the root value that match this JSONPath expression</returns>
        
        public IList<NormalizedPath> SelectPaths(JsonElement root, ResultOptions options = ResultOptions.Path)
        {
            options |= _requiredOptions;

            var resources = new DynamicResources();

            PathComponent pathStem = new PathComponent("$");
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

        /// <summary>
        /// Selects nodes that represent location-value pairs within the root value that match this JSONPath expression. 
        /// </summary>
        /// <param name="root">The root value.</param>
        /// <param name="options">Options for handling duplicate paths and order of results.</param>
        /// <returns>A list of <see cref="JsonPathNode"/> representing locations-value pairs 
        /// within the root value that match this JSONPath expression</returns>

        public IList<JsonPathNode> SelectNodes(JsonElement root, ResultOptions options = ResultOptions.Path)
        {
            options |= _requiredOptions;

            var resources = new DynamicResources();

            PathComponent pathStem = new PathComponent("$");
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

        /// <summary>
        /// Selects values within the root value that match the provided JSONPath expression. 
        /// This method parses and applies the expression in one operation.
        /// </summary>
        /// <param name="root">The root value.</param>
        /// <param name="pathStr">A JSONPath string.</param>
        /// <param name="options">Options for handling duplicate paths and order of results.</param>
        /// <returns>A list of values within the root value that match the provided JSONPath expression</returns>
        /// <exception cref="JsonPathParseException">
        ///   The <paramref name="pathStr"/> parameter is not a valid JSONPath expression.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="pathStr"/> is <see langword="null"/>.
        /// </exception>
        public static IList<JsonElement> Select(JsonElement root, string pathStr, ResultOptions options = 0)
        {
            if (pathStr == null)
            {
                throw new ArgumentNullException(nameof(pathStr));
            }
            var expr = JsonPath.Parse(pathStr);
            return expr.Select(root, options);
        }

        /// <summary>
        /// Selects paths identifying the values within the root value that match the JSONPath expression. 
        /// This method parses and applies the expression in one operation.
        /// </summary>
        /// <param name="root">The root value.</param>
        /// <param name="pathStr">A JSONPath string.</param>
        /// <param name="options">Options for handling duplicate paths and order of results.</param>
        /// <returns>A list of <see cref="NormalizedPath"/> identifying the values within the root value that match the provided JSONPath expression</returns>
        /// <exception cref="JsonPathParseException">
        ///   The <paramref name="pathStr"/> parameter is not a valid JSONPath expression.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="pathStr"/> is <see langword="null"/>.
        /// </exception>

        public static IList<NormalizedPath> SelectPaths(JsonElement root, string pathStr, ResultOptions options = ResultOptions.Path)
        {
            if (pathStr == null)
            {
                throw new ArgumentNullException(nameof(pathStr));
            }
            var expr = JsonPath.Parse(pathStr);
            return expr.SelectPaths(root, options);
        }

        /// <summary>
        /// Selects nodes that represent location-value pairs within the root value that match the JSONPath expression. 
        /// This method parses and applies the expression in one operation.
        /// </summary>
        /// <param name="root">The root value.</param>
        /// <param name="pathStr">A JSONPath string.</param>
        /// <param name="options">Options for handling duplicate paths and order of results.</param>
        /// <returns>A list of <see cref="JsonPathNode"/> representing locations-value pairs 
        /// within the root value that match the provided JSONPath expression</returns>
        /// <exception cref="JsonPathParseException">
        ///   The <paramref name="pathStr"/> parameter is not a valid JSONPath expression.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="pathStr"/> is <see langword="null"/>.
        /// </exception>

        public static IList<JsonPathNode> SelectNodes(JsonElement root, string pathStr, ResultOptions options = ResultOptions.Path)
        {
            if (pathStr == null)
            {
                throw new ArgumentNullException(nameof(pathStr));
            }
            var expr = JsonPath.Parse(pathStr);
            return expr.SelectNodes(root, options);
        }

    }

} // namespace JsonCons.JsonPathLib

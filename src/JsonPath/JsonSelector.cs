using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

#nullable enable        

namespace JsonCons.JsonPath
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
    /// Defines the various ways a JsonSelector can handle duplicate
    /// paths and order of results.
    /// </summary>
    public sealed class JsonSelectorOptions 
    {
        /// <summary>
        /// Remove items from results that correspond to the same path.
        /// </summary>
        public bool NoDuplicates {get;set;} = false;

        /// <summary>
        /// Sort results by path.
        /// </summary>
        public bool SortByPath {get;set;} = false;
    };

    /// <summary>
    /// Defines the various ways a JsonSelector query can deal with duplicate
	 /// paths and order of results.
    ///
    /// This enumeration has a FlagsAttribute attribute that allows a bitwise combination of its member values.
    /// </summary>
    
	 [Flags]
    enum ProcessingFlags {
        /// <summary>
        /// This bit indicates that paths are required and is automatically set as needed, e.g.
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
    ///   Provides functionality for retrieving selected values from a root <see href="https://docs.microsoft.com/en-us/dotnet/api/system.text.json.jsonelement?view=net-5.0">JsonElement</see>.
    /// </summary>
    /// <example>
    /// The following example shows how to select values, paths, and nodes from a JSON document
    /// <code>
    /// using System;
    /// using System.Diagnostics;
    /// using System.Text.Json;
    /// using JsonCons.Utilities;
    /// 
    /// public class Example
    /// {
    ///    public static void Main()
    ///    {
    ///     var doc = JsonDocument.Parse(@"
    /// {
    ///     ""books"":
    ///     [
    ///         {
    ///             ""category"": ""fiction"",
    ///             ""title"" : ""A Wild Sheep Chase"",
    ///             ""author"" : ""Haruki Murakami"",
    ///             ""price"" : 22.72
    ///         },
    ///         {
    ///             ""category"": ""fiction"",
    ///             ""title"" : ""The Night Watch"",
    ///             ""author"" : ""Sergei Lukyanenko"",
    ///             ""price"" : 23.58
    ///         },
    ///         {
    ///             ""category"": ""fiction"",
    ///             ""title"" : ""The Comedians"",
    ///             ""author"" : ""Graham Greene"",
    ///             ""price"" : 21.99
    ///         },
    ///         {
    ///             ""category"": ""memoir"",
    ///             ""title"" : ""The Night Watch"",
    ///             ""author"" : ""David Atlee Phillips"",
    ///             ""price"" : 260.90
    ///         }
    ///     ]
    /// }
    ///     ");
    /// 
    ///     var options = new JsonSerializerOptions() {WriteIndented = true};
    /// 
    ///     // Selector of titles from union of all books with category 'memoir' 
    ///     // and all books with price > 23
    ///     var selector = JsonSelector.Parse("$.books[?@.category=='memoir',?@.price > 23].title");
    /// 
    ///     Console.WriteLine("Select values");
    ///     IList&lt;JsonElement> values = selector.Select(doc.RootElement);
    ///     foreach (var value in values)
    ///     {
    ///         Console.WriteLine(JsonSerializer.Serialize(value, options));
    ///     }
    ///     Console.WriteLine();
    /// 
    ///     Console.WriteLine("Select paths");
    ///     IList&lt;NormalizedPath> paths = selector.SelectPaths(doc.RootElement);
    ///     foreach (var path in paths)
    ///     {
    ///         Console.WriteLine(path);
    ///     }
    ///     Console.WriteLine();
    /// 
    ///     Console.WriteLine("Select nodes");
    ///     IList&lt;JsonPathNode> nodes = selector.SelectNodes(doc.RootElement);
    ///     foreach (var node in nodes)
    ///     {
    ///         Console.WriteLine($"{node.Path} => {JsonSerializer.Serialize(node.Value, options)}");
    ///     }
    ///     Console.WriteLine();
    /// 
    ///     Console.WriteLine("Remove duplicate nodes");
    ///     IList&lt;JsonPathNode> uniqueNodes = selector.SelectNodes(doc.RootElement, 
    ///                                                 new JsonSelectorOptions{NoDuplicates=true});
    ///     foreach (var node in uniqueNodes)
    ///     {
    ///         Console.WriteLine($"{node.Path} > {JsonSerializer.Serialize(node.Value, options)}");
    ///     }
    ///     Console.WriteLine();
    ///     }
    /// }
    /// </code>
    /// Output:
    /// 
    /// <code>
    /// Select values
    /// "The Night Watch"
    /// "The Night Watch"
    /// "The Night Watch"
    /// 
    /// Select paths
    /// $['books'][3]['title']
    /// $['books'][1]['title']
    /// $['books'][3]['title']
    /// 
    /// Select nodes
    /// $['books'][3]['title'] => "The Night Watch"
    /// $['books'][1]['title'] => "The Night Watch"
    /// $['books'][3]['title'] => "The Night Watch"
    /// 
    /// Remove duplicate nodes
    /// $['books'][3]['title'] => "The Night Watch"
    /// $['books'][1]['title'] => "The Night Watch"
    /// </code>
    /// </example>

    public sealed class JsonSelector
    {
        readonly ISelector _selector;
        readonly ProcessingFlags _requiredFlags;

        /// <summary>
        /// Parses a JSONPath string into a JsonSelector, for "parse once, use many times".
        /// A JsonSelector instance is thread safe and has no mutable state.
        /// </summary>
        /// <param name="jsonPath">A JSONPath string.</param>
        /// <returns>A JsonSelector.</returns>
        /// <exception cref="JsonPathParseException">
        ///   The <paramref name="jsonPath"/> parameter is not a valid JSONPath expression.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   The <paramref name="jsonPath"/> is <see langword="null"/>.
        /// </exception>
        public static JsonSelector Parse(string jsonPath)
        {
            if (jsonPath == null)
            {
                throw new ArgumentNullException(nameof(jsonPath));
            }
            var compiler = new JsonPathParser(jsonPath);
            return compiler.Parse();
        }

        internal JsonSelector(ISelector selector, 
                          bool pathsRequired)
        {
            _selector = selector;
            if (pathsRequired)
            {
                _requiredFlags = ProcessingFlags.Path;
            }
        }

        /// <summary>
        /// Selects values within the root value that match this JSONPath expression. 
        /// </summary>
        /// <param name="root">The root value.</param>
        /// <param name="options">Options for handling duplicate paths and order of results.</param>
        /// <returns>A list of values within the root value that match this JSONPath expression</returns>

        public IList<JsonElement> Select(JsonElement root, 
                                         JsonSelectorOptions? options = default)
        {
            ProcessingFlags flags = _requiredFlags;
            if (options != null)
            {
                if (options.NoDuplicates)
                {
                    flags |= ProcessingFlags.NoDups;
                }
                if (options.SortByPath)
                {
                    flags |= ProcessingFlags.Sort;
                }
            }

            var resources = new DynamicResources();
            var values = new List<JsonElement>();

            if ((flags & ProcessingFlags.Sort | flags & ProcessingFlags.NoDups) != 0)
            {
                var nodes = new List<JsonPathNode>();
                INodeAccumulator accumulator = new NodeAccumulator(nodes);
                _selector.Select(resources, 
                                 new JsonElementValue(root), 
                                 PathNode.Root, 
                                 new JsonElementValue(root), 
                                 accumulator, 
                                 flags);

                if (nodes.Count > 1)
                {
                    if ((flags & ProcessingFlags.Sort) == ProcessingFlags.Sort)
                    {
                        nodes.Sort();
                    }
                    if ((flags & ProcessingFlags.NoDups) == ProcessingFlags.NoDups)
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
                                 PathNode.Root, 
                                 new JsonElementValue(root), 
                                 accumulator, 
                                 flags);
            }

            return values;
        }

        /// <summary>
        /// Selects paths identifying the values within the root value that match this JSONPath expression. 
        /// </summary>
        /// <param name="root">The root value.</param>
        /// <param name="options">Options for handling duplicate paths and order of results.</param>
        /// <returns>A list of <see cref="NormalizedPath"/> identifying the values within the root value that match this JSONPath expression</returns>

        public IList<NormalizedPath> SelectPaths(JsonElement root, 
                                                 JsonSelectorOptions? options = default)
        {
            ProcessingFlags flags = _requiredFlags;
            if (options != null)
            {
                if (options.NoDuplicates)
                {
                    flags |= ProcessingFlags.NoDups;
                }
                if (options.SortByPath)
                {
                    flags |= ProcessingFlags.Sort;
                }
            }

            var resources = new DynamicResources();

            var paths = new List<NormalizedPath>();
            INodeAccumulator accumulator = new PathAccumulator(paths);
            _selector.Select(resources, 
                             new JsonElementValue(root), 
                             PathNode.Root, 
                             new JsonElementValue(root), 
                             accumulator, 
                             flags | ProcessingFlags.Path);

            if ((flags & ProcessingFlags.Sort | flags & ProcessingFlags.NoDups) != 0)
            {
                if (paths.Count > 1)
                {
                    if ((flags & ProcessingFlags.Sort) == ProcessingFlags.Sort)
                    {
                        paths.Sort();
                    }
                    if ((flags & ProcessingFlags.NoDups) == ProcessingFlags.NoDups)
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

        public IList<JsonPathNode> SelectNodes(JsonElement root, 
                                               JsonSelectorOptions? options = default)
        {
            ProcessingFlags flags = _requiredFlags;
            if (options != null)
            {
                if (options.NoDuplicates)
                {
                    flags |= ProcessingFlags.NoDups;
                }
                if (options.SortByPath)
                {
                    flags |= ProcessingFlags.Sort;
                }
            }

            var resources = new DynamicResources();

            var nodes = new List<JsonPathNode>();
            var accumulator = new NodeAccumulator(nodes);
            _selector.Select(resources, 
                             new JsonElementValue(root), 
                             PathNode.Root, 
                             new JsonElementValue(root), 
                             accumulator, 
                             flags | ProcessingFlags.Path);

            if ((flags & ProcessingFlags.Sort | flags & ProcessingFlags.NoDups) != 0)
            {
                if (nodes.Count > 1)
                {
                    if ((flags & ProcessingFlags.Sort) == ProcessingFlags.Sort)
                    {
                        nodes.Sort();
                    }
                    if ((flags & ProcessingFlags.NoDups) == ProcessingFlags.NoDups)
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
        /// <param name="jsonPath">A JSONPath string.</param>
        /// <param name="options">Options for handling duplicate paths and order of results.</param>
        /// <returns>A list of values within the root value that match the provided JSONPath expression</returns>
        /// <exception cref="JsonPathParseException">
        ///   The <paramref name="jsonPath"/> parameter is not a valid JSONPath expression.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="jsonPath"/> is <see langword="null"/>.
        /// </exception>

        public static IList<JsonElement> Select(JsonElement root, string jsonPath, 
                                                JsonSelectorOptions? options = default)
        {
            if (jsonPath == null)
            {
                throw new ArgumentNullException(nameof(jsonPath));
            }
            var expr = JsonSelector.Parse(jsonPath);
            return expr.Select(root, options);
        }

        /// <summary>
        /// Selects paths identifying the values within the root value that match the JSONPath expression. 
        /// This method parses and applies the expression in one operation.
        /// </summary>
        /// <param name="root">The root value.</param>
        /// <param name="jsonPath">A JSONPath string.</param>
        /// <param name="options">Options for handling duplicate paths and order of results.</param>
        /// <returns>A list of <see cref="NormalizedPath"/> identifying the values within the root value that match the provided JSONPath expression</returns>
        /// <exception cref="JsonPathParseException">
        ///   The <paramref name="jsonPath"/> parameter is not a valid JSONPath expression.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="jsonPath"/> is <see langword="null"/>.
        /// </exception>

        public static IList<NormalizedPath> SelectPaths(JsonElement root, 
                                                        string jsonPath, 
                                                        JsonSelectorOptions? options = default)
        {
            if (jsonPath == null)
            {
                throw new ArgumentNullException(nameof(jsonPath));
            }
            var expr = JsonSelector.Parse(jsonPath);
            return expr.SelectPaths(root, options);
        }

        /// <summary>
        /// Selects nodes that represent location-value pairs within the root value that match the JSONPath expression. 
        /// This method parses and applies the expression in one operation.
        /// </summary>
        /// <param name="root">The root value.</param>
        /// <param name="jsonPath">A JSONPath string.</param>
        /// <param name="options">Options for handling duplicate paths and order of results.</param>
        /// <returns>A list of <see cref="JsonPathNode"/> representing locations-value pairs 
        /// within the root value that match the provided JSONPath expression</returns>
        /// <exception cref="JsonPathParseException">
        ///   The <paramref name="jsonPath"/> parameter is not a valid JSONPath expression.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="jsonPath"/> is <see langword="null"/>.
        /// </exception>

        public static IList<JsonPathNode> SelectNodes(JsonElement root, 
                                                      string jsonPath, 
                                                      JsonSelectorOptions? options = default)
        {
            if (jsonPath == null)
            {
                throw new ArgumentNullException(nameof(jsonPath));
            }
            var expr = JsonSelector.Parse(jsonPath);
            return expr.SelectNodes(root, options);
        }

    }

} // namespace JsonCons.JsonPath

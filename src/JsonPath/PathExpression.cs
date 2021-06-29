using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
        
namespace JsonCons.JsonPathLib
{
    interface IPathExpression 
    {
        IReadOnlyList<JsonElement> Select(JsonElement root, ResultOptions options);
    };

    public struct Node : IEquatable<Node>, IComparable<Node>
    {
        public NormalizedPath Path {get;}
        public JsonElement Value {get;}

        internal Node(NormalizedPath path, JsonElement value)
        {
            Path = path;
            Value = value;
        }

        public bool Equals(Node other)
        {
            return Path.Equals(other.Path);
        }

        public int CompareTo(Node other)
        {
            return Path.CompareTo(other.Path);
        }

        public override int GetHashCode()
        {
            return Path.GetHashCode();
        }
    };

    class ValueAccumulator : INodeAccumulator
    {
        IList<JsonElement> _values;

        internal ValueAccumulator(IList<JsonElement> values)
        {
            _values = values;
        }

        public void Accumulate(PathNode pathTail, JsonElement value)
        {
            _values.Add(value);
        }
    };

    class ValueAccumulator2 : INodeAccumulator2
    {
        IList<IJsonValue> _values;

        internal ValueAccumulator2(IList<IJsonValue> values)
        {
            _values = values;
        }

        public void Accumulate(IJsonValue value)
        {
            _values.Add(value);
        }
    };

    class NodeAccumulator : INodeAccumulator
    {
        IList<Node> _nodes;

        internal NodeAccumulator(IList<Node> nodes)
        {
            _nodes = nodes;
        }

        public void Accumulate(PathNode pathTail, JsonElement value)
        {
            _nodes.Add(new Node(new NormalizedPath(pathTail), value));
        }
    };

    public class PathExpression : IPathExpression
    {
        ISelector _selector;

        internal PathExpression(ISelector selector)
        {
            _selector = selector;
        }

        public IReadOnlyList<JsonElement> Select(JsonElement root, ResultOptions options)
        {
            PathNode pathTail = new PathNode("$");
            var values = new List<JsonElement>();

            if ((options & ResultOptions.Sort | options & ResultOptions.NoDups) != 0)
            {
                var nodes = new List<Node>();
                INodeAccumulator accumulator = new NodeAccumulator(nodes);
                _selector.Select(root, pathTail, root, accumulator, options);

                if (nodes.Count > 1)
                {
                    if ((options & ResultOptions.Sort) == ResultOptions.Sort)
                    {
                        nodes.Sort();
                    }
                    if ((options & ResultOptions.NoDups) == ResultOptions.NoDups)
                    {
                        var index = new HashSet<Node>(nodes);
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
                INodeAccumulator accumulator = new ValueAccumulator(values);            
                _selector.Select(root, pathTail, root, accumulator, options);
            }

            return values;
        }
    }

} // namespace JsonCons.JsonPathLib

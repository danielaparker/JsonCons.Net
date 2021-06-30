using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using NUnit.Framework;

namespace JsonCons.JsonPathLib
{
    public struct JsonPathNode : IEquatable<JsonPathNode>, IComparable<JsonPathNode>
    {
        public NormalizedPath Path {get;}
        public JsonElement Value {get;}

        internal JsonPathNode(NormalizedPath path, JsonElement value)
        {
            Path = path;
            Value = value;
        }

        public bool Equals(JsonPathNode other)
        {
            return Path.Equals(other.Path);
        }

        public int CompareTo(JsonPathNode other)
        {
            return Path.CompareTo(other.Path);
        }

        public override int GetHashCode()
        {
            return Path.GetHashCode();
        }
    };

    interface INodeAccumulator
    {
        void Accumulate(PathNode pathTail, JsonElement value);
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
    }

    class PathAccumulator : INodeAccumulator
    {
        IList<NormalizedPath> _values;

        internal PathAccumulator(IList<NormalizedPath> values)
        {
            _values = values;
        }

        public void Accumulate(PathNode pathTail, JsonElement value)
        {
            _values.Add(new NormalizedPath(pathTail));
        }
    }

    class NodeAccumulator : INodeAccumulator
    {
        IList<JsonPathNode> _nodes;

        internal NodeAccumulator(IList<JsonPathNode> nodes)
        {
            _nodes = nodes;
        }

        public void Accumulate(PathNode pathTail, JsonElement value)
        {
            _nodes.Add(new JsonPathNode(new NormalizedPath(pathTail), value));
        }
    }

} // namespace JsonCons.JsonPathLib

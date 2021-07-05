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
        void AddNode(PathNode pathStem, JsonElement value);
    };

    class JsonElementAccumulator : INodeAccumulator
    {
        IList<JsonElement> _values;

        internal JsonElementAccumulator(IList<JsonElement> values)
        {
            _values = values;
        }

        public void AddNode(PathNode pathStem, JsonElement value)
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

        public void AddNode(PathNode pathStem, JsonElement value)
        {
            _values.Add(new NormalizedPath(pathStem));
        }
    }

    class NodeAccumulator : INodeAccumulator
    {
        IList<JsonPathNode> _nodes;

        internal NodeAccumulator(IList<JsonPathNode> nodes)
        {
            _nodes = nodes;
        }

        public void AddNode(PathNode pathStem, JsonElement value)
        {
            _nodes.Add(new JsonPathNode(new NormalizedPath(pathStem), value));
        }
    }

} // namespace JsonCons.JsonPathLib

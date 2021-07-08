using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace JsonCons.JsonPathLib
{
    public readonly struct JsonPathNode : IEquatable<JsonPathNode>, IComparable<JsonPathNode>
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
        void AddNode(PathNode pathStem, IJsonValue value);
    };

    sealed class JsonElementAccumulator : INodeAccumulator
    {
        IList<JsonElement> _values;

        internal JsonElementAccumulator(IList<JsonElement> values)
        {
            _values = values;
        }

        public void AddNode(PathNode pathStem, IJsonValue value)
        {
            _values.Add(value.GetJsonElement());
        }
    }

    sealed class ValueAccumulator : INodeAccumulator
    {
        IList<IJsonValue> _values;

        internal ValueAccumulator(IList<IJsonValue> values)
        {
            _values = values;
        }

        public void AddNode(PathNode pathStem, IJsonValue value)
        {
            _values.Add(value);
        }
    }

    sealed class PathAccumulator : INodeAccumulator
    {
        IList<NormalizedPath> _values;

        internal PathAccumulator(IList<NormalizedPath> values)
        {
            _values = values;
        }

        public void AddNode(PathNode pathStem, IJsonValue value)
        {
            _values.Add(new NormalizedPath(pathStem));
        }
    }

    sealed class NodeAccumulator : INodeAccumulator
    {
        IList<JsonPathNode> _nodes;

        internal NodeAccumulator(IList<JsonPathNode> nodes)
        {
            _nodes = nodes;
        }

        public void AddNode(PathNode pathStem, IJsonValue value)
        {
            _nodes.Add(new JsonPathNode(new NormalizedPath(pathStem), value.GetJsonElement()));
        }
    }

} // namespace JsonCons.JsonPathLib

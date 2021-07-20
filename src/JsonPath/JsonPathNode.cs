using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace JsonCons.JsonPathLib
{

    /// <summary>
    /// Represents a specific location-value pair within a root JSON value.
    ///
    /// </summary>

    public readonly struct JsonPathNode : IEquatable<JsonPathNode>, IComparable<JsonPathNode>
    {
        /// <summary>
        /// Gets the location of this value within a root JSON value.
        ///
        /// </summary>
        public NormalizedPath Path {get;}
        /// <summary>
        /// Gets the value
        ///
        /// </summary>
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
        void AddNode(PathLink last, IValue value);
    };

    sealed class JsonElementAccumulator : INodeAccumulator
    {
        IList<JsonElement> _values;

        internal JsonElementAccumulator(IList<JsonElement> values)
        {
            _values = values;
        }

        public void AddNode(PathLink last, IValue value)
        {
            _values.Add(value.GetJsonElement());
        }
    }

    sealed class ValueAccumulator : INodeAccumulator
    {
        IList<IValue> _values;

        internal ValueAccumulator(IList<IValue> values)
        {
            _values = values;
        }

        public void AddNode(PathLink last, IValue value)
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

        public void AddNode(PathLink last, IValue value)
        {
            _values.Add(new NormalizedPath(last));
        }
    }

    sealed class NodeAccumulator : INodeAccumulator
    {
        IList<JsonPathNode> _nodes;

        internal NodeAccumulator(IList<JsonPathNode> nodes)
        {
            _nodes = nodes;
        }

        public void AddNode(PathLink last, IValue value)
        {
            _nodes.Add(new JsonPathNode(new NormalizedPath(last), value.GetJsonElement()));
        }
    }

} // namespace JsonCons.JsonPathLib

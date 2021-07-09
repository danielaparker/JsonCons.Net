using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace JsonCons.JsonPathLib
{
    public readonly struct JsonPathComponent : IEquatable<JsonPathComponent>, IComparable<JsonPathComponent>
    {
        public NormalizedPath Path {get;}
        public JsonElement Value {get;}

        internal JsonPathComponent(NormalizedPath path, JsonElement value)
        {
            Path = path;
            Value = value;
        }

        public bool Equals(JsonPathComponent other)
        {
            return Path.Equals(other.Path);
        }

        public int CompareTo(JsonPathComponent other)
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
        void AddNode(PathComponent pathStem, IValue value);
    };

    sealed class JsonElementAccumulator : INodeAccumulator
    {
        IList<JsonElement> _values;

        internal JsonElementAccumulator(IList<JsonElement> values)
        {
            _values = values;
        }

        public void AddNode(PathComponent pathStem, IValue value)
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

        public void AddNode(PathComponent pathStem, IValue value)
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

        public void AddNode(PathComponent pathStem, IValue value)
        {
            _values.Add(new NormalizedPath(pathStem));
        }
    }

    sealed class NodeAccumulator : INodeAccumulator
    {
        IList<JsonPathComponent> _nodes;

        internal NodeAccumulator(IList<JsonPathComponent> nodes)
        {
            _nodes = nodes;
        }

        public void AddNode(PathComponent pathStem, IValue value)
        {
            _nodes.Add(new JsonPathComponent(new NormalizedPath(pathStem), value.GetJsonElement()));
        }
    }

} // namespace JsonCons.JsonPathLib

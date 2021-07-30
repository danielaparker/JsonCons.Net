using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Runtime.CompilerServices;

namespace JsonCons.JsonPath
{

    /// <summary>
    /// Represents a specific location-value pair within a root JSON value.
    ///
    /// </summary>

    public readonly struct PathValuePair : IEquatable<PathValuePair>, IComparable<PathValuePair>
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

        internal PathValuePair(NormalizedPath path, JsonElement value)
        {
            Path = path;
            Value = value;
        }
        /// <summary>
        /// Determines whether this instance and another specified PathValuePair object have the same value.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(PathValuePair other)
        {
            return Path.Equals(other.Path);
        }
        /// <summary>
        /// Compares this instance with a specified PathValuePair object and indicates 
        /// whether this instance precedes, follows, or appears in the same position 
        /// in the sort order as the specified PathValuePair.
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if the value of the other PathValuePair object is the same as the value of 
        /// this instance; otherwise, false. If other is null, the method returns false.</returns>
        public int CompareTo(PathValuePair other)
        {
            return Path.CompareTo(other.Path);
        }
        /// <summary>
        /// Returns the hash code for this PathValuePair.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return Path.GetHashCode();
        }
    };

    interface INodeAccumulator
    {
        void Add(NormalizedPathNode last, IValue value);
    };

    sealed class SynchronizedNodeAccumulator : INodeAccumulator
    {
        INodeAccumulator _accumulator;

        internal SynchronizedNodeAccumulator(INodeAccumulator accumulator)
        {
            _accumulator = accumulator;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Add(NormalizedPathNode last, IValue value)
        {
            _accumulator.Add(last, value);
        }
    }

    sealed class JsonElementAccumulator : INodeAccumulator
    {
        IList<JsonElement> _values;

        internal JsonElementAccumulator(IList<JsonElement> values)
        {
            _values = values;
        }

        public void Add(NormalizedPathNode last, IValue value)
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

        public void Add(NormalizedPathNode last, IValue value)
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

        public void Add(NormalizedPathNode last, IValue value)
        {
            _values.Add(new NormalizedPath(last));
        }
    }

    sealed class NodeAccumulator : INodeAccumulator
    {
        IList<PathValuePair> _nodes;

        internal NodeAccumulator(IList<PathValuePair> nodes)
        {
            _nodes = nodes;
        }

        public void Add(NormalizedPathNode last, IValue value)
        {
            _nodes.Add(new PathValuePair(new NormalizedPath(last), value.GetJsonElement()));
        }
    }

} // namespace JsonCons.JsonPath

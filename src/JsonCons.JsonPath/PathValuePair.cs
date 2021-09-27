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
        public JsonLocation Path {get;}
        /// <summary>
        /// Gets the value
        ///
        /// </summary>
        public JsonElement Value {get;}

        internal PathValuePair(JsonLocation path, JsonElement value)
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

    interface INodeReceiver
    {
        void Add(JsonLocationNode lastNode, IValue value);
    };

    sealed class SynchronizedNodeReceiver : INodeReceiver
    {
        INodeReceiver _accumulator;

        internal SynchronizedNodeReceiver(INodeReceiver accumulator)
        {
            _accumulator = accumulator;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Add(JsonLocationNode lastNode, IValue value)
        {
            _accumulator.Add(lastNode, value);
        }
    }

    sealed class JsonElementReceiver : INodeReceiver
    {
        IList<JsonElement> _values;

        internal JsonElementReceiver(IList<JsonElement> values)
        {
            _values = values;
        }

        public void Add(JsonLocationNode lastNode, IValue value)
        {
            _values.Add(value.GetJsonElement());
        }
    }

    sealed class ValueReceiver : INodeReceiver
    {
        IList<IValue> _values;

        internal ValueReceiver(IList<IValue> values)
        {
            _values = values;
        }

        public void Add(JsonLocationNode lastNode, IValue value)
        {
            _values.Add(value);
        }
    }

    sealed class PathReceiver : INodeReceiver
    {
        IList<JsonLocation> _values;

        internal PathReceiver(IList<JsonLocation> values)
        {
            _values = values;
        }

        public void Add(JsonLocationNode lastNode, IValue value)
        {
            _values.Add(new JsonLocation(lastNode));
        }
    }

    sealed class NodeReceiver : INodeReceiver
    {
        IList<PathValuePair> _nodes;

        internal NodeReceiver(IList<PathValuePair> nodes)
        {
            _nodes = nodes;
        }

        public void Add(JsonLocationNode lastNode, IValue value)
        {
            _nodes.Add(new PathValuePair(new JsonLocation(lastNode), value.GetJsonElement()));
        }
    }

} // namespace JsonCons.JsonPath

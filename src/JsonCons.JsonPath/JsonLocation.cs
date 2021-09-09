using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace JsonCons.JsonPath
{

    /// <summary>
    /// Specifies the type of a path node.
    ///
    /// </summary>

    public enum JsonLocationNodeKind 
    {
        /// <summary>
        /// Indicates the root path node
        /// </summary>
        Root,
        /// <summary>
        /// Indicates a path node that matches on property names.
        /// </summary>
        Name,
        /// <summary>
        /// Indicates a path node that matches on array indices.
        /// </summary>
        Index
    };

    /// <summary>
    /// Represents a node of a <see cref="JsonLocation"/>.
    ///
    /// </summary>
    public sealed class JsonLocationNode
    {

        /// <summary>
        /// Gets the parent of this path node.
        ///
        /// </summary>
        public JsonLocationNode? Parent {get;}

        /// <summary>
        /// Gets the type of this path node.
        ///
        /// </summary>
        public JsonLocationNodeKind ComponentKind {get;}

        private readonly string _name;
        private readonly Int32 _index;

        /// <summary>
        /// Gets an instance of <see cref="JsonLocationNode"/> that represents the root value ($) 
        ///
        /// </summary>
        public static JsonLocationNode Root {get;} = new JsonLocationNode(JsonLocationNodeKind.Root, "$");

        /// <summary>
        /// Gets an instance of <see cref="JsonLocationNode"/> that represents the current node (@)
        ///
        /// </summary>
        public static JsonLocationNode Current { get;} = new JsonLocationNode(JsonLocationNodeKind.Root, "@");

        JsonLocationNode(JsonLocationNodeKind componentKind, string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            Parent = null;
            ComponentKind = componentKind;
            _name = name;
            _index = 0;
        }

        /// <summary>
        /// Constructs a path node from a parent and name
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The name.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="parent"/> is <see langword="null"/>.
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>

        public JsonLocationNode(JsonLocationNode parent, string name)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            Parent = parent;
            ComponentKind = JsonLocationNodeKind.Name;
            _name = name;
            _index = 0;
        }

        /// <summary>
        /// Constructs a path node from a parent and an index
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="index">The index.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="parent"/> is <see langword="null"/>.
        /// </exception>

        public JsonLocationNode(JsonLocationNode parent, Int32 index)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }
            Parent = parent;
            ComponentKind = JsonLocationNodeKind.Index;
            _name = "";
            _index = index;
        }

        /// <summary>
        /// Gets the value of this <see cref="JsonLocationNode"/> as a name.
        ///
        /// </summary>
        public string GetName()
        {
            return _name;
        }

        /// <summary>
        /// Gets the value of this <see cref="JsonLocationNode"/> as an index.
        ///
        /// </summary>
        public Int32 GetIndex()
        {
            return _index;
        }
        /// <summary>
        /// Compares this instance with a specified <see cref="JsonLocationNode"/> object and indicates 
        /// whether this instance precedes, follows, or appears in the same 
        /// position in the sort order as the specified <see cref="JsonLocationNode"/>.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(JsonLocationNode other)
        {
            if (other == null)
            {
               return 1;
            }
            int diff = 0;
            if (ComponentKind != other.ComponentKind)
            {
                diff = ComponentKind - other.ComponentKind;
            }
            else
            {
                switch (ComponentKind)
                {
                    case JsonLocationNodeKind.Root:
                        diff = string.Compare(_name, other._name);
                        break;
                    case JsonLocationNodeKind.Index:
                        diff = _index - other._index;
                        break;
                    case JsonLocationNodeKind.Name:
                        diff = string.Compare(_name, other._name);
                        break;
                }
            }
            return diff;
        }
        /// <summary>
        /// Returns the hash code for this <see cref="JsonLocationNode"/>.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            int hashCode = ComponentKind == JsonLocationNodeKind.Index ? _index.GetHashCode() : _name.GetHashCode();

            return hashCode;
        }
    };

    /// <summary>
    /// Represents the location of a specific JSON value within a root JSON value.
    ///
    /// </summary>

    public sealed class JsonLocation : IEquatable<JsonLocation>, IComparable<JsonLocation>, IEnumerable<JsonLocationNode>
    {
        readonly IReadOnlyList<JsonLocationNode> _components;

        /// <summary>
        /// Constructs a normalized path from the last location node.
        ///
        /// </summary>
        public JsonLocation(JsonLocationNode lastNode)
        {
            var nodes = new List<JsonLocationNode>();
            JsonLocationNode? node = lastNode;
            do
            {
                nodes.Add(node);
                node = node.Parent;
            }
            while (node != null);
            
            nodes.Reverse();

            _components = nodes;
        }

        /// <summary>
        /// Gets the last node of the <see cref="JsonLocation"/>. 
        ///
        /// </summary>

        public JsonLocationNode Last { get { return _components[_components.Count - 1]; } }

        /// <summary>
        /// Returns an enumerator that iterates through the components of the normalized path. 
        ///
        /// </summary>

        public IEnumerator<JsonLocationNode> GetEnumerator()
        {
            return _components.GetEnumerator();
        }


        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
           return (System.Collections.IEnumerator) GetEnumerator();
        }

        /// <summary>
        /// Gets a string representation for the normalized path. 
        /// The string will have the form $['aName']['anotherName'][anIndex]
        /// with any single quote characters appearing in names escaped with a backslash. 
        ///
        /// </summary>

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();

            foreach (var item in _components)
            {
                switch (item.ComponentKind)
                {
                    case JsonLocationNodeKind.Root:
                        buffer.Append(item.GetName());
                        break;
                    case JsonLocationNodeKind.Name:
                        buffer.Append('[');
                        buffer.Append('\'');
                        if (item.GetName().Contains('\''))
                        {
                            buffer.Append(item.GetName().Replace(@"'",@"\'"));
                        }
                        else
                        {
                            buffer.Append(item.GetName());
                        }
                        buffer.Append('\'');
                        buffer.Append(']');
                        break;
                    case JsonLocationNodeKind.Index:
                        buffer.Append('[');
                        buffer.Append(item.GetIndex().ToString());
                        buffer.Append(']');
                        break;
                }
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Gets a <see href="https://datatracker.ietf.org/doc/html/rfc6901">JSON Pointer</see> representation for the normalized path. 
        /// The string will have the form /aName/anotherName/anIndex with any '~' and '/' characters appearing in names escaped as per the 
        /// specification.
        ///
        /// </summary>

        public string ToJsonPointer()
        {
            StringBuilder buffer = new StringBuilder();

            foreach (var node in _components)
            {
                switch (node.ComponentKind)
                {
                    case JsonLocationNodeKind.Root:
                    {
                        break;
                    }
                    case JsonLocationNodeKind.Name:
                    {
                        buffer.Append('/');
                        foreach (var c in node.GetName())
                        {
                            switch (c)
                            {
                                case '~':
                                    buffer.Append('~');
                                    buffer.Append('0');
                                    break;
                                case '/':
                                    buffer.Append('~');
                                    buffer.Append('1');
                                    break;
                                default:
                                    buffer.Append(c);
                                    break;
                            }
                        }
                        break;
                    }
                    case JsonLocationNodeKind.Index:
                    {
                        buffer.Append('/');
                        buffer.Append(node.GetIndex().ToString());
                        break;
                    }
                }
            }
            return buffer.ToString();
        }
        /// <summary>
        /// Determines whether this instance and another specified <see cref="JsonLocation"/> object have the same value.
        /// </summary>
        /// <param name="other">The <see cref="JsonLocation"/> to compare to this instance.</param>
        /// <returns>true if the value of other is the same as the value of this instance; otherwise, false. If other is null, the method returns false.</returns>
        public bool Equals(JsonLocation other)
        {
            if (other == null)
            {
               return false;
            }

            return CompareTo(other) == 0;
        }
        /// <summary>
        /// Determines whether this instance and a specified object, which must also be a <see cref="JsonLocation"/> object, have the same value.
        /// </summary>
        /// <param name="other">The <see cref="JsonLocation"/> to compare to this instance.</param>
        /// <returns>true if other is a <see cref="JsonLocation"/> and its value is the same as this instance; otherwise, false. If other is null, the method returns false.</returns>
        public override bool Equals(Object? other)
        {
            if (other == null)
            {
               return false;
            }

            return Equals(other as JsonLocationNode);
        }
        /// <summary>
        /// Compares this instance with a specified <see cref="JsonLocation"/> object and indicates 
        /// whether this instance precedes, follows, or appears in the same 
        /// position in the sort order as the specified <see cref="JsonLocation"/>.
        /// </summary>
        /// <param name="other">The <see cref="JsonLocation"/> to compare with this instance.</param>
        /// <returns>A 32-bit signed integer that indicates whether this instance precedes, 
        /// follows, or appears in the same position in the sort order as other.</returns>
        public int CompareTo(JsonLocation other)
        {
            int i = 0;

            while (i < _components.Count && i < other._components.Count)
            {
                int diff = _components[i].CompareTo(other._components[i]);
                if (diff != 0)
                {
                    return diff;
                }
                ++i;
            }
            return _components.Count - other._components.Count;
        }
        /// <summary>
        /// Returns the hash code for this <see cref="JsonLocation"/>.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            int hash = _components[0].GetHashCode();
            for (int i = 1; i < _components.Count; ++i)
            {
                hash += 17*_components[i].GetHashCode();
            }

            return hash;
        }

        /// <summary>
        ///   Looks for a value within the root value that matches this normalized path, returning
        ///   <see langword="true"/> if such a value exists, <see langword="false"/> otherwise. When the value exists <paramref name="element"/>
        ///   is assigned that value.
        /// </summary>
        /// <param name="root">The root value.</param>
        /// <param name="element">Receives the value.</param>
        /// <returns>
        ///   <see langword="true"/> if the value was found, <see langword="false"/> otherwise.
        /// </returns>

        public bool TryGetValue(JsonElement root, out JsonElement element)
        {
            element = root;
            foreach (var node in _components)
            {
                if (node.ComponentKind == JsonLocationNodeKind.Index)
                {
                    if (element.ValueKind != JsonValueKind.Array || node.GetIndex() >= element.GetArrayLength())
                    {
                        return false; 
                    }
                    element = element[node.GetIndex()];
                }
                else if (node.ComponentKind == JsonLocationNodeKind.Name)
                {
                    if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(node.GetName(), out element))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        ///   Looks for a value within the root value that matches this normalized path, returning
        ///   <see langword="true"/> if such a value exists, <see langword="false"/> otherwise. 
        /// </summary>
        /// <param name="root">The root value.</param>
        /// <returns>
        ///   <see langword="true"/> if the value was found, <see langword="false"/> otherwise.
        /// </returns>

        public bool ContainsValue(JsonElement root)
        {
            JsonElement element = root;
            foreach (var node in _components)
            {
                if (node.ComponentKind == JsonLocationNodeKind.Index)
                {
                    if (element.ValueKind != JsonValueKind.Array || node.GetIndex() >= element.GetArrayLength())
                    {
                        return false; 
                    }
                    element = element[node.GetIndex()];
                }
                else if (node.ComponentKind == JsonLocationNodeKind.Name)
                {
                    if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(node.GetName(), out element))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

    }

} // namespace JsonCons.JsonPath

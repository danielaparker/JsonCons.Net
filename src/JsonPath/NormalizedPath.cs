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

    public enum PathNodeKind 
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
    /// Represents a node of a <see cref="NormalizedPath"/>.
    ///
    /// </summary>
    public sealed class PathNode
    {

        /// <summary>
        /// Gets the parent of this path node.
        ///
        /// </summary>
        public PathNode Parent {get;}

        /// <summary>
        /// Gets the type of this path node.
        ///
        /// </summary>
        public PathNodeKind ComponentKind {get;}

        private readonly string _name;
        private readonly Int32 _index;

        /// <summary>
        /// Gets a root node 
        ///
        /// </summary>
        public static PathNode Root {get;} = new PathNode(PathNodeKind.Root, "$");

        /// <summary>
        /// Gets a current node 
        ///
        /// </summary>
        public static PathNode Current { get;} = new PathNode(PathNodeKind.Root, "@");

        PathNode(PathNodeKind componentKind, string name)
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

        public PathNode(PathNode parent, string name)
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
            ComponentKind = PathNodeKind.Name;
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

        public PathNode(PathNode parent, Int32 index)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }
            Parent = parent;
            ComponentKind = PathNodeKind.Index;
            _name = null;
            _index = index;
        }

        /// <summary>
        /// Gets the value of this path node as a name.
        ///
        /// </summary>
        public string GetName()
        {
            return _name;
        }

        /// <summary>
        /// Gets the value of this path node as an index.
        ///
        /// </summary>
        public Int32 GetIndex()
        {
            return _index;
        }

        public int CompareTo(PathNode other)
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
                    case PathNodeKind.Root:
                        diff = string.Compare(_name, other._name);
                        break;
                    case PathNodeKind.Index:
                        diff = _index - other._index;
                        break;
                    case PathNodeKind.Name:
                        diff = string.Compare(_name, other._name);
                        break;
                }
            }
            return diff;
        }

        public override int GetHashCode()
        {
            int hashCode = ComponentKind == PathNodeKind.Index ? _index.GetHashCode() : _name.GetHashCode();

            return hashCode;
        }
    };

    /// <summary>
    /// Represents the location of a specific JSON value within a root JSON value.
    ///
    /// </summary>

    public sealed class NormalizedPath : IEquatable<NormalizedPath>, IComparable<NormalizedPath>, IEnumerable<PathNode>
    {
        readonly IReadOnlyList<PathNode> _components;

        /// <summary>
        /// Constructs a normalized path from the last path node.
        ///
        /// </summary>
        public NormalizedPath(PathNode last)
        {
            var nodes = new List<PathNode>();
            PathNode node = last;
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
        /// Gets the last node of the normalized path. 
        ///
        /// </summary>

        public PathNode Last { get { return _components[_components.Count - 1]; } }

        /// <summary>
        /// Returns an enumerator that iterates through the components of the normalized path. 
        ///
        /// </summary>

        public IEnumerator<PathNode> GetEnumerator()
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
                    case PathNodeKind.Root:
                        buffer.Append(item.GetName());
                        break;
                    case PathNodeKind.Name:
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
                    case PathNodeKind.Index:
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
                    case PathNodeKind.Root:
                    {
                        break;
                    }
                    case PathNodeKind.Name:
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
                    case PathNodeKind.Index:
                    {
                        buffer.Append('/');
                        buffer.Append(node.GetIndex().ToString());
                        break;
                    }
                }
            }
            return buffer.ToString();
        }

        public bool Equals(NormalizedPath other)
        {
            if (other == null)
            {
               return false;
            }

            return CompareTo(other) == 0;
        }

        public override bool Equals(Object other)
        {
            if (other == null)
            {
               return false;
            }

            return Equals(other as PathNode);
        }

        public int CompareTo(NormalizedPath other)
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

        public bool TryGet(JsonElement root, out JsonElement element)
        {
            element = root;
            foreach (var node in _components)
            {
                if (node.ComponentKind == PathNodeKind.Index)
                {
                    if (element.ValueKind != JsonValueKind.Array || node.GetIndex() >= element.GetArrayLength())
                    {
                        return false; 
                    }
                    element = element[node.GetIndex()];
                }
                else if (node.ComponentKind == PathNodeKind.Name)
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

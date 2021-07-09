using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace JsonCons.JsonPathLib
{

    /// <summary>
    /// Specifies the type of a path component.
    ///
    /// </summary>

    public enum PathComponentKind 
    {
        /// <summary>
        /// Indicates the root path component
        /// </summary>
        Root,
        /// <summary>
        /// Indicates a path component that matches on property names.
        /// </summary>
        Name,
        /// <summary>
        /// Indicates a path component that matches on array indices.
        /// </summary>
        Index
    };

    /// <summary>
    /// Represents a component of a <see cref="NormalizedPath"/>.
    ///
    /// </summary>
    public sealed class PathComponent
    {

        /// <summary>
        /// Gets the parent of this component.
        ///
        /// </summary>
        public PathComponent Parent {get;}

        /// <summary>
        /// Gets the type of the component.
        ///
        /// </summary>
        public PathComponentKind ComponentKind {get;}

        private readonly string _name;
        private readonly Int32 _index;

        public PathComponent(string name)
        {
            Parent = null;
            ComponentKind = PathComponentKind.Root;
            _name = name;
            _index = 0;
        }

        public PathComponent(PathComponent parent, string name)
        {
            Parent = parent;
            ComponentKind = PathComponentKind.Name;
            _name = name;
            _index = 0;
        }

        public PathComponent(PathComponent parent, Int32 index)
        {
            Parent = parent;
            ComponentKind = PathComponentKind.Index;
            _name = null;
            _index = index;
        }

        /// <summary>
        /// Gets the value of the component as a name.
        ///
        /// </summary>
        public string GetName()
        {
            return _name;
        }

        /// <summary>
        /// Gets the value of the component as an index.
        ///
        /// </summary>
        public Int32 GetIndex()
        {
            return _index;
        }

        public int CompareTo(PathComponent other)
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
                    case PathComponentKind.Root:
                        diff = string.Compare(_name, other._name);
                        break;
                    case PathComponentKind.Index:
                        diff = _index - other._index;
                        break;
                    case PathComponentKind.Name:
                        diff = string.Compare(_name, other._name);
                        break;
                }
            }
            return diff;
        }

        public override int GetHashCode()
        {
            int hashCode = ComponentKind == PathComponentKind.Index ? _index.GetHashCode() : _name.GetHashCode();

            return hashCode;
        }
    };

    /// <summary>
    /// Represents the location of a specific JSON value within a root JSON value.
    ///
    /// </summary>

    public sealed class NormalizedPath : IEquatable<NormalizedPath>, IComparable<NormalizedPath>, IEnumerable<PathComponent>
    {
        readonly IReadOnlyList<PathComponent> _components;

        /// <summary>
        /// Constructs a normalized path from the last path component.
        ///
        /// </summary>
        public NormalizedPath(PathComponent last)
        {
            var nodes = new List<PathComponent>();
            PathComponent component = last;
            do
            {
                nodes.Add(component);
                component = component.Parent;
            }
            while (component != null);
            
            nodes.Reverse();

            _components = nodes;
        }

        /// <summary>
        /// Gets the last component of the normalized path. 
        ///
        /// </summary>

        public PathComponent Last { get { return _components[_components.Count - 1]; } }

        /// <summary>
        /// Returns an enumerator that iterates through the components of the normalized path. 
        ///
        /// </summary>

        public IEnumerator<PathComponent> GetEnumerator()
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
                    case PathComponentKind.Root:
                        buffer.Append(item.GetName());
                        break;
                    case PathComponentKind.Name:
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
                    case PathComponentKind.Index:
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
                    case PathComponentKind.Root:
                    {
                        break;
                    }
                    case PathComponentKind.Name:
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
                    case PathComponentKind.Index:
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

            return Equals(other as PathComponent);
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

        public bool TryGet(JsonElement root, out JsonElement element)
        {
            element = root;
            foreach (var component in _components)
            {
                if (component.ComponentKind == PathComponentKind.Index)
                {
                    if (element.ValueKind != JsonValueKind.Array || component.GetIndex() >= element.GetArrayLength())
                    {
                        return false; 
                    }
                    element = element[component.GetIndex()];
                }
                else if (component.ComponentKind == PathComponentKind.Name)
                {
                    if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(component.GetName(), out element))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    };

} // namespace JsonCons.JsonPathLib

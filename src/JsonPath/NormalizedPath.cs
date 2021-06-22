using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace JsonCons.JsonPathLib
{
    public class PathNode
    {
        enum PathNodeKind {Root,Identifier,Index};

        public PathNode Parent {get;}

        private readonly PathNodeKind _nodeKind;
        private readonly string _identifier;
        private readonly Int32 _index;

        public PathNode(string identifier)
        {
            Parent = null;
            _nodeKind = PathNodeKind.Root;
            _identifier = identifier;
            _index = 0;
        }

        public PathNode(PathNode parent, string identifier)
        {
            Parent = parent;
            _nodeKind = PathNodeKind.Identifier;
            _identifier = identifier;
            _index = 0;
        }

        public PathNode(PathNode parent, Int32 index)
        {
            Parent = parent;
            _nodeKind = PathNodeKind.Index;
            _identifier = null;
            _index = index;
        }

        public int CompareTo(PathNode other)
        {
            if (other == null)
            {
               return 1;
            }
            int diff = 0;
            if (_nodeKind != other._nodeKind)
            {
                diff = _nodeKind - other._nodeKind;
            }
            else
            {
                switch (_nodeKind)
                {
                    case PathNodeKind.Root:
                        diff = string.Compare(_identifier, other._identifier);
                        break;
                    case PathNodeKind.Index:
                        diff = _index - other._index;
                        break;
                    case PathNodeKind.Identifier:
                        diff = string.Compare(_identifier, other._identifier);
                        break;
                }
            }
            return diff;
        }

        public override int GetHashCode()
        {
            int hashCode = _nodeKind == PathNodeKind.Index ? _index.GetHashCode() : _identifier.GetHashCode();

            return hashCode;
        }

        internal void ToStringBuilder(StringBuilder buffer) 
        {
            switch (_nodeKind)
            {
                case PathNodeKind.Root:
                    buffer.Append(_identifier);
                    break;
                case PathNodeKind.Identifier:
                    buffer.Append('[');
                    buffer.Append('\'');
                    buffer.Append(_identifier);
                    buffer.Append('\'');
                    buffer.Append(']');
                    break;
                case PathNodeKind.Index:
                    buffer.Append('[');
                    buffer.Append(_index.ToString());
                    buffer.Append(']');
                    break;
            }
        }
    };

    public class NormalizedPath : IEquatable<NormalizedPath>, IComparable<NormalizedPath>
    {
        PathNode[] _nodes;

        public NormalizedPath(PathNode node)
        {
            var nodes = new List<PathNode>();
            PathNode p = node;
            do
            {
                nodes.Add(p);
                p = p.Parent;
            }
            while (p != null);
            
            nodes.Reverse();

            _nodes = nodes.ToArray();
        }

        PathNode Head()
        {
            return _nodes[0];
        }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();

            for (int i = 0; i < _nodes.Length; ++i)
            {
                _nodes[i].ToStringBuilder(buffer);
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

            while (i < _nodes.Length && i < other._nodes.Length)
            {
                int diff = _nodes[i].CompareTo(other._nodes[i]);
                if (diff != 0)
                {
                    return diff;
                }
                ++i;
            }
            return _nodes.Length - other._nodes.Length;
        }

        public override int GetHashCode()
        {
            int hash = _nodes[0].GetHashCode();
            for (int i = 1; i < _nodes.Length; ++i)
            {
                hash += 17*_nodes[i].GetHashCode();
            }

            return hash;
        }
    };

} // namespace JsonCons.JsonPathLib

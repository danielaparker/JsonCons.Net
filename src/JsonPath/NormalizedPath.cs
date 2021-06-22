using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace JsonCons.JsonPathLib
{
    public enum PathNodeKind {Root,Name,Index};

    public class PathNode
    {
        public PathNode Parent {get;}

        private readonly PathNodeKind _nodeKind;
        private readonly string _name;
        private readonly Int32 _index;

        public PathNode(string name)
        {
            Parent = null;
            _nodeKind = PathNodeKind.Root;
            _name = name;
            _index = 0;
        }

        public PathNode(PathNode parent, string name)
        {
            Parent = parent;
            _nodeKind = PathNodeKind.Name;
            _name = name;
            _index = 0;
        }

        public PathNode(PathNode parent, Int32 index)
        {
            Parent = parent;
            _nodeKind = PathNodeKind.Index;
            _name = null;
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
            int hashCode = _nodeKind == PathNodeKind.Index ? _index.GetHashCode() : _name.GetHashCode();

            return hashCode;
        }

        internal void ToStringBuilder(StringBuilder buffer) 
        {
            switch (_nodeKind)
            {
                case PathNodeKind.Root:
                    buffer.Append(_name);
                    break;
                case PathNodeKind.Name:
                    buffer.Append('[');
                    buffer.Append('\'');
                    buffer.Append(_name);
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
        IReadOnlyList<PathNode> _nodes;

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

            _nodes = nodes;
        }

        public PathNode Root { get {return _nodes[0]; } }

        public PathNode Stem { get { return _nodes[_nodes.Count - 1]; } }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();

            for (int i = 0; i < _nodes.Count; ++i)
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

            while (i < _nodes.Count && i < other._nodes.Count)
            {
                int diff = _nodes[i].CompareTo(other._nodes[i]);
                if (diff != 0)
                {
                    return diff;
                }
                ++i;
            }
            return _nodes.Count - other._nodes.Count;
        }

        public override int GetHashCode()
        {
            int hash = _nodes[0].GetHashCode();
            for (int i = 1; i < _nodes.Count; ++i)
            {
                hash += 17*_nodes[i].GetHashCode();
            }

            return hash;
        }
    };

} // namespace JsonCons.JsonPathLib

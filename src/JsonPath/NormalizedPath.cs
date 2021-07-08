using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace JsonCons.JsonPathLib
{
    public enum PathNodeKind {Root,Name,Index};

    public sealed class PathNode
    {
        public PathNode Parent {get;}

        public PathNodeKind NodeKind {get;}

        private readonly string _name;
        private readonly Int32 _index;

        public PathNode(string name)
        {
            Parent = null;
            NodeKind = PathNodeKind.Root;
            _name = name;
            _index = 0;
        }

        public PathNode(PathNode parent, string name)
        {
            Parent = parent;
            NodeKind = PathNodeKind.Name;
            _name = name;
            _index = 0;
        }

        public PathNode(PathNode parent, Int32 index)
        {
            Parent = parent;
            NodeKind = PathNodeKind.Index;
            _name = null;
            _index = index;
        }

        public string GetName()
        {
            return _name;
        }

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
            if (NodeKind != other.NodeKind)
            {
                diff = NodeKind - other.NodeKind;
            }
            else
            {
                switch (NodeKind)
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
            int hashCode = NodeKind == PathNodeKind.Index ? _index.GetHashCode() : _name.GetHashCode();

            return hashCode;
        }
    };

    public sealed class NormalizedPath : IEquatable<NormalizedPath>, IComparable<NormalizedPath>, IEnumerable<PathNode>
    {
        readonly IReadOnlyList<PathNode> _nodes;

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

        public IEnumerator<PathNode> GetEnumerator()
        {
            return _nodes.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
           return (System.Collections.IEnumerator) GetEnumerator();
        }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();

            foreach (var item in _nodes)
            {
                switch (item.NodeKind)
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

        public string ToJsonPointer()
        {
            StringBuilder buffer = new StringBuilder();

            foreach (var node in _nodes)
            {
                switch (node.NodeKind)
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

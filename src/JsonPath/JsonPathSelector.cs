using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using NUnit.Framework;

namespace JsonCons.JsonPathLib
{

    public enum ResultOptions {NoDups=1, Sort=2, Path=4};

    interface ISelector 
    {
        void Select(PathNode pathNode,
                    JsonElement root,
                    JsonElement current, 
                    IList<JsonElement> nodes,
                    ResultOptions options);

        void AppendSelector(ISelector tail);
    };

    abstract class BaseSelector : ISelector 
    {
        ISelector Tail {get;set;} = null;

        public abstract void Select(PathNode pathNode,
                                    JsonElement root, 
                                    JsonElement current,
                                    IList<JsonElement> nodes,
                                    ResultOptions options);

        public void AppendSelector(ISelector tail)
        {
            if (Tail == null)
            {
                Tail = tail;
            }
            else
            {
                Tail.AppendSelector(tail);
            }
        }

        protected void EvaluateTail(PathNode pathNode,
                                    JsonElement root, 
                                    JsonElement current,
                                    IList<JsonElement> nodes,
                                    ResultOptions options)
        {
            if (Tail == null)
            {
                nodes.Add(current);
            }
            else
            {
                Tail.Select(pathNode, root, current, nodes, options);
            }
        }
    }

    class RootSelector : BaseSelector
    {
        Int32 _selector_id;

        internal RootSelector(Int32 selector_id)
        {
            _selector_id = selector_id;
        }

        public override void Select(PathNode pathNode,
                                    JsonElement root, 
                                    JsonElement current,
                                    IList<JsonElement> nodes,
                                    ResultOptions options)
        {
            TestContext.WriteLine("RootSelector...");
            this.EvaluateTail(pathNode, root, root, nodes, options);        
        }
    }

    class CurrentNodeSelector : BaseSelector
    {
        public override void Select(PathNode pathNode,
                                    JsonElement root, 
                                    JsonElement current,
                                    IList<JsonElement> nodes,
                                    ResultOptions options)
        {
            this.EvaluateTail(pathNode, root, current, nodes, options);        
        }
    }

    class IdentifierSelector : BaseSelector
    {
        string _identifier;

        public IdentifierSelector(string identifier)
        {
            _identifier = identifier;
        }

        public override void Select(PathNode pathNode,
                                    JsonElement root, 
                                    JsonElement current,
                                    IList<JsonElement> nodes,
                                    ResultOptions options)
        {
            TestContext.WriteLine("IdentifierSelector...");
            if (current.ValueKind == JsonValueKind.Object)
            { 
                JsonElement value;
                if (current.TryGetProperty(_identifier, out value))
                {
                    this.EvaluateTail(pathNode, root, value, nodes, options);
                }
            }
        }
    }

    class IndexSelector : BaseSelector
    {
        Int32 _index;

        public IndexSelector(Int32 index)
        {
            _index = index;
        }

        public override void Select(PathNode pathNode,
                                    JsonElement root, 
                                    JsonElement current,
                                    IList<JsonElement> nodes,
                                    ResultOptions options)
        {
            if (current.ValueKind == JsonValueKind.Array)
            { 
                if (_index >= 0 && _index < current.GetArrayLength())
                {
                    this.EvaluateTail(pathNode, root, current[_index], nodes, options);
                }
                else
                {
                    Int32 index = current.GetArrayLength() + _index;
                    if (index >= 0 && index < current.GetArrayLength())
                    {
                        this.EvaluateTail(pathNode, root, current[index], nodes, options);
                    }
                }
            }
        }
    }

    class SliceSelector : BaseSelector
    {
        Slice _slice;

        public SliceSelector(Slice slice)
        {
            _slice = slice;
        }

        public override void Select(PathNode pathNode,
                                    JsonElement root,
                                    JsonElement current,
                                    IList<JsonElement> nodes,
                                    ResultOptions options) 
        {
            if (current.ValueKind == JsonValueKind.Array)
            {
                Int32 start = _slice.GetStart(current.GetArrayLength());
                Int32 end = _slice.GetStop(current.GetArrayLength());
                Int32 step = _slice.Step;

                if (step > 0)
                {
                    if (start < 0)
                    {
                        start = 0;
                    }
                    if (end > current.GetArrayLength())
                    {
                        end = current.GetArrayLength();
                    }
                    for (Int32 i = start; i < end; i += step)
                    {
                        this.EvaluateTail(pathNode, root, current[i], nodes, options);
                    }
                }
                else if (step < 0)
                {
                    if (start >= current.GetArrayLength())
                    {
                        start = current.GetArrayLength() - 1;
                    }
                    if (end < -1)
                    {
                        end = -1;
                    }
                    for (Int32 i = start; i > end; i += step)
                    {
                        if (i < current.GetArrayLength())
                        {
                            this.EvaluateTail(pathNode, root, current[i], nodes, options);
                        }
                    }
                }
            }
        }
    };

    class RecursiveDescentSelector : BaseSelector
    {
        public override void Select(PathNode pathNode,
                                    JsonElement root, 
                                    JsonElement current,
                                    IList<JsonElement> nodes,
                                    ResultOptions options)
        {
            TestContext.WriteLine("RecursiveDescentSelector ...");
            if (current.ValueKind == JsonValueKind.Array)
            {
                TestContext.WriteLine("RecursiveDescentSelector Array ...");
                this.EvaluateTail(pathNode, root, current, nodes, options);
                foreach (var item in current.EnumerateArray())
                {
                    Select(pathNode, root, item, nodes, options);
                }
            }
            else if (current.ValueKind == JsonValueKind.Object)
            {
                TestContext.WriteLine("RecursiveDescentSelector Object ...");
                this.EvaluateTail(pathNode, root, current, nodes, options);
                foreach (var prop in current.EnumerateObject())
                {
                    Select(pathNode, root, prop.Value, nodes, options);
                }
            }
        }
    }

    class WildcardSelector : BaseSelector
    {
        public override void Select(PathNode pathNode,
                                    JsonElement root, 
                                    JsonElement current,
                                    IList<JsonElement> nodes,
                                    ResultOptions options)
        {
            TestContext.WriteLine("WildcardSelector ...");
            if (current.ValueKind == JsonValueKind.Array)
            {
                TestContext.WriteLine("WildcardSelector Array ...");
                foreach (var item in current.EnumerateArray())
                {
                    this.EvaluateTail(pathNode, root, item, nodes, options);
                }
            }
            else if (current.ValueKind == JsonValueKind.Object)
            {
                TestContext.WriteLine("WildcardSelector Object ...");
                foreach (var prop in current.EnumerateObject())
                {
                    this.EvaluateTail(pathNode, root, prop.Value, nodes, options);
                }
            }
        }
    }

    class UnionSelector : ISelector
    {
        IList<ISelector> _selectors;

        internal UnionSelector(IList<ISelector> selectors)
        {
            _selectors = selectors;
        }

        public void AppendSelector(ISelector tail)
        {
            foreach (var selector in _selectors)
            {
                selector.AppendSelector(tail);
            }
        }

        public void Select(PathNode pathNode,
                           JsonElement root, 
                           JsonElement current,
                           IList<JsonElement> nodes,
                           ResultOptions options)
        {
            foreach (var selector in _selectors)
            {
                selector.Select(pathNode, root, current, nodes, options);
            }
        }
    }

    class FilterSelector : BaseSelector
    {
        Expression _expr;

        public FilterSelector(Expression expr)
        {
            _expr = expr;
        }

        public override void Select(PathNode pathNode,
                                    JsonElement root, 
                                    JsonElement current,
                                    IList<JsonElement> nodes,
                                    ResultOptions options)
        {
        }
    }


} // namespace JsonCons.JsonPathLib

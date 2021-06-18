using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using NUnit.Framework;

namespace JsonCons.JsonPathLib
{
    struct Slice
    {
        Int32? _start;
        Int32? _stop;

        public Int32 Step {get;}

        public Slice(Int32? start, Int32? stop, Int32 step) 
        {
            _start = start;
            _stop = stop;
            Step = step;
        }

        public Int32 GetStart(Int32 size)
        {
            if (_start != null)
            {
                Int32 len = _start.Value >= 0 ? _start.Value : size + _start.Value;
                return len <= size ? len : size;
            }
            else
            {
                if (Step >= 0)
                {
                    return 0;
                }
                else 
                {
                    return size;
                }
            }
        }

        public Int32 GetStop(Int32 size)
        {
            if (_stop != null)
            {
                Int32 len = _stop.Value >= 0 ? _stop.Value : size + _stop.Value;
                return len <= size ? len : size;
            }
            else
            {
                return Step >= 0 ? size : -1;
            }
        }
    };

    public abstract class BaseSelector : ISelector 
    {
        ISelector Tail {get;set;} = null;

        public abstract void Select(JsonElement root, 
                                    JsonElement current,
                                    IList<JsonElement> nodes);

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

        protected void EvaluateTail(JsonElement root, 
                                    JsonElement current,
                                    IList<JsonElement> nodes)
        {
            if (Tail == null)
            {
                nodes.Add(current);
            }
            else
            {
                Tail.Select(root, current, nodes);
            }
        }
    }

    public class RootSelector : BaseSelector
    {
        public override void Select(JsonElement root, 
                                    JsonElement current,
                                    IList<JsonElement> nodes)
        {
            TestContext.WriteLine("RootSelector...");
            this.EvaluateTail(root, root, nodes);        
        }
    }

    public class CurrentNodeSelector : BaseSelector
    {
        public override void Select(JsonElement root, 
                                    JsonElement current,
                                    IList<JsonElement> nodes)
        {
            this.EvaluateTail(root, current, nodes);        
        }
    }

    public class IdentifierSelector : BaseSelector
    {
        string _identifier;

        public IdentifierSelector(string identifier)
        {
            _identifier = identifier;
        }

        public override void Select(JsonElement root, 
                                    JsonElement current,
                                    IList<JsonElement> nodes)
        {
            TestContext.WriteLine("IdentifierSelector...");
            if (current.ValueKind == JsonValueKind.Object)
            { 
                JsonElement value;
                if (current.TryGetProperty(_identifier, out value))
                {
                    this.EvaluateTail(root, value, nodes);
                }
            }
        }
    }

    public class IndexSelector : BaseSelector
    {
        Int32 _index;

        public IndexSelector(Int32 index)
        {
            _index = index;
        }

        public override void Select(JsonElement root, 
                                    JsonElement current,
                                    IList<JsonElement> nodes)
        {
            if (current.ValueKind == JsonValueKind.Array)
            { 
                if (_index >= 0 && _index < current.GetArrayLength())
                {
                    this.EvaluateTail(root, current[_index], nodes);
                }
                else
                {
                    Int32 index = current.GetArrayLength() + _index;
                    if (index >= 0 && index < current.GetArrayLength())
                    {
                        this.EvaluateTail(root, current[index], nodes);
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

        public override void Select(JsonElement root,
                                    JsonElement current,
                                    IList<JsonElement> nodes) 
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
                        this.EvaluateTail(root, current[i], nodes);
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
                            this.EvaluateTail(root, current[i], nodes);
                        }
                    }
                }
            }
        }
    };

    public class RecursiveDescentSelector : BaseSelector
    {
        public override void Select(JsonElement root, 
                                    JsonElement current,
                                    IList<JsonElement> nodes)
        {
            TestContext.WriteLine("RecursiveDescentSelector ...");
            if (current.ValueKind == JsonValueKind.Array)
            {
                TestContext.WriteLine("RecursiveDescentSelector Array ...");
                this.EvaluateTail(root, current, nodes);
                foreach (var item in current.EnumerateArray())
                {
                    Select(root, item, nodes);
                }
            }
            else if (current.ValueKind == JsonValueKind.Object)
            {
                TestContext.WriteLine("RecursiveDescentSelector Object ...");
                this.EvaluateTail(root, current, nodes);
                foreach (var prop in current.EnumerateObject())
                {
                    Select(root, prop.Value, nodes);
                }
            }
        }
    }

    public class WildcardSelector : BaseSelector
    {
        public override void Select(JsonElement root, 
                                    JsonElement current,
                                    IList<JsonElement> nodes)
        {
            TestContext.WriteLine("WildcardSelector ...");
            if (current.ValueKind == JsonValueKind.Array)
            {
                TestContext.WriteLine("WildcardSelector Array ...");
                foreach (var item in current.EnumerateArray())
                {
                    this.EvaluateTail(root, item, nodes);
                }
            }
            else if (current.ValueKind == JsonValueKind.Object)
            {
                TestContext.WriteLine("WildcardSelector Object ...");
                foreach (var prop in current.EnumerateObject())
                {
                    this.EvaluateTail(root, prop.Value, nodes);
                }
            }
        }
    }

    public class UnionSelector : ISelector
    {
        IList<ISelector> _selectors;

        UnionSelector(IList<ISelector> selectors)
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

        public void Select(JsonElement root, 
                           JsonElement current,
                           IList<JsonElement> nodes)
        {
            foreach (var selector in _selectors)
            {
                selector.Select(root, current, nodes);
            }
        }
    }

    public class FilterSelector : BaseSelector
    {
        public override void Select(JsonElement root, 
                                    JsonElement current,
                                    IList<JsonElement> nodes)
        {
        }
    }


} // namespace JsonCons.JsonPathLib

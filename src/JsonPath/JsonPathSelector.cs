using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using NUnit.Framework;

namespace JsonCons.JsonPathLib
{
    interface INodeAccumulator
    {
        void Accumulate(PathNode pathTail, JsonElement value);
    };

    static class PathGenerator 
    {
        static internal PathNode Generate(PathNode pathTail, 
                                          Int32 index, 
                                          ResultOptions options) 
        {
            if ((options & ResultOptions.Path) != 0)
            {
                return new PathNode(pathTail, index);
            }
            else
            {
                return pathTail;
            }
        }

        static internal PathNode Generate(PathNode pathTail, 
                                          string identifier, 
                                          ResultOptions options) 
        {
            if ((options & ResultOptions.Path) != 0)
            {
                return new PathNode(pathTail, identifier);
            }
            else
            {
                return pathTail;
            }
        }
    };

    interface ISelector 
    {
        void Select(JsonElement root,
                    PathNode pathTail,
                    JsonElement current, 
                    INodeAccumulator accumulator,
                    ResultOptions options);

        void AppendSelector(ISelector tail);
    };

    abstract class BaseSelector : ISelector 
    {
        ISelector Tail {get;set;} = null;

        public abstract void Select(JsonElement root, 
                                    PathNode pathTail,
                                    JsonElement current,
                                    INodeAccumulator accumulator,
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

        protected void EvaluateTail(JsonElement root, 
                                    PathNode pathTail,
                                    JsonElement current,
                                    INodeAccumulator accumulator,
                                    ResultOptions options)
        {
            if (Tail == null)
            {
                accumulator.Accumulate(pathTail, current);
            }
            else
            {
                Tail.Select(root, pathTail, current, accumulator, options);
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

        public override void Select(JsonElement root, 
                                    PathNode pathTail,
                                    JsonElement current,
                                    INodeAccumulator accumulator,
                                    ResultOptions options)
        {
            this.EvaluateTail(root, pathTail, root, accumulator, options);        
        }
    }

    class CurrentNodeSelector : BaseSelector
    {
        public override void Select(JsonElement root, 
                                    PathNode pathTail,
                                    JsonElement current,
                                    INodeAccumulator accumulator,
                                    ResultOptions options)
        {
            this.EvaluateTail(root, pathTail, current, accumulator, options);        
        }
    }

    class IdentifierSelector : BaseSelector
    {
        string _identifier;

        internal IdentifierSelector(string identifier)
        {
            _identifier = identifier;
        }

        public override void Select(JsonElement root, 
                                    PathNode pathTail,
                                    JsonElement current,
                                    INodeAccumulator accumulator,
                                    ResultOptions options)
        {
            if (current.ValueKind == JsonValueKind.Object)
            { 
                JsonElement value;
                if (current.TryGetProperty(_identifier, out value))
                {
                    this.EvaluateTail(root, 
                                      PathGenerator.Generate(pathTail, _identifier, options), 
                                      value, accumulator, options);
                }
            }
        }
    }

    class IndexSelector : BaseSelector
    {
        Int32 _index;

        internal IndexSelector(Int32 index)
        {
            _index = index;
        }

        public override void Select(JsonElement root, 
                                    PathNode pathTail,
                                    JsonElement current,
                                    INodeAccumulator accumulator,
                                    ResultOptions options)
        {
            if (current.ValueKind == JsonValueKind.Array)
            { 
                if (_index >= 0 && _index < current.GetArrayLength())
                {
                    this.EvaluateTail(root, 
                                      PathGenerator.Generate(pathTail, _index, options), 
                                      current[_index], accumulator, options);
                }
                else
                {
                    Int32 index = current.GetArrayLength() + _index;
                    if (index >= 0 && index < current.GetArrayLength())
                    {
                        this.EvaluateTail(root, 
                                          PathGenerator.Generate(pathTail, _index, options), 
                                          current[index], accumulator, options);
                    }
                }
            }
        }
    }

    class SliceSelector : BaseSelector
    {
        Slice _slice;

        internal SliceSelector(Slice slice)
        {
            _slice = slice;
        }

        public override void Select(JsonElement root,
                                    PathNode pathTail,
                                    JsonElement current,
                                    INodeAccumulator accumulator,
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
                        this.EvaluateTail(root, 
                                          PathGenerator.Generate(pathTail, i, options), 
                                          current[i], accumulator, options);
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
                            this.EvaluateTail(root, 
                                              PathGenerator.Generate(pathTail, i, options), 
                                              current[i], accumulator, options);
                        }
                    }
                }
            }
        }
    };

    class RecursiveDescentSelector : BaseSelector
    {
        public override void Select(JsonElement root, 
                                    PathNode pathTail,
                                    JsonElement current,
                                    INodeAccumulator accumulator,
                                    ResultOptions options)
        {
            if (current.ValueKind == JsonValueKind.Array)
            {
                this.EvaluateTail(root, pathTail, current, accumulator, options);
                Int32 index = 0;
                foreach (var item in current.EnumerateArray())
                {
                    Select(root, PathGenerator.Generate(pathTail, index++, options), 
                           item, accumulator, options);
                }
            }
            else if (current.ValueKind == JsonValueKind.Object)
            {
                this.EvaluateTail(root, pathTail, current, accumulator, options);
                foreach (var prop in current.EnumerateObject())
                {
                    Select(root, PathGenerator.Generate(pathTail, prop.Name, options), 
                           prop.Value, accumulator, options);
                }
            }
        }
    }

    class WildcardSelector : BaseSelector
    {
        public override void Select(JsonElement root, 
                                    PathNode pathTail,
                                    JsonElement current,
                                    INodeAccumulator accumulator,
                                    ResultOptions options)
        {
            if (current.ValueKind == JsonValueKind.Array)
            {
                Int32 index = 0;
                foreach (var item in current.EnumerateArray())
                {
                    this.EvaluateTail(root, PathGenerator.Generate(pathTail, index++, options), 
                                      item, accumulator, options);
                }
            }
            else if (current.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in current.EnumerateObject())
                {
                    this.EvaluateTail(root, PathGenerator.Generate(pathTail, prop.Name, options), 
                                      prop.Value, accumulator, options);
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

        public void Select(JsonElement root, 
                           PathNode pathTail,
                           JsonElement current,
                           INodeAccumulator accumulator,
                           ResultOptions options)
        {
            foreach (var selector in _selectors)
            {
                selector.Select(root, pathTail, current, accumulator, options);
            }
        }
    }

    class FilterSelector : BaseSelector
    {
        Expression _expr;

        internal FilterSelector(Expression expr)
        {
            _expr = expr;
        }

        public override void Select(JsonElement root, 
                                    PathNode pathTail,
                                    JsonElement current,
                                    INodeAccumulator accumulator,
                                    ResultOptions options)
        {
        }
    }

} // namespace JsonCons.JsonPathLib

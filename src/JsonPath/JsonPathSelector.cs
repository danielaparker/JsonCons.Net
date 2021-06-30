using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using NUnit.Framework;

namespace JsonCons.JsonPathLib
{
    static class PathGenerator 
    {
        static internal PathNode Generate(PathNode pathStem, 
                                          Int32 index, 
                                          ResultOptions options) 
        {
            if ((options & ResultOptions.Path) != 0)
            {
                return new PathNode(pathStem, index);
            }
            else
            {
                return pathStem;
            }
        }

        static internal PathNode Generate(PathNode pathStem, 
                                          string identifier, 
                                          ResultOptions options) 
        {
            if ((options & ResultOptions.Path) != 0)
            {
                return new PathNode(pathStem, identifier);
            }
            else
            {
                return pathStem;
            }
        }
    };

    interface ISelector 
    {
        void Select(JsonElement root,
                    PathNode pathStem,
                    JsonElement current, 
                    INodeAccumulator accumulator,
                    ResultOptions options);

        IJsonValue Evaluate(IJsonValue root,
                            PathNode pathStem, 
                            IJsonValue current, 
                            ResultOptions options);

        void AppendSelector(ISelector tail);
    };

    abstract class BaseSelector : ISelector 
    {
        ISelector Tail {get;set;} = null;

        public abstract void Select(JsonElement root, 
                                    PathNode pathStem,
                                    JsonElement current,
                                    INodeAccumulator accumulator,
                                    ResultOptions options);

        public abstract IJsonValue Evaluate(IJsonValue root, 
                                            PathNode pathStem, 
                                            IJsonValue current,
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
                                    PathNode pathStem,
                                    JsonElement current,
                                    INodeAccumulator accumulator,
                                    ResultOptions options)
        {
            if (Tail == null)
            {
                accumulator.Accumulate(pathStem, current);
            }
            else
            {
                Tail.Select(root, pathStem, current, accumulator, options);
            }
        }

        protected IJsonValue EvaluateTail(IJsonValue root, 
                                          PathNode pathStem, 
                                          IJsonValue current,
                                          ResultOptions options)
        {
            if (Tail == null)
            {
                return current;
            }
            else
            {
                return Tail.Evaluate(root, pathStem, current, options);
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
                                    PathNode pathStem,
                                    JsonElement current,
                                    INodeAccumulator accumulator,
                                    ResultOptions options)
        {
            this.EvaluateTail(root, pathStem, root, accumulator, options);        
        }
        public override IJsonValue Evaluate(IJsonValue root, 
                                            PathNode pathStem, 
                                            IJsonValue current,
                                            ResultOptions options)
        {
            return this.EvaluateTail(root, pathStem, root, options);        
        }

        public override string ToString()
        {
            return "RootSelector";
        }
    }

    class CurrentNodeSelector : BaseSelector
    {
        public override void Select(JsonElement root, 
                                    PathNode pathStem,
                                    JsonElement current,
                                    INodeAccumulator accumulator,
                                    ResultOptions options)
        {
            this.EvaluateTail(root, pathStem, current, accumulator, options);        
        }
        public override IJsonValue Evaluate(IJsonValue root, 
                                            PathNode pathStem, 
                                            IJsonValue current,
                                            ResultOptions options)
        {
            return this.EvaluateTail(root, pathStem, current, options);        
        }

        public override string ToString()
        {
            return "CurrentNodeSelector";
        }
    }

    class ParentNodeSelector : BaseSelector
    {
        internal ParentNodeSelector()
        {
        }

        public override void Select(JsonElement root, 
                                    PathNode pathStem,
                                    JsonElement current,
                                    INodeAccumulator accumulator,
                                    ResultOptions options)
        {
            if (pathStem.Parent != null)
            {
                NormalizedPath path = new NormalizedPath(pathStem.Parent);
                JsonElement parent = JsonPath.Select(root, path);
                this.EvaluateTail(root, path.Stem, parent, accumulator, options);        
            }
        }
        public override IJsonValue Evaluate(IJsonValue root, 
                                            PathNode pathStem, 
                                            IJsonValue current,
                                            ResultOptions options)
        {
            if (pathStem.Parent != null)
            {
                NormalizedPath path = new NormalizedPath(pathStem.Parent);
                IJsonValue parent = JsonPath.Select(root, path);
                return this.EvaluateTail(root, path.Stem, current, options);        
            }
            else
            {
                return JsonConstants.Null;
            }
        }

        public override string ToString()
        {
            return "RootSelector";
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
                                    PathNode pathStem,
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
                                      PathGenerator.Generate(pathStem, _identifier, options), 
                                      value, accumulator, options);
                }
            }
        }

        public override IJsonValue Evaluate(IJsonValue root, 
                                            PathNode pathStem, 
                                            IJsonValue current,
                                            ResultOptions options)
        {
            if (current.ValueKind == JsonValueKind.Object)
            { 
                IJsonValue value;
                if (current.TryGetProperty(_identifier, out value))
                {
                    return this.EvaluateTail(root, 
                                             PathGenerator.Generate(pathStem, _identifier, options), 
                                             value, options);
                }
                else
                {
                    return JsonConstants.Null;
                }
            }
            else
            {
                return JsonConstants.Null;
            }
        }

        public override string ToString()
        {
            return $"IdentifierSelector {_identifier}";
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
                                    PathNode pathStem,
                                    JsonElement current,
                                    INodeAccumulator accumulator,
                                    ResultOptions options)
        {
            if (current.ValueKind == JsonValueKind.Array)
            { 
                if (_index >= 0 && _index < current.GetArrayLength())
                {
                    this.EvaluateTail(root, 
                                      PathGenerator.Generate(pathStem, _index, options), 
                                      current[_index], accumulator, options);
                }
                else
                {
                    Int32 index = current.GetArrayLength() + _index;
                    if (index >= 0 && index < current.GetArrayLength())
                    {
                        this.EvaluateTail(root, 
                                          PathGenerator.Generate(pathStem, _index, options), 
                                          current[index], accumulator, options);
                    }
                }
            }
        }

        public override IJsonValue Evaluate(IJsonValue root, 
                                            PathNode pathStem,
                                            IJsonValue current,
                                            ResultOptions options)
        {
            if (current.ValueKind == JsonValueKind.Array)
            { 
                if (_index >= 0 && _index < current.GetArrayLength())
                {
                    return this.EvaluateTail(root, 
                                             PathGenerator.Generate(pathStem, _index, options), 
                                             current[_index], options);
                }
                else
                {
                    Int32 index = current.GetArrayLength() + _index;
                    if (index >= 0 && index < current.GetArrayLength())
                    {
                        return this.EvaluateTail(root, 
                                                 PathGenerator.Generate(pathStem, _index, options), 
                                                 current[index], options);
                    }
                    else
                    {
                        return JsonConstants.Null;
                    }
                }
            }
            else
            {
                return JsonConstants.Null;
            }
        }

        public override string ToString()
        {
            return $"IndexSelector {_index}";
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
                                    PathNode pathStem,
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
                                          PathGenerator.Generate(pathStem, i, options), 
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
                                              PathGenerator.Generate(pathStem, i, options), 
                                              current[i], accumulator, options);
                        }
                    }
                }
            }
        }

        public override IJsonValue Evaluate(IJsonValue root,
                                            PathNode pathStem,
                                            IJsonValue current,
                                            ResultOptions options) 
        {
            var results = new List<IJsonValue>();
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
                        results.Add(this.EvaluateTail(root, 
                                                      PathGenerator.Generate(pathStem, i, options), 
                                                      current[i], options));
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
                            results.Add(this.EvaluateTail(root, 
                                                          PathGenerator.Generate(pathStem, i, options), 
                                                          current[i], options));
                        }
                    }
                }
            }
            return new ArrayJsonValue(results);
        }

        public override string ToString()
        {
            return "SliceSelector";
        }
    };

    class RecursiveDescentSelector : BaseSelector
    {
        public override void Select(JsonElement root, 
                                    PathNode pathStem,
                                    JsonElement current,
                                    INodeAccumulator accumulator,
                                    ResultOptions options)
        {
            if (current.ValueKind == JsonValueKind.Array)
            {
                this.EvaluateTail(root, pathStem, current, accumulator, options);
                Int32 index = 0;
                foreach (var item in current.EnumerateArray())
                {
                    Select(root, 
                           PathGenerator.Generate(pathStem, index++, options), 
                           item, accumulator, options);
                }
            }
            else if (current.ValueKind == JsonValueKind.Object)
            {
                this.EvaluateTail(root, pathStem, current, accumulator, options);
                foreach (var prop in current.EnumerateObject())
                {
                    Select(root, 
                           PathGenerator.Generate(pathStem, prop.Name, options), 
                           prop.Value, accumulator, options);
                }
            }
        }
        public override IJsonValue Evaluate(IJsonValue root, 
                                            PathNode pathStem,
                                            IJsonValue current,
                                            ResultOptions options)
        {
            var results = new List<IJsonValue>();
            if (current.ValueKind == JsonValueKind.Array)
            {
                results.Add(this.EvaluateTail(root, pathStem, current, options));
                foreach (var item in current.EnumerateArray())
                {
                    results.Add(Evaluate(root,
                                         pathStem, 
                                         item, options));
                }
            }
            else if (current.ValueKind == JsonValueKind.Object)
            {
                results.Add(this.EvaluateTail(root, pathStem, current, options));
                foreach (var prop in current.EnumerateObject())
                {
                    results.Add(Evaluate(root,
                                         pathStem, 
                                         prop.Value, options));
                }
            }
            return new ArrayJsonValue(results);
        }

        public override string ToString()
        {
            return "RecursiveDescentSelector";
        }
    }

    class WildcardSelector : BaseSelector
    {
        public override void Select(JsonElement root, 
                                    PathNode pathStem,
                                    JsonElement current,
                                    INodeAccumulator accumulator,
                                    ResultOptions options)
        {
            if (current.ValueKind == JsonValueKind.Array)
            {
                Int32 index = 0;
                foreach (var item in current.EnumerateArray())
                {
                    this.EvaluateTail(root, 
                                      PathGenerator.Generate(pathStem, index++, options), 
                                      item, accumulator, options);
                }
            }
            else if (current.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in current.EnumerateObject())
                {
                    this.EvaluateTail(root, 
                                      PathGenerator.Generate(pathStem, prop.Name, options), 
                                      prop.Value, accumulator, options);
                }
            }
        }
        public override IJsonValue Evaluate(IJsonValue root, 
                                            PathNode pathStem,
                                            IJsonValue current,
                                            ResultOptions options)
        {
            var results = new List<IJsonValue>();
            if (current.ValueKind == JsonValueKind.Array)
            {
                Int32 index = 0;
                foreach (var item in current.EnumerateArray())
                {
                    results.Add(this.EvaluateTail(root, 
                                                  PathGenerator.Generate(pathStem, index++, options), 
                                                  item, options));
                }
            }
            else if (current.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in current.EnumerateObject())
                {
                    results.Add(this.EvaluateTail(root, 
                                                  PathGenerator.Generate(pathStem, prop.Name, options), 
                                                  prop.Value, options));
                }
            }
            return new ArrayJsonValue(results);
        }

        public override string ToString()
        {
            return "WildcardSelector";
        }
    }

    class UnionSelector : ISelector
    {
        IList<ISelector> _selectors;
        ISelector _tail;

        internal UnionSelector(IList<ISelector> selectors)
        {
            _selectors = selectors;
            _tail = null;
        }

        public void AppendSelector(ISelector tail)
        {
            if (_tail == null)
            {
                _tail = tail;
                foreach (var selector in _selectors)
                {
                    selector.AppendSelector(tail);
                }
            }
            else
            {
                _tail.AppendSelector(tail);
            }
        }

        public void Select(JsonElement root, 
                           PathNode pathStem,
                           JsonElement current,
                           INodeAccumulator accumulator,
                           ResultOptions options)
        {
            foreach (var selector in _selectors)
            {
                selector.Select(root, pathStem, current, accumulator, options);
            }
        }

        public IJsonValue Evaluate(IJsonValue root, 
                                   PathNode pathStem,
                                   IJsonValue current,
                                   ResultOptions options)
        {
            var results = new List<IJsonValue>();
            foreach (var selector in _selectors)
            {
                results.Add(selector.Evaluate(root, pathStem, current, options));
            }
            return new ArrayJsonValue(results);
        }

        public override string ToString()
        {
            return "UnionSelector";
        }
    }

    class FilterSelector : BaseSelector
    {
        IExpression _expr;

        internal FilterSelector(IExpression expr)
        {
            //TestContext.WriteLine("FilterSelector constructor");

            _expr = expr;
        }

        public override void Select(JsonElement root, 
                                    PathNode pathStem,
                                    JsonElement current,
                                    INodeAccumulator accumulator,
                                    ResultOptions options)
        {
            //TestContext.WriteLine("FilterSelector");

            if (current.ValueKind == JsonValueKind.Array)
            {
                Int32 index = 0;
                foreach (var item in current.EnumerateArray())
                {
                    var r = _expr.Evaluate(new JsonElementJsonValue(root), new JsonElementJsonValue(item), options);
                    if (Expression.IsTrue(r))
                    {
                        this.EvaluateTail(root, 
                                          PathGenerator.Generate(pathStem, index++, options), 
                                          item, accumulator, options);
                    }
                }
            }
            else if (current.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in current.EnumerateObject())
                {
                    var r = _expr.Evaluate(new JsonElementJsonValue(root), new JsonElementJsonValue(property.Value), options);
                    if (Expression.IsTrue(r))
                    {
                        this.EvaluateTail(root, 
                                          PathGenerator.Generate(pathStem, property.Name, options), 
                                          property.Value, accumulator, options);
                    }
                }
            }
        }

        public override IJsonValue Evaluate(IJsonValue root, 
                                            PathNode pathStem,
                                            IJsonValue current,
                                            ResultOptions options)
        {
            //TestContext.WriteLine("FilterSelector");

            var results = new List<IJsonValue>();
            if (current.ValueKind == JsonValueKind.Array)
            {
                Int32 index = 0;
                foreach (var item in current.EnumerateArray())
                {
                    var r = _expr.Evaluate(root, item, options);
                    if (Expression.IsTrue(r))
                    {
                        results.Add(this.EvaluateTail(root, 
                                                      PathGenerator.Generate(pathStem, index++, options), 
                                                      item, options));
                    }
                }
            }
            else if (current.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in current.EnumerateObject())
                {
                    var r = _expr.Evaluate(root, property.Value, options);
                    if (Expression.IsTrue(r))
                    {
                        results.Add(this.EvaluateTail(root, 
                                                      PathGenerator.Generate(pathStem, property.Name, options), 
                                                      property.Value, options));
                    }
                }
            }
            return new ArrayJsonValue(results);
        }

        public override string ToString()
        {
            return "FilterSelector";
        }
    }

} // namespace JsonCons.JsonPathLib

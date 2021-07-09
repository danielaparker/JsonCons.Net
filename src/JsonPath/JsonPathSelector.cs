using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace JsonCons.JsonPathLib
{
    static class PathGenerator 
    {
        static internal PathComponent Generate(PathComponent pathStem, 
                                          Int32 index, 
                                          ResultOptions options) 
        {
            if ((options & ResultOptions.Path) != 0)
            {
                return new PathComponent(pathStem, index);
            }
            else
            {
                return pathStem;
            }
        }

        static internal PathComponent Generate(PathComponent pathStem, 
                                          string identifier, 
                                          ResultOptions options) 
        {
            if ((options & ResultOptions.Path) != 0)
            {
                return new PathComponent(pathStem, identifier);
            }
            else
            {
                return pathStem;
            }
        }
    };

    interface ISelector 
    {
        void Select(DynamicResources resources,
                    IValue root,
                    PathComponent pathStem,
                    IValue current, 
                    INodeAccumulator accumulator,
                    ResultOptions options);

        bool TryEvaluate(DynamicResources resources, 
                         IValue root,
                         PathComponent pathStem, 
                         IValue current, 
                         ResultOptions options,
                         out IValue value);

        void AppendSelector(ISelector tail);

        bool IsRoot();
    };

    abstract class BaseSelector : ISelector 
    {
        ISelector Tail {get;set;} = null;

        public abstract void Select(DynamicResources resources,
                                    IValue root, 
                                    PathComponent pathStem,
                                    IValue current,
                                    INodeAccumulator accumulator,
                                    ResultOptions options);

        public abstract bool TryEvaluate(DynamicResources resources, 
                                         IValue root, 
                                         PathComponent pathStem, 
                                         IValue current,
                                         ResultOptions options,
                                         out IValue value);

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

        protected void TailSelect(DynamicResources resources, 
                                  IValue root, 
                                  PathComponent pathStem,
                                  IValue current,
                                  INodeAccumulator accumulator,
                                  ResultOptions options)
        {
            if (Tail == null)
            {
                accumulator.AddNode(pathStem, current);
            }
            else
            {
                Tail.Select(resources, root, pathStem, current, accumulator, options);
            }
        }

        protected bool TryEvaluateTail(DynamicResources resources, 
                                       IValue root, 
                                       PathComponent pathStem, 
                                       IValue current,
                                       ResultOptions options,
                                       out IValue value)
        {
            if (Tail == null)
            {
                value = current;
                return true;
            }
            else
            {
                return Tail.TryEvaluate(resources, root, pathStem, current, options, out value);
            }
        }

        public virtual bool IsRoot()
        {
            return false;
        }
    }

    sealed class RootSelector : BaseSelector
    {
        readonly Int32 _id;

        internal RootSelector(Int32 id)
        {
            _id = id;
        }

        public override void Select(DynamicResources resources, 
                                    IValue root, 
                                    PathComponent pathStem,
                                    IValue current,
                                    INodeAccumulator accumulator,
                                    ResultOptions options)
        {
            this.TailSelect(resources, root, pathStem, root, accumulator, options);        
        }
        public override bool TryEvaluate(DynamicResources resources, 
                                         IValue root, 
                                         PathComponent pathStem, 
                                         IValue current,
                                         ResultOptions options,
                                         out IValue result)
        {
            if (resources.TryRetrieveFromCache(_id, out result))
            {
                return true;
            }
            else
            {
                if (!this.TryEvaluateTail(resources, root, pathStem, root, options, out result))
                {
                    result = JsonConstants.Null;
                    return false;
                }
                resources.AddToCache(_id, result);
                return true;
            }
        }

        public override bool IsRoot()
        {
            return true;
        }

        public override string ToString()
        {
            return "RootSelector";
        }
    }

    sealed class CurrentNodeSelector : BaseSelector
    {
        public override void Select(DynamicResources resources, 
                                    IValue root, 
                                    PathComponent pathStem,
                                    IValue current,
                                    INodeAccumulator accumulator,
                                    ResultOptions options)
        {
            this.TailSelect(resources, root, pathStem, current, accumulator, options);        
        }
        public override bool TryEvaluate(DynamicResources resources, IValue root, 
                                         PathComponent pathStem, 
                                         IValue current,
                                         ResultOptions options,
                                         out IValue value)
        {
            return this.TryEvaluateTail(resources, root, pathStem, current, options, out value);        
        }

        public override bool IsRoot()
        {
            return true;
        }

        public override string ToString()
        {
            return "CurrentNodeSelector";
        }
    }

    sealed class ParentNodeSelector : BaseSelector
    {
        readonly int _ancestorDepth;

        internal ParentNodeSelector(int ancestorDepth)
        {
            _ancestorDepth = ancestorDepth;
        }

        public override void Select(DynamicResources resources, 
                                    IValue root, 
                                    PathComponent pathStem,
                                    IValue current,
                                    INodeAccumulator accumulator,
                                    ResultOptions options)
        {
            PathComponent ancestor = pathStem;
            int index = 0;
            while (ancestor != null && index < _ancestorDepth)
            {
                ancestor = ancestor.Parent;
                ++index;
            }

            if (ancestor != null)
            {
                NormalizedPath path = new NormalizedPath(ancestor);
                IValue value;
                if (TryGetValue(root, path, out value))
                {
                    this.TailSelect(resources, root, path.Last, value, accumulator, options);        
                }
            }
        }
        public override bool TryEvaluate(DynamicResources resources, IValue root, 
                                         PathComponent pathStem, 
                                         IValue current,
                                         ResultOptions options,
                                         out IValue result)
        {
            PathComponent ancestor = pathStem;
            int index = 0;
            while (ancestor != null && index < _ancestorDepth)
            {
                ancestor = ancestor.Parent;
                ++index;
            }

            if (ancestor != null)
            {
                NormalizedPath path = new NormalizedPath(ancestor);
                IValue value;
                if (TryGetValue(root, path, out value))
                {

                    return this.TryEvaluateTail(resources, root, path.Last, value, options, out result);        
                }
                else
                {
                    result = JsonConstants.Null;
                    return true;
                }
            }
            else
            {
                result = JsonConstants.Null;
                return true;
            }
        }

        bool TryGetValue(IValue root, NormalizedPath path, out IValue element)
        {
            element = root;
            foreach (var pathComponent in path)
            {
                if (pathComponent.ComponentKind == PathComponentKind.Index)
                {
                    if (element.ValueKind != JsonValueKind.Array || pathComponent.GetIndex() >= element.GetArrayLength())
                    {
                        return false; 
                    }
                    element = element[pathComponent.GetIndex()];
                }
                else if (pathComponent.ComponentKind == PathComponentKind.Name)
                {
                    if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(pathComponent.GetName(), out element))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public override string ToString()
        {
            return "RootSelector";
        }
    }

    sealed class IdentifierSelector : BaseSelector
    {
        readonly string _identifier;

        internal IdentifierSelector(string identifier)
        {
            _identifier = identifier;
        }

        public override void Select(DynamicResources resources, 
                                    IValue root, 
                                    PathComponent pathStem,
                                    IValue current,
                                    INodeAccumulator accumulator,
                                    ResultOptions options)
        {
            if (current.ValueKind == JsonValueKind.Object)
            { 
                IValue value;
                if (current.TryGetProperty(_identifier, out value))
                {
                    this.TailSelect(resources, root, 
                                      PathGenerator.Generate(pathStem, _identifier, options), 
                                      value, accumulator, options);
                }
            }
        }

        public override bool TryEvaluate(DynamicResources resources, IValue root, 
                                         PathComponent pathStem, 
                                         IValue current,
                                         ResultOptions options,
                                         out IValue value)
        {
            if (current.ValueKind == JsonValueKind.Object)
            {
                IValue element;
                if (current.TryGetProperty(_identifier, out element))
                {
                    return this.TryEvaluateTail(resources, root, 
                                                PathGenerator.Generate(pathStem, _identifier, options), 
                                                element, options, out value);
                }
                else
                {
                    value = JsonConstants.Null;
                    return true;
                }
            }
            else if (current.ValueKind == JsonValueKind.Array && _identifier == "length")
            {
                value = new DecimalValue(new Decimal(current.GetArrayLength()));
                return true;
            }
            else if (current.ValueKind == JsonValueKind.String && _identifier == "length")
            {
                byte[] bytes = Encoding.UTF32.GetBytes(current.GetString().ToCharArray());
                value = new DecimalValue(new Decimal(current.GetString().Length));
                return true;
            }
            else
            {
                value = JsonConstants.Null;
                return true;
            }
        }

        public override string ToString()
        {
            return $"IdentifierSelector {_identifier}";
        }
    }

    sealed class IndexSelector : BaseSelector
    {
        readonly Int32 _index;

        internal IndexSelector(Int32 index)
        {
            _index = index;
        }

        public override void Select(DynamicResources resources, 
                                    IValue root, 
                                    PathComponent pathStem,
                                    IValue current,
                                    INodeAccumulator accumulator,
                                    ResultOptions options)
        {
            if (current.ValueKind == JsonValueKind.Array)
            { 
                if (_index >= 0 && _index < current.GetArrayLength())
                {
                    this.TailSelect(resources, root, 
                                      PathGenerator.Generate(pathStem, _index, options), 
                                      current[_index], accumulator, options);
                }
                else
                {
                    Int32 index = current.GetArrayLength() + _index;
                    if (index >= 0 && index < current.GetArrayLength())
                    {
                        this.TailSelect(resources, root, 
                                          PathGenerator.Generate(pathStem, _index, options), 
                                          current[index], accumulator, options);
                    }
                }
            }
        }

        public override bool TryEvaluate(DynamicResources resources, IValue root, 
                                         PathComponent pathStem,
                                         IValue current,
                                         ResultOptions options,
                                         out IValue value)
        {
            if (current.ValueKind == JsonValueKind.Array)
            { 
                if (_index >= 0 && _index < current.GetArrayLength())
                {
                    return this.TryEvaluateTail(resources, root, 
                                                PathGenerator.Generate(pathStem, _index, options), 
                                                current[_index], options, out value);
                }
                else
                {
                    Int32 index = current.GetArrayLength() + _index;
                    if (index >= 0 && index < current.GetArrayLength())
                    {
                        return this.TryEvaluateTail(resources, root, 
                                                    PathGenerator.Generate(pathStem, _index, options), 
                                                    current[index], options, out value);
                    }
                    else
                    {
                        value = JsonConstants.Null;
                        return true;
                    }
                }
            }
            else
            {
                value = JsonConstants.Null;
                return true;
            }
        }

        public override string ToString()
        {
            return $"IndexSelector {_index}";
        }
    }

    sealed class SliceSelector : BaseSelector
    {
        readonly Slice _slice;

        internal SliceSelector(Slice slice)
        {
            _slice = slice;
        }

        public override void Select(DynamicResources resources, 
                                    IValue root,
                                    PathComponent pathStem,
                                    IValue current,
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
                        this.TailSelect(resources, root, 
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
                            this.TailSelect(resources, root, 
                                              PathGenerator.Generate(pathStem, i, options), 
                                              current[i], accumulator, options);
                        }
                    }
                }
            }
        }

        public override bool TryEvaluate(DynamicResources resources, 
                                         IValue root,
                                         PathComponent pathStem,
                                         IValue current,
                                         ResultOptions options,
                                         out IValue results) 
        {
            var elements = new List<IValue>();
            INodeAccumulator accumulator = new ValueAccumulator(elements);  
            Select(resources, 
                   root, 
                   pathStem, 
                   current,
                   accumulator,
                   options);   
            results = new ArrayJsonValue(elements);
            return true;
        }

        public override string ToString()
        {
            return "SliceSelector";
        }
    };

    sealed class RecursiveDescentSelector : BaseSelector
    {
        public override void Select(DynamicResources resources, 
                                    IValue root, 
                                    PathComponent pathStem,
                                    IValue current,
                                    INodeAccumulator accumulator,
                                    ResultOptions options)
        {
            if (current.ValueKind == JsonValueKind.Array)
            {
                this.TailSelect(resources, root, pathStem, current, accumulator, options);
                Int32 index = 0;
                foreach (var item in current.EnumerateArray())
                {
                    Select(resources, root, 
                           PathGenerator.Generate(pathStem, index, options), 
                           item, accumulator, options);
                    ++index;
                }
            }
            else if (current.ValueKind == JsonValueKind.Object)
            {
                this.TailSelect(resources, root, pathStem, current, accumulator, options);
                foreach (var prop in current.EnumerateObject())
                {
                    Select(resources, root, 
                           PathGenerator.Generate(pathStem, prop.Name, options), 
                           prop.Value, accumulator, options);
                }
            }
        }
        public override bool TryEvaluate(DynamicResources resources, IValue root, 
                                         PathComponent pathStem,
                                         IValue current,
                                         ResultOptions options,
                                         out IValue results)
        {
            var elements = new List<IValue>();
            INodeAccumulator accumulator = new ValueAccumulator(elements);  
            Select(resources, 
                   root, 
                   pathStem, 
                   current,
                   accumulator,
                   options);   
            results = new ArrayJsonValue(elements);
            return true;
        }

        public override string ToString()
        {
            return "RecursiveDescentSelector";
        }
    }

    sealed class WildcardSelector : BaseSelector
    {
        public override void Select(DynamicResources resources, 
                                    IValue root, 
                                    PathComponent pathStem,
                                    IValue current,
                                    INodeAccumulator accumulator,
                                    ResultOptions options)
        {
            if (current.ValueKind == JsonValueKind.Array)
            {
                Int32 index = 0;
                foreach (var item in current.EnumerateArray())
                {
                    this.TailSelect(resources, root, 
                                    PathGenerator.Generate(pathStem, index, options), 
                                    item, accumulator, options);
                    ++index;
                }
            }
            else if (current.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in current.EnumerateObject())
                {
                    this.TailSelect(resources, root, 
                                    PathGenerator.Generate(pathStem, prop.Name, options), 
                                    prop.Value, accumulator, options);
                }
            }
        }
        public override bool TryEvaluate(DynamicResources resources, IValue root, 
                                         PathComponent pathStem,
                                         IValue current,
                                         ResultOptions options,
                                         out IValue results)
        {
            var elements = new List<IValue>();
            INodeAccumulator accumulator = new ValueAccumulator(elements);  
            Select(resources, 
                   root, 
                   pathStem, 
                   current,
                   accumulator,
                   options);   
            results = new ArrayJsonValue(elements);
            return true;
        }

        public override string ToString()
        {
            return "WildcardSelector";
        }
    }

    sealed class UnionSelector : ISelector
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

        public void Select(DynamicResources resources, 
                           IValue root, 
                           PathComponent pathStem,
                           IValue current,
                           INodeAccumulator accumulator,
                           ResultOptions options)
        {
            foreach (var selector in _selectors)
            {
                selector.Select(resources, root, pathStem, current, accumulator, options);
            }
        }

        public bool TryEvaluate(DynamicResources resources, IValue root, 
                                PathComponent pathStem,
                                IValue current,
                                ResultOptions options,
                                out IValue results)
        {
            var elements = new List<IValue>();
            INodeAccumulator accumulator = new ValueAccumulator(elements);  
            Select(resources, 
                   root, 
                   pathStem, 
                   current,
                   accumulator,
                   options);   
            results = new ArrayJsonValue(elements);
            return true;
        }

        public bool IsRoot()
        {
            return false;
        }

        public override string ToString()
        {
            return "UnionSelector";
        }
    }

    sealed class FilterSelector : BaseSelector
    {
        readonly IExpression _expr;

        internal FilterSelector(IExpression expr)
        {
            _expr = expr;
        }

        public override void Select(DynamicResources resources, 
                                    IValue root, 
                                    PathComponent pathStem,
                                    IValue current,
                                    INodeAccumulator accumulator,
                                    ResultOptions options)
        {
            if (current.ValueKind == JsonValueKind.Array)
            {
                Int32 index = 0;
                foreach (var item in current.EnumerateArray())
                {
                    IValue val;
                    if (_expr.TryEvaluate(resources, root, item, options, out val) 
                        && Expression.IsTrue(val)) 
                    {
                        this.TailSelect(resources, root, 
                                        PathGenerator.Generate(pathStem, index, options), 
                                        item, accumulator, options);
                    }
                    ++index;
                }
            }
            else if (current.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in current.EnumerateObject())
                {
                    IValue val;
                    if (_expr.TryEvaluate(resources, root, property.Value, options, out val) 
                        && Expression.IsTrue(val))
                    {
                        this.TailSelect(resources, root, 
                                          PathGenerator.Generate(pathStem, property.Name, options), 
                                          property.Value, accumulator, options);
                    }
                }
            }
        }

        public override bool TryEvaluate(DynamicResources resources, IValue root, 
                                         PathComponent pathStem,
                                         IValue current,
                                         ResultOptions options,
                                         out IValue results)
        {
            var elements = new List<IValue>();
            INodeAccumulator accumulator = new ValueAccumulator(elements);  
            Select(resources, 
                   root, 
                   pathStem, 
                   current,
                   accumulator,
                   options);   
            results = new ArrayJsonValue(elements);
            return true;
        }

        public override string ToString()
        {
            return "FilterSelector";
        }
    }

} // namespace JsonCons.JsonPathLib

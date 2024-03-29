﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace JsonCons.JsonPath
{
    static class PathGenerator 
    {
        static internal JsonLocationNode Generate(JsonLocationNode lastNode, 
                                                  Int32 index, 
                                                  ProcessingFlags options) 
        {
            if ((options & ProcessingFlags.Path) != 0)
            {
                return new JsonLocationNode(lastNode, index);
            }
            else
            {
                return lastNode;
            }
        }

        static internal JsonLocationNode Generate(JsonLocationNode lastNode, 
                                                  string identifier, 
                                                  ProcessingFlags options) 
        {
            if ((options & ProcessingFlags.Path) != 0)
            {
                return new JsonLocationNode(lastNode, identifier);
            }
            else
            {
                return lastNode;
            }
        }
    };

    interface ISelector 
    {
        void Select(DynamicResources resources,
                    IValue root,
                    JsonLocationNode lastNode,
                    IValue current, 
                    INodeReceiver receiver                    ,
                    ProcessingFlags options,
                    int depth);

        bool TryEvaluate(DynamicResources resources, 
                         IValue root,
                         JsonLocationNode lastNode, 
                         IValue current, 
                         ProcessingFlags options,
                         out IValue value);

        void AppendSelector(ISelector tail);

        bool IsRoot();
    };

    abstract class BaseSelector : ISelector 
    {
        ISelector? Tail {get;set;} = null;

        public abstract void Select(DynamicResources resources,
                                    IValue root, 
                                    JsonLocationNode lastNode,
                                    IValue current,
                                    INodeReceiver receiver                    ,
                                    ProcessingFlags options,
                                    int depth);

        public abstract bool TryEvaluate(DynamicResources resources, 
                                         IValue root, 
                                         JsonLocationNode lastNode, 
                                         IValue current,
                                         ProcessingFlags options,
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
                                  JsonLocationNode lastNode,
                                  IValue current,
                                  INodeReceiver receiver                    ,
                                  ProcessingFlags options,
                                  int depth)
        {
            if (Tail == null)
            {
                receiver                    .Add(lastNode, current);
            }
            else
            {
                Tail.Select(resources, root, lastNode, current, receiver                    , options, depth);
            }
        }

        protected bool TryEvaluateTail(DynamicResources resources, 
                                       IValue root, 
                                       JsonLocationNode lastNode, 
                                       IValue current,
                                       ProcessingFlags options,
                                       out IValue value)
        {
            if (Tail == null)
            {
                value = current;
                return true;
            }
            else
            {
                return Tail.TryEvaluate(resources, root, lastNode, current, options, out value);
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
                                    JsonLocationNode lastNode,
                                    IValue current,
                                    INodeReceiver receiver                    ,
                                    ProcessingFlags options,
                                    int depth)
        {
            this.TailSelect(resources, root, lastNode, root, receiver                    , options, depth);        
        }
        public override bool TryEvaluate(DynamicResources resources, 
                                         IValue root, 
                                         JsonLocationNode lastNode, 
                                         IValue current,
                                         ProcessingFlags options,
                                         out IValue result)
        {
            if (resources.TryRetrieveFromCache(_id, out result))
            {
                return true;
            }
            else
            {
                if (!this.TryEvaluateTail(resources, root, lastNode, root, options, out result))
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
                                    JsonLocationNode lastNode,
                                    IValue current,
                                    INodeReceiver receiver                    ,
                                    ProcessingFlags options,
                                    int depth)
        {
            this.TailSelect(resources, root, lastNode, current, receiver                    , options, depth);        
        }
        public override bool TryEvaluate(DynamicResources resources, IValue root, 
                                         JsonLocationNode lastNode, 
                                         IValue current,
                                         ProcessingFlags options,
                                         out IValue value)
        {
            return this.TryEvaluateTail(resources, root, lastNode, current, options, out value);        
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
                                    JsonLocationNode lastNode,
                                    IValue current,
                                    INodeReceiver receiver                    ,
                                    ProcessingFlags options,
                                    int depth)
        {
            JsonLocationNode? ancestor = lastNode;
            int index = 0;
            while (ancestor != null && index < _ancestorDepth)
            {
                ancestor = ancestor.Parent;
                ++index;
            }

            if (ancestor != null)
            {
                JsonLocation path = new JsonLocation(ancestor);
                IValue value;
                if (TryGetValue(root, path, out value))
                {
                    this.TailSelect(resources, root, path.Last, value, receiver                    , options, depth);        
                }
            }
        }
        public override bool TryEvaluate(DynamicResources resources, IValue root, 
                                         JsonLocationNode lastNode, 
                                         IValue current,
                                         ProcessingFlags options,
                                         out IValue result)
        {
            JsonLocationNode? ancestor = lastNode;
            int index = 0;
            while (ancestor != null && index < _ancestorDepth)
            {
                ancestor = ancestor.Parent;
                ++index;
            }

            if (ancestor != null)
            {
                JsonLocation path = new JsonLocation(ancestor);
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

        bool TryGetValue(IValue root, JsonLocation path, out IValue element)
        {
            element = root;
            foreach (var pathComponent in path)
            {
                if (pathComponent.ComponentKind == JsonLocationNodeKind.Index)
                {
                    if (element.ValueKind != JsonValueKind.Array || pathComponent.GetIndex() >= element.GetArrayLength())
                    {
                        return false; 
                    }
                    element = element[pathComponent.GetIndex()];
                }
                else if (pathComponent.ComponentKind == JsonLocationNodeKind.Name)
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
                                    JsonLocationNode lastNode,
                                    IValue current,
                                    INodeReceiver receiver                    ,
                                    ProcessingFlags options,
                                    int depth)
        {
            if (current.ValueKind == JsonValueKind.Object)
            { 
                IValue value;
                if (current.TryGetProperty(_identifier, out value))
                {
                    this.TailSelect(resources, root, 
                                      PathGenerator.Generate(lastNode, _identifier, options), 
                                      value, receiver                    , options, depth);
                }
            }
        }

        public override bool TryEvaluate(DynamicResources resources, IValue root, 
                                         JsonLocationNode lastNode, 
                                         IValue current,
                                         ProcessingFlags options,
                                         out IValue value)
        {
            if (current.ValueKind == JsonValueKind.Object)
            {
                IValue element;
                if (current.TryGetProperty(_identifier, out element))
                {
                    return this.TryEvaluateTail(resources, root, 
                                                PathGenerator.Generate(lastNode, _identifier, options), 
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
                                    JsonLocationNode lastNode,
                                    IValue current,
                                    INodeReceiver receiver                    ,
                                    ProcessingFlags options,
                                    int depth)
        {
            if (current.ValueKind == JsonValueKind.Array)
            { 
                if (_index >= 0 && _index < current.GetArrayLength())
                {
                    this.TailSelect(resources, root, 
                                      PathGenerator.Generate(lastNode, _index, options), 
                                      current[_index], receiver                    , options, depth);
                }
                else
                {
                    Int32 index = current.GetArrayLength() + _index;
                    if (index >= 0 && index < current.GetArrayLength())
                    {
                        this.TailSelect(resources, root, 
                                          PathGenerator.Generate(lastNode, _index, options), 
                                          current[index], receiver                    , options, depth);
                    }
                }
            }
        }

        public override bool TryEvaluate(DynamicResources resources, IValue root, 
                                         JsonLocationNode lastNode,
                                         IValue current,
                                         ProcessingFlags options,
                                         out IValue value)
        {
            if (current.ValueKind == JsonValueKind.Array)
            { 
                if (_index >= 0 && _index < current.GetArrayLength())
                {
                    return this.TryEvaluateTail(resources, root, 
                                                PathGenerator.Generate(lastNode, _index, options), 
                                                current[_index], options, out value);
                }
                else
                {
                    Int32 index = current.GetArrayLength() + _index;
                    if (index >= 0 && index < current.GetArrayLength())
                    {
                        return this.TryEvaluateTail(resources, root, 
                                                    PathGenerator.Generate(lastNode, _index, options), 
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
                                    JsonLocationNode lastNode,
                                    IValue current,
                                    INodeReceiver receiver                    ,
                                    ProcessingFlags options,
                                    int depth) 
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
                                          PathGenerator.Generate(lastNode, i, options), 
                                          current[i], receiver                    , options, depth);
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
                                              PathGenerator.Generate(lastNode, i, options), 
                                              current[i], receiver                    , options, depth);
                        }
                    }
                }
            }
        }

        public override bool TryEvaluate(DynamicResources resources, 
                                         IValue root,
                                         JsonLocationNode lastNode,
                                         IValue current,
                                         ProcessingFlags options,
                                         out IValue results) 
        {
            var elements = new List<IValue>();
            INodeReceiver receiver = new ValueReceiver(elements);  
            if (resources.Options.ExecutionMode == PathExecutionMode.Parallel)
            {
                receiver = new SynchronizedNodeReceiver(receiver                    );
            }
            Select(resources, 
                   root, 
                   lastNode, 
                   current,
                   receiver                    ,
                   options,
                   0);   
            results = new ArrayValue(elements);
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
                                    JsonLocationNode lastNode,
                                    IValue current,
                                    INodeReceiver receiver                    ,
                                    ProcessingFlags options,
                                    int depth)
        {
            if (depth >= resources.Options.MaxDepth)
            {
                throw new InvalidOperationException($"Maximum depth level exceeded in recursive descent selector.");
            }

            if (current.ValueKind == JsonValueKind.Array)
            {
                this.TailSelect(resources, root, lastNode, current, receiver                    , options, depth+1);
                Int32 index = 0;
                foreach (var item in current.EnumerateArray())
                {
                    Select(resources, root, 
                           PathGenerator.Generate(lastNode, index, options), 
                           item, receiver                    , options, depth+1);
                    ++index;
                }
            }
            else if (current.ValueKind == JsonValueKind.Object)
            {
                this.TailSelect(resources, root, lastNode, current, receiver                    , options, depth+1);
                foreach (var prop in current.EnumerateObject())
                {
                    Select(resources, root, 
                           PathGenerator.Generate(lastNode, prop.Name, options), 
                           prop.Value, receiver                    , options, depth+1);
                }
            }
        }
        public override bool TryEvaluate(DynamicResources resources, IValue root, 
                                         JsonLocationNode lastNode,
                                         IValue current,
                                         ProcessingFlags options,
                                         out IValue results)
        {
            var elements = new List<IValue>();
            INodeReceiver receiver = new ValueReceiver(elements);  
            if (resources.Options.ExecutionMode == PathExecutionMode.Parallel)
            {
                receiver = new SynchronizedNodeReceiver(receiver                    );
            }
            Select(resources, 
                   root, 
                   lastNode, 
                   current,
                   receiver                    ,
                   options,
                   0);   
            results = new ArrayValue(elements);
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
                                    JsonLocationNode lastNode,
                                    IValue current,
                                    INodeReceiver receiver                    ,
                                    ProcessingFlags options,
                                    int depth)
        {
            if (current.ValueKind == JsonValueKind.Array)
            {
                Int32 index = 0;
                foreach (var item in current.EnumerateArray())
                {
                    this.TailSelect(resources, root, 
                                    PathGenerator.Generate(lastNode, index, options), 
                                    item, receiver                    , options, depth);
                    ++index;
                }
            }
            else if (current.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in current.EnumerateObject())
                {
                    this.TailSelect(resources, root, 
                                    PathGenerator.Generate(lastNode, prop.Name, options), 
                                    prop.Value, receiver                    , options, depth);
                }
            }
        }
        public override bool TryEvaluate(DynamicResources resources, IValue root, 
                                         JsonLocationNode lastNode,
                                         IValue current,
                                         ProcessingFlags options,
                                         out IValue results)
        {
            var elements = new List<IValue>();
            INodeReceiver receiver = new ValueReceiver(elements);  
            if (resources.Options.ExecutionMode == PathExecutionMode.Parallel)
            {
                receiver = new SynchronizedNodeReceiver(receiver                    );
            }
            Select(resources, 
                   root, 
                   lastNode, 
                   current,
                   receiver                    ,
                   options,
                   0);   
            results = new ArrayValue(elements);
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
        ISelector? _tail;

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
                           JsonLocationNode lastNode,
                           IValue current,
                           INodeReceiver receiver                    ,
                           ProcessingFlags options,
                           int depth)
        {
            if (resources.Options.ExecutionMode == PathExecutionMode.Sequential)
            {
                foreach (var selector in _selectors)
                {
                    selector.Select(resources, root, lastNode, current, receiver                    , options, depth);
                }
            }
            else
            {
                Action<int> action = delegate(int i)
                {
                    _selectors[i].Select(resources, root, lastNode, current, receiver                    , options, depth);
                };
                Parallel.For(0, _selectors.Count, action);
            }
        }

        public bool TryEvaluate(DynamicResources resources, IValue root, 
                                JsonLocationNode lastNode,
                                IValue current,
                                ProcessingFlags options,
                                out IValue results)
        {
            var elements = new List<IValue>();
            INodeReceiver receiver = new ValueReceiver(elements);  
            if (resources.Options.ExecutionMode == PathExecutionMode.Parallel)
            {
                receiver = new SynchronizedNodeReceiver(receiver                    );
            }
            Select(resources, 
                   root, 
                   lastNode, 
                   current,
                   receiver                    ,
                   options,
                   0);   
            results = new ArrayValue(elements);
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
                                    JsonLocationNode lastNode,
                                    IValue current,
                                    INodeReceiver receiver                    ,
                                    ProcessingFlags options,
                                    int depth)
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
                                        PathGenerator.Generate(lastNode, index, options), 
                                        item, receiver                    , options, depth);
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
                                          PathGenerator.Generate(lastNode, property.Name, options), 
                                          property.Value, receiver                    , options, depth);
                    }
                }
            }
        }

        public override bool TryEvaluate(DynamicResources resources, IValue root, 
                                         JsonLocationNode lastNode,
                                         IValue current,
                                         ProcessingFlags options,
                                         out IValue results)
        {
            var elements = new List<IValue>();
            INodeReceiver receiver = new ValueReceiver(elements);  
            if (resources.Options.ExecutionMode == PathExecutionMode.Parallel)
            {
                receiver = new SynchronizedNodeReceiver(receiver                    );
            }
            Select(resources, 
                   root, 
                   lastNode, 
                   current,
                   receiver                    ,
                   options,
                   0);   
            results = new ArrayValue(elements);
            return true;
        }

        public override string ToString()
        {
            return "FilterSelector";
        }
    }

} // namespace JsonCons.JsonPath

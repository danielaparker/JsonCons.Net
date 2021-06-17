using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using NUnit.Framework;

namespace JsonCons.JsonPathLib
{
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


} // namespace JsonCons.JsonPathLib

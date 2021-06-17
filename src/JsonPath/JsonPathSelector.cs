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
            TestContext.WriteLine("EvaluateTail...");
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
        }
    }

    public class WildcardSelector : BaseSelector
    {
        public override void Select(JsonElement root, 
                                    JsonElement current,
                                    IList<JsonElement> nodes)
        {
        }
    }


} // namespace JsonCons.JsonPathLib

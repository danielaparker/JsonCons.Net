using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.RegularExpressions;
using NUnit.Framework;
        
namespace JsonCons.JsonPathLib
{
    interface IFunction 
    {
        int? Arity {get;}
        bool TryEvaluate(IList<JsonElement> parameters, out JsonElement element);
    };

    abstract class BaseFunction : IFunction
    {
        internal BaseFunction(int? argCount)
        {
            Arity = argCount;
        }

        public int? Arity {get;}

        public abstract bool TryEvaluate(IList<JsonElement> parameters, out JsonElement element);
    };  

    class AbsFunction : BaseFunction
    {
        internal AbsFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(IList<JsonElement> args, out JsonElement element) 
        {
            if (this.Arity != null)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }

            var arg= args[0];
            if (arg.ValueKind != JsonValueKind.Number)
            {
                element = JsonConstants.Null;
            }

            StringBuilder builder = new StringBuilder(arg.ToString());
            if (arg.ToString().StartsWith("-"))
            {
                builder.Remove(0,1);
            }
            else
            {
                builder.Insert(0,'-');
            }
            element = JsonDocument.Parse(builder.ToString()).RootElement;
            return true;
        }

        public override string ToString()
        {
            return "abs";
        }
    };

    class BuiltInFunctions 
    {
        internal static BuiltInFunctions Instance {get;} = new BuiltInFunctions();

        Dictionary<string,IFunction> _functions = new Dictionary<string, IFunction>(); 

        internal BuiltInFunctions()
        {
            _functions.Add("abs", new AbsFunction());
        }

        internal bool TryGetFunction(string name, out IFunction func)
        {
            return _functions.TryGetValue(name, out func);
        }
    };

} // namespace JsonCons.JsonPathLib

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
        bool TryEvaluate(IList<IJsonValue> parameters, out IJsonValue element);
    };

    abstract class BaseFunction : IFunction
    {
        internal BaseFunction(int? argCount)
        {
            Arity = argCount;
        }

        public int? Arity {get;}

        public abstract bool TryEvaluate(IList<IJsonValue> parameters, out IJsonValue element);
    };  

    class AbsFunction : BaseFunction
    {
        internal AbsFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(IList<IJsonValue> args, out IJsonValue element) 
        {
            if (this.Arity.HasValue)
            {
                Debug.Assert(args.Count == this.Arity.Value);
            }

            var arg = args[0];

            Decimal decVal;
            double dblVal;

            if (arg.TryGetDecimal(out decVal))
            {
                element = new DecimalJsonValue(decVal >= 0 ? decVal : -decVal);
                return true;
            }
            else if (arg.TryGetDouble(out dblVal))
            {
                element = new DecimalJsonValue(dblVal >= 0 ? decVal : new Decimal(-dblVal));
                return true;
            }
            else
            {
                element = JsonConstants.Null;
                return false;
            }
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

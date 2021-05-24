using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonCons.JsonPathLib
{
    public class JsonPathExpression
    {
        public static JsonPathExpression CreateInstance(string expr)
        {
            return new JsonPathExpression();
        }
    }
}

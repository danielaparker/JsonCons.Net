using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using JsonCons.Utilities;
using System.Text.RegularExpressions;

namespace JsonCons.JsonSchema
{
    public struct ValidationOutput 
    {
        public string InstanceLocation {get;}
        public string Message {get;}
        public string Keyword {get;}
        public string AbsoluteKeywordLocation {get;}
        public IList<ValidationOutput> NestedErrors {get;}

        internal ValidationOutput(string keyword,
                                  string absoluteKeywordLocation,
                                  string instanceLocation,
                                  string message)
        {
            Keyword = keyword;
            AbsoluteKeywordLocation = absoluteKeywordLocation;
            InstanceLocation = instanceLocation;
            Message = message; 
            NestedErrors = new List<ValidationOutput>();
        }

        internal ValidationOutput(string keyword,
                                  string absoluteKeywordLocation,
                                  string instanceLocation,
                                  string message,
                                  IList<ValidationOutput> nestedErrors)
        {
            Keyword = keyword;
            AbsoluteKeywordLocation = absoluteKeywordLocation;
            InstanceLocation = instanceLocation;
            Message = message; 
            NestedErrors = nestedErrors;
        }
    }

    // Interface for validation error handlers
    abstract class ErrorReporter
    {
        int _errorCount;

        internal bool FailEarly {get;}
        internal int ErrorCount {get {return _errorCount;}}
   
        internal ErrorReporter(bool failEarly = false)
        {
            _errorCount = 0;
            FailEarly = failEarly;
        }

        internal void Error(ValidationOutput o)
        {
            ++_errorCount;
            OnError(o);
        }

        internal abstract void OnError(ValidationOutput e); 
    }

} // namespace JsonCons.JsonSchema

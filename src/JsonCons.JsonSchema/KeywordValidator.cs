using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using JsonCons.Utilities;
using System.Text.RegularExpressions;

#nullable enable        

namespace JsonCons.JsonSchema
{
    interface IKeywordValidator 
    {
    };

    class StringValidator : IKeywordValidator 
    {
        int? MaxLength {get;} = null;
        string? AbsoluteMaxLengthLocation {get;} = null;

        int? MinLength {get;} = null;
        string? AbsoluteMinLengthLocation {get;} = null;

        Regex? Pattern {get;} = null;
        string? PatternString {get;} = null;
        string? AbsolutePatternLocation {get;} = null;

        Action<string,UriWrapper,string,ErrorReporter>? FormatChecker {get;} = null; 
        string? AbsoluteFormatLocation {get;} = null;

        string? ContentEncoding {get;} = null;
        string? AbsoluteContentEncodingLocation {get;} = null;

        string? ContentMediaType {get;} = null;
        string? AbsoluteContentMediaTypeLocation {get;} = null;
    };


} // namespace JsonCons.JsonSchema

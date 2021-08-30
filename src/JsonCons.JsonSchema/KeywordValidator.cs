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
    abstract class KeywordValidator 
    {
        internal abstract void OnValidate(JsonElement instance, 
                                          SchemaLocation instanceLocation, 
                                          ErrorReporter reporter,
                                          ref JsonElement patch);
    };

    class StringValidator : KeywordValidator 
    {
        internal int? MaxLength {get;} = null;
        internal string? MaxLengthLocation {get;} = null;

        internal int? MinLength {get;} = null;
        internal string? MinLengthLocation {get;} = null;

        internal Regex? Pattern {get;} = null;
        internal string? PatternLocation {get;} = null;

        IFormatValidator? FormatValidator {get;} = null; 
        
        internal string? ContentEncoding {get;} = null;
        internal string? ContentEncodingLocation {get;} = null;

        internal string? ContentMediaType {get;} = null;
        internal string? ContentMediaTypeLocation {get;} = null;

        internal StringValidator(int? maxLength, string? maxLengthLocation,
                                 int? minLength, string? minLengthLocation,
                                 Regex? pattern, string? patternLocation,
                                 IFormatValidator? formatValidator, 
                                 string? contentEncoding, string? contentEncodingLocation,
                                 string? contentMediaType, string? contentMediaTypeLocation)
        {
            MaxLength = maxLength;
            MaxLengthLocation = maxLengthLocation;
            MinLength = minLength;
            MinLengthLocation = minLengthLocation;
            Pattern = pattern;
            PatternLocation = patternLocation;
            FormatValidator = formatValidator;
            ContentEncoding = contentEncoding;
            ContentEncodingLocation = contentEncodingLocation;
            ContentMediaType = contentMediaType;
            ContentMediaTypeLocation = contentMediaTypeLocation;
        }

        internal override void OnValidate(JsonElement instance,
                                          SchemaLocation instanceLocation,
                                          ErrorReporter reporter,
                                          ref JsonElement patch)
        {
        }

        internal static StringValidator Create(JsonElement schema, IList<SchemaLocation> uris)
        {
            int? maxLength = null;
            string? maxLengthLocation = null;
            int? minLength = null;
            string? minLengthLocation = null;
            Regex? pattern = null;
            string? patternLocation = null;
            IFormatValidator? formatValidator = null; 
            string? contentEncoding = null;
            string? contentEncodingLocation = null;
            string? contentMediaType = null;
            string? contentMediaTypeLocation = null;

            JsonElement element;
            if (schema.TryGetProperty("maxLength", out element))
            {   
                maxLength = element.GetInt32();
                maxLengthLocation = SchemaLocation.CreateAbsoluteKeywordLocation(uris, "maxLength");
            }
            if (schema.TryGetProperty("minLength", out element))
            {   
                minLength = element.GetInt32();
                minLengthLocation = SchemaLocation.CreateAbsoluteKeywordLocation(uris, "minLength");
            }

            if (schema.TryGetProperty("pattern", out element))
            {   
                string? patternString = element.GetString();
                pattern = new Regex(patternString);
                patternLocation = SchemaLocation.CreateAbsoluteKeywordLocation(uris, "pattern");
            }
            if (schema.TryGetProperty("format", out element))
            {   
                string? format = element.GetString();
                string formatLocation = SchemaLocation.CreateAbsoluteKeywordLocation(uris, "format");
                switch (format)
                {
                    case "date-time":
                        formatValidator = new DateTimeValidator(formatLocation);
                        break;
                    case "date":
                        formatValidator = new DateValidator(formatLocation);
                        break;
                    case "time":
                        formatValidator = new TimeValidator(formatLocation);
                        break;
                    case "email":
                        formatValidator = new EmailValidator(formatLocation);
                        break;
                    case "hostname":
                        formatValidator = new HostnameValidator(formatLocation);
                        break;
                    case "ipv4":
                        formatValidator = new Ipv4Validator(formatLocation);
                        break;
                    case "ipv6":
                        formatValidator = new Ipv6Validator(formatLocation);
                        break;
                    case "regex":
                        formatValidator = new RegexValidator(formatLocation, pattern);
                        break;
                    default:
                        break;
                }
                formatLocation = SchemaLocation.CreateAbsoluteKeywordLocation(uris, "format");
            }
            if (schema.TryGetProperty("contentEncoding", out element))
            {   
                contentEncoding = element.GetString();
                contentEncodingLocation = SchemaLocation.CreateAbsoluteKeywordLocation(uris, "contentEncoding");
            }
            if (schema.TryGetProperty("contentMediaType", out element))
            {   
                contentMediaType = element.GetString();
                contentMediaTypeLocation = SchemaLocation.CreateAbsoluteKeywordLocation(uris, "contentMediaType");
            }
            return new StringValidator(maxLength, maxLengthLocation,
                                       minLength, minLengthLocation,
                                       pattern, patternLocation,
                                       formatValidator,
                                       contentEncoding, contentEncodingLocation,
                                       contentMediaType, contentMediaTypeLocation);
        }
    }

} // namespace JsonCons.JsonSchema

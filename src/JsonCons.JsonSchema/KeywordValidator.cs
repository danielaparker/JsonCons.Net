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
            string? content = null;
            if (ContentEncoding != null)
            {
                if (ContentEncoding == "base64")
                {
                    string? s = instance.GetString();
                    try
                    {
                        content = Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
                    }
                    catch (Exception)
                    {
                        reporter.Error(new ValidationOutput("contentEncoding", 
                                                            ContentEncodingLocation, 
                                                            instanceLocation.ToString(), 
                                                            "Content is not a base64 string"));
                        if (reporter.FailEarly)
                        {
                            return;
                        }
                    }
                }
                else if (ContentEncoding.Length != 0)
                {
                    reporter.Error(new ValidationOutput("contentEncoding", 
                                                    ContentEncodingLocation,
                                                    instanceLocation.ToString(), 
                                                    $"Unable to check for contentEncoding '{ContentEncoding}'"));
                    if (reporter.FailEarly)
                    {
                        return;
                    }
                }
            }
            else
            {
                content = instance.GetString();
            }
            if (content == null)
            {
                return;
            }

            if (ContentMediaType != null) 
            {
                if (ContentMediaType.Equals("application/Json"))
                {
                    try
                    {
                        using JsonDocument doc = JsonDocument.Parse(content);
                    }
                    catch (Exception e)
                    {
                        reporter.Error(new ValidationOutput("contentMediaType", 
                                                            ContentMediaTypeLocation,
                                                            instanceLocation.ToString(), 
                                                            $"Content is not JSON: {e.Message}"));
                    }
                }
            } 

            if (instance.ValueKind != JsonValueKind.String) 
            {
                return; 
            }

            if (MinLength != null) 
            {
                byte[] bytes = Encoding.UTF32.GetBytes(content.ToCharArray());
                int length = bytes.Length/4;
                if (length < MinLength) 
                {
                    reporter.Error(new ValidationOutput("minLength", 
                                                    MinLengthLocation, 
                                                    instanceLocation.ToString(), 
                                                    $"Expected minLength: {MinLength}, actual: {length}"));
                    if (reporter.FailEarly)
                    {
                        return;
                    }
                }
            }

            if (MaxLength != null) 
            {
                byte[] bytes = Encoding.UTF32.GetBytes(content.ToCharArray());
                int length = bytes.Length/4;
                if (length > MaxLength)
                {
                    reporter.Error(new ValidationOutput("maxLength", 
                                                    MaxLengthLocation, 
                                                    instanceLocation.ToString(), 
                                                    $"Expected maxLength: {MaxLength}, actual: {length}"));
                    if (reporter.FailEarly)
                    {
                        return;
                    }
                }
            }

            if (Pattern != null)
            {
                var match = Pattern.Match(content);
                if (match.Success)
                {
                    reporter.Error(new ValidationOutput("pattern", 
                                                    PatternLocation, 
                                                    instanceLocation.ToString(), 
                                                    $"String '{content}' does not match pattern '{Pattern}'"));
                    if (reporter.FailEarly)
                    {
                        return;
                    }
                }
            }

            if (FormatValidator != null) 
            {
                FormatValidator.Validate(content, instanceLocation.ToString(), reporter);
                if (reporter.ErrorCount > 0 && reporter.FailEarly)
                {
                    return;
                }
            }
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
                        formatValidator = new RegexValidator(formatLocation);
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

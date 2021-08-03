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
    class UriWrapper
    {
        Uri _uri;
        string _identifier;
    
        internal UriWrapper(string uri)
        {
            var pos = uri.LastIndexOf('#');
            if (pos != -1)
            {
                _identifier = uri.Substring(pos + 1); 
                UnescapePercent(_identifier);
            }
            _uri = new Uri(uri);
        }

        Uri uri() 
        {
            return _uri;
        }

        bool HasJsonPointer() 
        {
            return _identifier.Length != 0 && _identifier[0] == '/';
        }

        bool HasIdentifier() 
        {
            return _identifier.Length != 0 && _identifier[0] != '/';
        }

        string PathAndQuery 
        {
            get {return _uri.PathAndQuery;}
        }

        bool IsAbsoluteUri 
        {
            get {return _uri.IsAbsoluteUri;}
        }

        string GetPointer() 
        {
            return _identifier;
        }

        string GetIdentifier() 
        {
            return _identifier;
        }

        string GetFragment() 
        {
            return _identifier;
        }

        UriWrapper Resolve( UriWrapper& uri) 
        {
            UriWrapper new_uri = new UriWrapper();
            new_uri._identifier = _identifier;
            new_uri._uri = _uri.TryCreate(uri._uri);
            return new_uri;
        }

        int Compare( UriWrapper& other) 
        {
            int result = _uri.compare(other._uri);
            if (result != 0) 
            {
                return result;
            }
            return result; 
        }

        UriWrapper append(string field) 
        {
            if (HasIdentifier())
                return *this;

            jsoncons::jsonpointer::json_pointer pointer(string(_uri.GetFragment()));
            pointer /= field;

            Uri new_uri(_uri.scheme(),
                                  _uri.userinfo(),
                                  _uri.host(),
                                  _uri.port(),
                                  _uri.PathAndQuery,
                                  _uri.query(),
                                  pointer.to_string());

            UriWrapper wrapper;
            wrapper._uri = new_uri;
            wrapper._identifier = pointer.to_string();

            return wrapper;
        }

        UriWrapper append(std::size_t index) 
        {
            if (HasIdentifier())
                return *this;

            jsoncons::jsonpointer::json_pointer pointer(string(_uri.GetFragment()));
            pointer /= index;

            Uri new_uri(_uri.scheme(),
                                  _uri.userinfo(),
                                  _uri.host(),
                                  _uri.port(),
                                  _uri.PathAndQuery,
                                  _uri.query(),
                                  pointer.to_string());

            UriWrapper wrapper;
            wrapper._uri = new_uri;
            wrapper._identifier = pointer.to_string();

            return wrapper;
        }

        string string() 
        {
            string s = _uri.string();
            return s;
        }

        friend bool operator==( UriWrapper& lhs,  UriWrapper& rhs)
        {
            return lhs.compare(rhs) == 0;
        }

        friend bool operator!=( UriWrapper& lhs,  UriWrapper& rhs)
        {
            return lhs.compare(rhs) != 0;
        }

        friend bool operator<( UriWrapper& lhs,  UriWrapper& rhs)
        {
            return lhs.compare(rhs) < 0;
        }

        friend bool operator<=( UriWrapper& lhs,  UriWrapper& rhs)
        {
            return lhs.compare(rhs) <= 0;
        }

        friend bool operator>( UriWrapper& lhs,  UriWrapper& rhs)
        {
            return lhs.compare(rhs) > 0;
        }

        friend bool operator>=( UriWrapper& lhs,  UriWrapper& rhs)
        {
            return lhs.compare(rhs) >= 0;
        }
    private:
        static void UnescapePercent(string& s)
        {
            if (s.size() >= 3)
            {
                std::size_t pos = s.size() - 2;
                while (pos-- >= 1)
                {
                    if (s[pos] == '%')
                    {
                        string hex = s.substr(pos + 1, 2);
                        char ch = (char) std::strtoul(hex.c_str(), nullptr, 16);
                        s.replace(pos, 3, 1, ch);
                    }
                }
            }
        }
    };

    // Interface for validation error handlers
    class error_reporter
    {
        bool fail_early_;
        std::size_t error_count_;
    public:
        error_reporter(bool fail_early = false)
            : fail_early_(fail_early), error_count_(0)
        {
        }

        virtual ~error_reporter() = default;

        void error( validation_output& o)
        {
            ++error_count_;
            do_error(o);
        }

        std::size_t error_count() 
        {
            return error_count_;
        }

        bool fail_early() 
        {
            return fail_early_;
        }

    private:
        virtual void do_error( validation_output& /* e */) = 0;
    };


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

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using JsonCons.Utilities;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("JsonCons.JsonSchema.Tests")]

namespace JsonCons.JsonSchema
{
    class SchemaLocation2
    {
        string _scheme = "";
        string _authority = "";
        string _pathAndQuery = "";

        JsonPointer _pointer = null;
        string _identifier = null;

        private static readonly Uri _empty = new Uri("http://JsonCons/");

        internal SchemaLocation2(string scheme, string authority, string pathAndQuery, JsonPointer pointer)
        {
            _scheme = scheme;
            _authority = authority;
            _pathAndQuery = pathAndQuery;
            _pointer = pointer;
        }

        internal SchemaLocation2(string uri)
        {
            //Debug.WriteLine($"From string: {uri}");

            int pos = uri.IndexOf('#');
            if (pos != -1)
            {
                if (pos < uri.Length)
                {
                    string s = pos+1 < uri.Length ? Uri.UnescapeDataString(uri.Substring(pos+1)) : "";
                    if (s.Length > 0 && s[0] != '/')
                    {
                        _identifier = s;
                    }
                    else
                    {
                        _pointer = JsonPointer.Parse(uri.Substring(pos));
                    }
                }
            }

            var location = new Uri(uri,UriKind.RelativeOrAbsolute);
            bool isAbsoluteUri = location.IsAbsoluteUri;
            if (!isAbsoluteUri)
            {
                if (pos != -1)
                {
                    _pathAndQuery = uri.Substring(0,pos);
                }
                else
                {
                    _pathAndQuery = uri;
                }
            }
            else
            {
                _pathAndQuery = location.PathAndQuery;
                _authority = location.Authority;
                _scheme = location.Scheme;
            }
        }

        internal string Identifier {get {return _identifier;}}

        internal JsonPointer Pointer {get {return _pointer;}}

        internal string Scheme {get {return _scheme;}}

        internal string PathAndQuery {get {return _pathAndQuery;}}

        internal string Authority {get {return _authority;}}

        internal string Fragment {get 
            {
                if (HasIdentifier)
                {
                    return "#" + Identifier;
                }
                else if (HasPointer)
                {
                    return Pointer.ToString();
                }
                else
                {
                    return "";
                }
            }
        }

        internal bool IsAbsoluteUri 
        {
            get {return !String.IsNullOrEmpty(_scheme);}
        }

        internal bool HasIdentifier
        {
            get {return _identifier != null;}
        }

        internal bool HasPointer
        {
            get {return _pointer != null;}
        }

        internal bool TryGetPointer(out JsonPointer pointer) 
        {
            pointer = _pointer;
            return HasPointer ? true : false;
        }

        internal bool TryGetIdentifier(out string identifier) 
        {
            identifier = _identifier;
            return HasIdentifier ? true : false;
        }

        internal static SchemaLocation2 Resolve(SchemaLocation2 baseUri, SchemaLocation2 relativeUri) 
        {
            Uri resolved = new Uri(new Uri(baseUri.ToString()), relativeUri.ToString()); 
            return new SchemaLocation2(resolved.ToString());
        }

        internal static SchemaLocation2 Append(SchemaLocation2 uri, string name) 
        {
            if (uri.HasIdentifier)
                return uri;

            var tokens = new List<string>();
            if (uri.HasPointer)
            {
                foreach (var token in uri.Pointer.Tokens)
                {
                    tokens.Add(token);
                }
            }
            tokens.Add(name);
            var pointer2 = new JsonPointer(tokens);

            return new SchemaLocation2(uri.Scheme, uri.Authority, uri.PathAndQuery, new JsonPointer(tokens));
        }

        internal static SchemaLocation2 Append(SchemaLocation2 uri, int index) 
        {
            if (uri.HasIdentifier)
                return uri;

            var tokens = new List<string>();
            if (uri.HasPointer)
            {
                var pointer = uri.Pointer;
                foreach (var token in pointer.Tokens)
                {
                    tokens.Add(token);
                }
            }
            tokens.Add(index.ToString());

            return new SchemaLocation2(uri.Scheme, uri.Authority, uri.PathAndQuery, new JsonPointer(tokens));
        }

        public string Location()
        {
            var builder = new StringBuilder();
            if (!String.IsNullOrEmpty(_scheme))
            {
                builder.Append(_scheme);
                builder.Append("://");
            }
            if (!String.IsNullOrEmpty(_authority))
            {
                builder.Append(_authority);
            }
            builder.Append(_pathAndQuery);
            return builder.ToString();
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            if (!String.IsNullOrEmpty(_scheme))
            {
                builder.Append(_scheme);
                builder.Append("://");
            }
            builder.Append(_authority);
            builder.Append(_pathAndQuery);

            if (HasIdentifier || HasPointer)
            {
                builder.Append("#");
                if (HasPointer)
                {
                    builder.Append(Pointer.ToString());
                }
                else if (HasIdentifier)
                {
                    builder.Append(_identifier);
                }
            }
            return builder.ToString();
        }

        internal string GetPathAndQuery()
        {
            return _pathAndQuery;
        }

        internal static SchemaLocation2 GetAbsoluteKeywordLocation(IList<SchemaLocation2> uris)
        {
            foreach (var item in uris)
            {
                if (!item.HasIdentifier && item.IsAbsoluteUri)
                {
                    return item;
                }
            }
            return new SchemaLocation2("#");
        }
    }

} // namespace JsonCons.JsonSchema

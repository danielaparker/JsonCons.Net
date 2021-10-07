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
    class SchemaLocation
    {
        Uri _location = null;
        string _pointer = null;
        string _identifier = null;

        internal SchemaLocation(string uri)
        {
            int pos = uri.IndexOf('#');
            if (pos != -1)
            {
                string s = Uri.UnescapeDataString(uri.Substring(pos+1));
                if (s.Length > 0 && s[0] == '/')
                {
                    _pointer = s;
                }
                else
                {
                    _identifier = s;
                }

                string location = uri.Substring(0, pos);
                if (location.Length > 0)
                {
                    _location = new Uri(location);
                }
            }
            else
            {
                _location = new Uri(uri);
            }
        }
        internal SchemaLocation(Uri uri)
        {
            _location = uri;
        }

        internal Uri Uri {get {return _location;}}

        internal string Identifier {get {return _identifier;}}

        internal string Pointer {get {return _pointer;}}

        internal string Scheme {get {return Uri.Scheme;}}

        internal string Fragment {get {return Uri.Fragment;}}

        internal bool IsAbsoluteUri 
        {
            get {return _location != null && Uri.IsAbsoluteUri;}
        }

        internal string AbsolutePath
        {
            get {return Uri.AbsolutePath;}
        }

        internal bool HasIdentifier
        {
            get {return _identifier != null;}
        }

        internal bool HasJsonPointer
        {
            get {return _pointer != null;}
        }

        internal bool TryGetPointer(out string pointer) 
        {
            pointer = _pointer;
            return HasJsonPointer ? true : false;
        }

        internal bool TryGetIdentifier(out string identifier) 
        {
            identifier = _identifier;
            return HasIdentifier ? true : false;
        }

        internal static SchemaLocation Resolve(SchemaLocation baseUri, SchemaLocation relativeUri) 
        {
            Uri newUri;
            if (Uri.TryCreate(baseUri.Uri, relativeUri.Uri, out newUri))
            {
            }
            return new SchemaLocation(newUri);
        }

        internal static SchemaLocation Append(SchemaLocation uri, string field) 
        {
            if (uri.HasIdentifier)
                return new SchemaLocation(uri.Uri);

            JsonPointer pointer = JsonPointer.Parse(uri.Fragment);
            var tokens = new List<string>();
            foreach (var token in pointer.Tokens)
            {
                tokens.Add(token);
            }
            tokens.Add(field);

            string newUri = uri.Uri.GetLeftPart(UriPartial.Query) + "#" + pointer.ToString();

            return new SchemaLocation(newUri);
        }

        internal static SchemaLocation Append(SchemaLocation uri, int index) 
        {
            if (uri.HasIdentifier)
                return new SchemaLocation(uri.Uri);

            JsonPointer pointer = JsonPointer.Parse(uri.Fragment);
            var tokens = new List<string>();
            foreach (var token in pointer.Tokens)
            {
                tokens.Add(token);
            }
            tokens.Add(index.ToString());

            string newUri = uri.Uri.GetLeftPart(UriPartial.Query) + "#" + pointer.ToString();

            return new SchemaLocation(newUri);
        }

        public override string ToString()
        {
            if (_location != null)
            {
                return _location.ToString();
            }
            else if (_pointer != null)
            {
                return _pointer;
            }
            else if (_identifier != null)
            {
                return _identifier;
            }
            else
            {
                return "";
            }
        }

        internal string GetPathAndQuery()
        {
            return Uri.GetComponents(UriComponents.PathAndQuery, UriFormat.UriEscaped);
        }

        internal bool Equals(SchemaLocation uri)
        {
            return Uri.Equals(uri);
        }

        internal int Compare(SchemaLocation uri)
        {
            return Uri.Compare(Uri, uri.Uri, UriComponents.AbsoluteUri, UriFormat.UriEscaped, 
                               StringComparison.Ordinal); 
        }

        public override int GetHashCode()
        {
            return Uri.GetHashCode();
        }

        internal static SchemaLocation GetAbsoluteKeywordLocation(IList<SchemaLocation> uris)
        {
            foreach (var item in uris)
            {
                if (!item.HasIdentifier && item.IsAbsoluteUri)
                {
                    return item;
                }
            }
            return new SchemaLocation("#");
        }
    }

} // namespace JsonCons.JsonSchema

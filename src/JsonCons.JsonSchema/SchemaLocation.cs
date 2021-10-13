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

    class SchemaLocation
    {
        Uri _location = null;
        string _pointer = null;
        string _identifier = null;

        internal SchemaLocation(string uri)
        {
            //Debug.WriteLine($"From string: {uri}");

            _location = new Uri(uri,UriKind.RelativeOrAbsolute);

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
                        _pointer = uri.Substring(pos);
                    }
                }

                string location = uri.Substring(0, pos);
            }
        }
        internal SchemaLocation(Uri uri)
        {
            //Debug.WriteLine($"From URI: {uri}");

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

        internal bool HasPointer
        {
            get {return _pointer != null;}
        }

        internal bool TryGetPointer(out string pointer) 
        {
            pointer = _pointer;
            return HasPointer ? true : false;
        }

        internal bool TryGetIdentifier(out string identifier) 
        {
            identifier = _identifier;
            return HasIdentifier ? true : false;
        }

        internal static SchemaLocation Resolve(SchemaLocation baseUri, SchemaLocation relativeUri) 
        {
            Uri newUri;
            Debug.WriteLine($"base: {baseUri.ToString()} relative: {relativeUri.ToString()}");
            if (Uri.TryCreate(baseUri.Uri, relativeUri.Uri, out newUri))
            {
            }
            return new SchemaLocation(newUri);
        }

        internal static SchemaLocation Append(SchemaLocation uri, string field) 
        {
            if (uri.HasIdentifier)
                return uri;

            var tokens = new List<string>();
            if (uri.HasPointer)
            {
                var pointer = JsonPointer.Parse(uri.Pointer);
                foreach (var token in pointer.Tokens)
                {
                    tokens.Add(token);
                }
            }
            tokens.Add(field);
            var pointer2 = new JsonPointer(tokens);

            Debug.WriteLine("GetLeftPart");
            string newUri = uri.Uri.GetLeftPart(UriPartial.Query) + pointer2.ToUriFragment();
            Debug.WriteLine("GetLeftPart done");

            return new SchemaLocation(newUri);
        }

        internal static SchemaLocation Append(SchemaLocation uri, int index) 
        {
            if (uri.HasIdentifier)
                return new SchemaLocation(uri.Uri);

            var tokens = new List<string>();
            if (uri.HasPointer)
            {
                var pointer = JsonPointer.Parse(uri.Pointer);
                foreach (var token in pointer.Tokens)
                {
                    tokens.Add(token);
                }
            }
            tokens.Add(index.ToString());
            var pointer2 = new JsonPointer(tokens);

            Debug.WriteLine("GetLeftPart");
            string newUri= uri.Uri.GetLeftPart(UriPartial.Query) + pointer2.ToUriFragment();
            Debug.WriteLine("GetLeftPart done");

            return new SchemaLocation(newUri);
        }

        public override string ToString()
        {
            return _location.ToString();
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
            //return Uri.Compare(Uri, uri.Uri, UriComponents.AbsoluteUri, UriFormat.UriEscaped, 
            //                   StringComparison.Ordinal); 

            return uri.ToString().CompareTo(Uri.ToString());
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

        internal static bool IsRooted(string basepath)
        {
            if (!string.IsNullOrEmpty(basepath) && (basepath[0] != '/'))
                return (basepath[0] == '\\');

            return true;
        }

        internal static bool IsRelativeUri(string virtualPath)
        {
            if (virtualPath.IndexOf(":") != -1)
                return false;

            return !IsRooted(virtualPath);
        }    }

} // namespace JsonCons.JsonSchema

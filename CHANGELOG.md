v1.1.0

Enhancements:

- Added JsonCons.JmesPath library that implements JMESPath

Bugs fixed:

- Fixed issue with JsonPath filter comparison operators
`>`, `>=`, and `<=` when applied to strings.

v1.0.0

Move from prerelease to release

v1.0.0.preview.2

Enhancements to JsonCons.Utilities:

- Added `JsonMergePatch.FromDiff` for creating a JSON Merge Patch
from source and target JSON documents

Changes to JsonCons.JsonPath:

- class JsonPathOptions renamed to JsonSelectorOptions
- property JsonSelectorOptions.SortBy replaced by 
JsonSelectorOptions.Sort, which is a boolean value that indicates 
whether to sort results by normalized paths
- JsonPathExecutionMode renamed to PathExecutionMode

Defect fixes for JsonCons.JsonPath:

- Unquoted strings now allow all characters in the range
%x80-10FFFF, as per specification.




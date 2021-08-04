main

Enhancements to JsonCons.Utilities:

- Added `JsonMergePatch.FromDiff` for creating a JSON Merge Patch
from source and target JSON documents

v1.0.0.preview.2

Changes to JsonCons.JsonPath:

- class JsonPathOptions renamed to JsonSelectorOptions
- property JsonSelectorOptions.SortBy replaced by 
JsonSelectorOptions.Sort, which is a boolean value that indicates 
whether to sort results by normalized paths
- JsonPathExecutionMode renamed to PathExecutionMode

Defect fixes for JsonCons.JsonPath:

- Unquoted strings now allow all characters in the range
%x80-10FFFF, as per specification.




# JsonCons.JmesPath

The JsonCons.JmesPath library complements the functionality of the 
[System.Text.Json namespace](https://docs.microsoft.com/en-us/dotnet/api/system.text.json?view=netcore-3.1)
with an implementation of JMESPath. It provides support for querying
JsonDocument/JsonElement instances with code like this:
```csharp
var searcher = JsonSearcher.Parse("people[?age > `20`].[name, age]");

JsonDocument result = searcher.Search(doc.RootElement);
```
It targets .Net Standard 2.1.

Documentation and examples may be found here:

- [Reference](https://danielaparker.github.io/JsonCons.Net/ref/JsonCons.JmesPath.html)

- [Code examples](https://github.com/danielaparker/JsonCons.Net/blob/main/examples/JmesPath.Examples/JmesPathExamples.cs)



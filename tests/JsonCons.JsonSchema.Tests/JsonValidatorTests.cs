using System.Text.Json;
using JsonCons.JsonSchema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JsonCons.JsonSchema.Tests
{
	public class JsonValidatorTests
	{
       [TestMethod]
       public void Test1()
       {
           using var schema = JsonDocument.Parse(@"
{
  ""$id"": ""https://example.com/polygon"",
  ""$schema"": ""http://json-schema.org/draft-07/schema#"",
  ""$defs"": {
    ""point"": {
      ""type"": ""object"",
      ""properties"": {
        ""x"": { ""type"": ""number"" },
        ""y"": { ""type"": ""number"" }
      },
      ""additionalProperties"": false,
      ""required"": [ ""x"", ""y"" ]
    }
  },
  ""type"": ""array"",
  ""items"": { ""$ref"": ""#/$defs/point"" },
  ""minItems"": 3,
  ""maxItems"": 1
}
            ");

           using var instance = JsonDocument.Parse(@"
[
  {
    ""x"": 2.5,
    ""y"": 1.3
  },
  {
    ""x"": 1,
    ""z"": 6.7
  }
]
            ");

            JsonValidator validator = JsonValidator.Create(schema.RootElement);
       }
   }
}

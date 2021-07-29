using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonCons.Utilities;
using JsonCons.JsonPath;

namespace JsonCons.JsonPath.Tests
{
    class Book 
    {
        public string Title {get;}
        public string Author {get;}
        public string Category {get;}

        internal Book(string title, string author, string category)
        {
            Title = title;
            Author = author;
            Category = category;
        }
    };

    [TestClass]
    public class JsonSelectorParallelExecutionTests
    {
        [TestMethod]
        public void Test()
        {
            var books = new List<Book>();
            for (int i = 0; i < 1000; ++i)
            {
                string category;
                switch (i % 8)
                {
                    case 0:
                        category = "Fiction";
                        break;
                    case 1:
                        category = "Poetry";
                        break;
                    case 2:
                        category = "Fantasy";
                        break;
                    case 3:
                        category = "ScienceFiction";
                        break;
                    case 4:
                        category = "Mystery";
                        break;
                    case 5:
                        category = "Biography";
                        break;
                    case 6:
                        category = "Drama";
                        break;
                    default:
                        category = "Nonfiction";
                        break;
                }
                books.Add(new Book($"Title{i}", $"Author{i}", category));
            }
            string jsonString = JsonSerializer.Serialize(books);
            var doc = JsonDocument.Parse(jsonString);

            var selector = JsonSelector.Parse(@"$[?@.Category=='Fiction',
                                                  ?@.Category=='Poetry',                                                 
                                                  ?@.Category=='Fantasy',
                                                  ?@.Category=='ScienceFiction',
                                                  ?@.Category=='Mystery',
                                                  ?@.Category=='Biography',
                                                  ?@.Category=='Drama',
                                                  ?@.Category=='Nonfiction'
                                                ]");

            IList<JsonElement> results1 = selector.Select(doc.RootElement, new JsonSelectorOptions{ExecutionMode = JsonSelectorExecutionMode.Sequential});

            var serializerOptions = new JsonSerializerOptions() { WriteIndented = true };
            Debug.WriteLine($"{JsonSerializer.Serialize(doc, serializerOptions)}\n");

            IList<JsonElement> results2 = selector.Select(doc.RootElement, new JsonSelectorOptions{ExecutionMode = JsonSelectorExecutionMode.Parallelized});

            System.Collections.ArrayList.Adapter((System.Collections.IList)results1).Sort(new JsonElementComparer());
            System.Collections.ArrayList.Adapter((System.Collections.IList)results2).Sort(new JsonElementComparer());
            //Debug.WriteLine($"{JsonSerializer.Serialize(results2, serializerOptions)}\n");

            Assert.IsTrue(Enumerable.SequenceEqual(results1, results2, JsonElementEqualityComparer.Instance));
        }
    }
}

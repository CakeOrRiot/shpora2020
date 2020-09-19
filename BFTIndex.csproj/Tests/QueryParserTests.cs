using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace BFTIndex.Tests
{
    class QueryParserTests
    {
        TextParser parser;

        [SetUp]
        public void SetUp()
        {
            parser = new TextParser();
        }

        public void Test(IEnumerable<string> expected, string query)
        {
            Assert.AreEqual(expected, parser.GetAllAllowedWords(query));
        }

        [TestCase("not word")]
        [TestCase("not not word")]
        [TestCase("not not not word")]
        public void ManyNots(string query)
        {
            Test(new List<string>() { "not word" }, query);

        }

        [TestCase("word not word not not parser")]
        public void GetAllNotWords(string query)
        {
            Assert.AreEqual(new HashSet<string>() { "word", "parser" },
                parser.GetAllNotWords(query).ToHashSet<string>());
        }
    }
}

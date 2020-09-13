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
        QueryParser parser;

        [SetUp]
        public void SetUp()
        {
            parser = new QueryParser();
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
    }
}

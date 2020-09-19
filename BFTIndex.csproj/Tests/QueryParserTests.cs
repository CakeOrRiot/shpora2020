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

        [TestCase("word not word not not parser")]
        [TestCase("word not word not parser")]
        [TestCase("not word not word not parser")]
        public void GetAllNotWords(string query)
        {
            Assert.AreEqual(new HashSet<string>() { "word", "parser" },
                parser.GetAllNotWords(query).ToHashSet<string>());
        }

        [TestCase("word \"print,,,\" another,. \"apple\" ")]
        [TestCase("word \"print,,,\" another,. \"apple\" ")]
        [TestCase("word \"print,,,\" another,.   \"apple\" ")]
        //[TestCase("word not word not parser")]
        //[TestCase("not word not word not parser")]
        public void GetAllPhrasesSingleWords(string query)
        {
            Assert.AreEqual(new HashSet<string>() { "print,,,", "apple" },
                parser.GetAllPhrases(query).ToHashSet<string>());
        }

        [TestCase("word \"print,,open;;; door\" another,. \"apple\" ")]
        public void GetAllPhrasesMultiWords(string query)
        {
            Assert.AreEqual(new HashSet<string>() { "print,,open;;; door", "apple" },
                parser.GetAllPhrases(query).ToHashSet<string>());
        }

        [TestCase("word \"print,,open;;; door\" another,. \"apple\" \"")]
        public void GetAllPhrasesAdditionalQuote(string query)
        {
            Assert.AreEqual(new HashSet<string>(), null);
        }

    }
}

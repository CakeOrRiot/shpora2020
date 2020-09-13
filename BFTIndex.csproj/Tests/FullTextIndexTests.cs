using NUnit.Framework;
using System.Collections.Generic;
using BFTIndex.Models;
using System.Linq;
using System.ComponentModel.Design;
using NUnit.Framework.Internal;

namespace BFTIndex.Tests
{
    class FullTextIndexTests
    {
        private IFullTextIndex fullTextIndex;
        [SetUp]
        public void SetUp()
        {
            fullTextIndex = new FullTextIndexFactory().Create();
        }

        private void TestSearch(string[] expectedIds, MatchedDocument[] searchResult)
        {
            var ids = searchResult.Select(doc => doc.Id).OrderBy(id => id).ToArray();
            expectedIds = expectedIds.OrderBy(id => id).ToArray();
            Assert.AreEqual(expectedIds, ids);
        }

        [TestCase("ds")]
        [TestCase("abs.ds")]
        [TestCase("abs.ds,,oqpe")]
        [TestCase("abs.ds,,oqpe")]
        [TestCase("abs.dS,!!,OqPe")]
        public void ShouldIgnoreNonWordCharacters(string query)
        {
            fullTextIndex.AddOrUpdate("1", "abs ds...sad  .... fds fa ,.das/oqpe[ ewq[ [[]]");
            fullTextIndex.AddOrUpdate("2", "");
            var expected = new string[] { "1" };
            TestSearch(expected, fullTextIndex.Search(query));
        }

        [TestCase("")]
        public void EmptyText(string query)
        {
            var result = new MatchedDocument[0];
            Assert.AreEqual(fullTextIndex.Search(query), result);
        }
        [Test]
        public void UpdateTest()
        {
            var expected = new string[0];
            TestSearch(expected, fullTextIndex.Search("gfgf"));
            fullTextIndex.AddOrUpdate("2", "gfgf");
            expected = new string[] { "2" };
            TestSearch(expected, fullTextIndex.Search("gfgf"));
        }

        [Test]
        public void AddTest()
        {
            var expected = new MatchedDocument[0];
            Assert.AreEqual(fullTextIndex.Search("321"), expected);
            fullTextIndex.AddOrUpdate("3", "321...");
            fullTextIndex.AddOrUpdate("4", "341...");
            expected = new MatchedDocument[] { new MatchedDocument("3", 0) };
            Assert.AreEqual(expected, fullTextIndex.Search("321"));
        }

        [TestCase("РАЗ ДВА")]
        [TestCase("раз два")]
        [TestCase("..раз! два.")]
        [TestCase("раз-два")]
        public void OrdinaryTest(string query)
        {
            fullTextIndex.AddOrUpdate("1", "раз три два");
            fullTextIndex.AddOrUpdate("2", "рАз-два");
            fullTextIndex.AddOrUpdate("7", "раз-..два..");
            fullTextIndex.AddOrUpdate("10", "раЗ-..два..");
            fullTextIndex.AddOrUpdate("11", "раЫЗ-..два..");
            var expected = new string[] { "1", "10", "2", "7" };

            TestSearch(expected, fullTextIndex.Search(query));
        }
    }

    //class GetWordsTests
    //{
    //    public FullTextIndex index = new FullTextIndex();
    //    private void Test(IEnumerable<string> expected, string query)
    //    {
    //        Assert.AreEqual(expected, index.GetWords(query));
    //    }

    //    [Test]
    //    public void OneSplitter()
    //    {
    //        Test(new List<string>() { "word1", "word2", "word3" }, "word1,word2,word3");
    //    }

    //    [Test]
    //    public void OnlySplitters()
    //    {
    //        Test(new List<string>(), "..,.,,../[]][]``");
    //    }

    //    [Test]
    //    public void OrdinaryTest1()
    //    {
    //        Test(new List<string>() { "Раз", "два", "три" }, "Раз - два, три.");
    //    }

    //    [Test]
    //    public void DelimetersBeforeWords()
    //    {
    //        Test(new List<string>() { "слово", "ЕщЁСЛОВО" }, "...слово..ЕщЁСЛОВО");
    //    }
    //}

    class NormalizerTests
    {
        private Normalizer defaultNormalizer;
        private Dictionary<char, char> defaultTable = new Dictionary<char, char>
        {
            ['ё'] = 'е',
            ['ю'] = 'у'
        };

        [SetUp]
        public void SetUp()
        {
            defaultNormalizer = new Normalizer(defaultTable);
        }

        [TestCase("фыВиаёауые../", "фывиаеауые../")]
        [TestCase("ааа", "ааа")]
        [TestCase("ёёё", "еее")]
        [TestCase("ёЁёА", "еееа")]
        [TestCase("ёЁёюАУУУ", "еееуаууу")]
        public void DefaultNormalization(string qu, string exp)
        {
            Assert.AreEqual(exp, defaultNormalizer.Normalize(qu));
        }
    }

    class StopWordsFilterTests
    {
        private StopWordsFilter stopWordsFilter;
        private List<string> stopWords = new List<string>() { "и", "а", "слово" };
        private Normalizer normalizer = new Normalizer();
        [SetUp]
        public void SetUp()
        {
            stopWordsFilter = new StopWordsFilter(new HashSet<string>(stopWords), normalizer);
        }

        [TestCase("случайные. слова. здесьь авф..", true)]
        [TestCase("ТуТТОжЕЕ", true)]
        [TestCase("СЛОВА", true)]
        public void NoStopWords(string query, bool expected)
        {
            Assert.AreEqual(expected, stopWordsFilter.IsAllowedWord(query));
        }

        [TestCase("слово", false)]
        [TestCase("и", false)]
        public void OnlyStopWords(string query, bool expected)
        {
            Assert.AreEqual(expected, stopWordsFilter.IsAllowedWord(query));
        }

        [TestCase("слово.", true)]
        [TestCase("сЛово.", true)]
        [TestCase("СЛОВо.", true)]
        [TestCase("[]СЛОВо.", true)]
        [TestCase("[]А.", true)]
        public void StopWordsWithDelimeters(string query, bool expected)
        {
            Assert.AreEqual(expected, stopWordsFilter.IsAllowedWord(query));
        }
    }
}

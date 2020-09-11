using NUnit.Framework;
using BFTIndex;
using System.Collections.Generic;

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

        [TestCase("", "")]
        public void EmptyNormalizationTable(string qu, string exp)
        {
            //Assert.AreEqual(exp, fullTextIndex.)
        }

    }

    class NormalizationTests
    {
        private readonly Dictionary<char, char> defaultTable = new Dictionary<char, char>
        {
            ['е'] = 'ё'
        };

        private readonly Dictionary<char, char> randomTable = new Dictionary<char, char>
        {
            ['а'] = 'б',
            ['у'] = 'з',
            ['й'] = 'р',
            ['ф'] = 'е'
        };

        private readonly List<Dictionary<char, char>> tables = new List<Dictionary<char, char>>();
        [SetUp]
        public void SetUp()
        {
            //tables.Add 
        }

        [TestCase("", "")]
        public void EmptyNormalizationTable(string qu, string exp)
        {
            Assert.True(true);
            //Assert.AreEqual(exp, fullTextIndex.)
        }
    }
}

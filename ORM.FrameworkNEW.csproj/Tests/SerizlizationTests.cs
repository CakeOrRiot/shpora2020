using NUnit.Framework;
using ORM.Contracts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ORM.Tests
{
    class SerizlizationTests
    {
        private Serializer serializer;
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer();
        }

        [Test]
        public void TestCulture()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("ru-RU");
            var book = new Book()
            {
                Id = "1",
                Author = "YA",
                Time = DateTime.Parse("02/02/10 00:00:00")
            };
            Assert.AreEqual(book, serializer.Deserialize<Book>(serializer.Serialize<Book>(book)));
        }
    }
}

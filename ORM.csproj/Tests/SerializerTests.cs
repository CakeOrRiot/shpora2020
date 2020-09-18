using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ORM.Contracts;

namespace ORM.Tests
{
    class SerializerTests
    {
        private Serializer<Book> serializer;
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer<Book>();
        }

        public void TestInverse(Book obj)
        {
            var res = serializer.Deserialize(serializer.Serialize(obj));
            Assert.AreEqual(obj, res);
        }

        [Test]
        public void EscapeSymbols()
        {
            var obj = new Contracts.Book
            {
                Id = "1",
                Title = @"Fighters\,Guild\; History\, 1st\= Ed\\",
                Price = 75,
                Weight = 1,
                Author = "Anonymous",
                Skill = "Heavy Armor"
            };
            TestInverse(obj);
        }
    }
}

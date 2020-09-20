using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ORM.Contracts;
using ORM.Db;

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
            var obj = new Book
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

        [Test]
        public void OptimizationDefaultValueTest()
        {
            var obj = new Book
            {
                Id = "1",
                Title = @"df",
                Author = @"fdfas",
                Price = 0
            };
            Assert.AreEqual("Id=1,Title=df,Author=fdfas;", serializer.Serialize(obj));
        }

        [Test]
        public void OptimizationUpdateTest()
        {
            var obj = new Book
            {
                Id = "1",
                Title = @"df",
                Author = @"fdfas",
                Price = 0
            };
            var dbContext = new DataContext(new DbEngine());
            dbContext.Insert(obj);
            dbContext.SubmitChanges();
            dbContext.Insert(new Book() { Id = "2" });
            dbContext.SubmitChanges();
            Assert.AreEqual("Id=1;", serializer.Serialize(obj));
        }
    }
}

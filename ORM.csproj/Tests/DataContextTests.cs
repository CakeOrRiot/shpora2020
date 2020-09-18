using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ORM.Tests
{
    class DataContextTests
    {
        private DataContext dataContext;

        [SetUp]
        public void SetUp()
        {
            dataContext = new DataContext(new Db.DbEngine());

        }

        [Test]
        public void FindExistingItem()
        {
            var obj = new Contracts.Book();
            obj.Id = "1";
            dataContext.Insert(obj);
            dataContext.SubmitChanges();
            Assert.AreEqual(obj, dataContext.Find("1"));
        }

        [Test]
        public void FindNotExistingItem()
        {
            var obj = new Contracts.Book
            {
                Id = "2"
            };
            dataContext.Insert(obj);
            dataContext.SubmitChanges();
            Assert.AreEqual(null, dataContext.Find("1"));
        }

        [Test]
        public void FindWithEscapeSymbols()
        {
            var obj = new Contracts.Book
            {
                Id = "1",
                Title = "df,\\",
                Author = @"fdfas"
            };
            dataContext.Insert(obj);
            dataContext.SubmitChanges();
            Assert.AreEqual(obj, dataContext.Find("1"));
        }

        [Test]
        public void FindWithManySlashes()
        {
            var obj = new Contracts.Book
            {
                Id = "1",
                Title = @"abc\\\\\\\\\\",
                Author = "asgd"
            };
            dataContext.Insert(obj);
            dataContext.SubmitChanges();
            Assert.AreEqual(obj, dataContext.Find("1"));
        }

        [Test]
        public void FindEscapeSymbols2()
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
            dataContext.Insert(obj);
            dataContext.SubmitChanges();
            var res = dataContext.Find("1");
            Assert.AreEqual(obj.Title, res.Title);
        }

        [Test]
        public void SubmitChangesInsertingBookBecameUpdateable()
        {
            var obj = new Contracts.Book
            {
                Id = "1",
                Title = @"df",
                Author = @"fdfas"
            };
            dataContext.Insert(obj);
            obj.Author = "dsa";
            dataContext.SubmitChanges();
            obj.Author = "dsb";
            Assert.AreEqual(obj, dataContext.Find("1"));
        }
    }
}

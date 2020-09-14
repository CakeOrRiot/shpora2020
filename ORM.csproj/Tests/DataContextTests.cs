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
            var obj = new Contracts.Book();
            obj.Id = "2";
            dataContext.Insert(obj);
            dataContext.SubmitChanges();
            Assert.AreEqual(null, dataContext.Find("1"));
        }
    }
}

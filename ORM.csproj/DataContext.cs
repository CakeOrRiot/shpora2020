using ORM.Contracts;
using ORM.Db;
using System;
using System.Collections.Generic;

namespace ORM
{
    public class DataContext : IDataContext
    {
        private readonly IDbEngine dbEngine;
        private Dictionary<string, Book> cash;
        private Dictionary<string, Book> dataBase;
        public DataContext(IDbEngine dbEngine)
        {
            this.dbEngine = dbEngine;
        }

        public Book Find(string id)
        {
            throw new System.NotImplementedException();
        }

        public Book Read(string id)
        {
            return
    }

        public void Insert(Book entity)
        {
            if (entity is null)
                throw new Exception();
            throw new System.NotImplementedException();
        }

        public void SubmitChanges()
        {
            throw new System.NotImplementedException();
        }
    }
}
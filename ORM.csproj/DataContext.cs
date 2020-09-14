using ORM.Contracts;
using ORM.Db;

namespace ORM
{
    public class DataContext : IDataContext
    {
        private readonly IDbEngine dbEngine;

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
            throw new System.NotImplementedException();
        }

        public void Insert(Book entity)
        {
            throw new System.NotImplementedException();
        }

        public void SubmitChanges()
        {
            throw new System.NotImplementedException();
        }
    }
}
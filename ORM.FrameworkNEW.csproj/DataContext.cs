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

        public T Find<T>(string id) where T : DbEntity
        {
            throw new System.NotImplementedException();
        }

        public T Read<T>(string id) where T : DbEntity
        {
            throw new System.NotImplementedException();
        }

        public void Insert<T>(T entity) where T : DbEntity
        {
            throw new System.NotImplementedException();
        }

        public void SubmitChanges()
        {
            throw new System.NotImplementedException();
        }
    }
}
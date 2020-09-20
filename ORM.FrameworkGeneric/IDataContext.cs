using ORM.Contracts;

namespace ORM
{
    public interface IDataContext
    {
        T Find<T>(string id) where T : DbEntity;
        T Read<T>(string id) where T : DbEntity;
        void Insert<T>(T entity) where T : DbEntity;
        void SubmitChanges();
    }
}
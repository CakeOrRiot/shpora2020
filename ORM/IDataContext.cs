using ORM.Contracts;

namespace ORM
{
    public interface IDataContext
    {
        Book Find(string id);
        Book Read(string id);
        void Insert(Book entity);
        void SubmitChanges();
    }
}
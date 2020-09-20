using BFTIndex.Models;

namespace BFTIndex
{
    public interface IFullTextIndex
    {
        void AddOrUpdate(string documentId, string text);
        void Remove(string documentId);
        MatchedDocument[] Search(string query);
    }
}
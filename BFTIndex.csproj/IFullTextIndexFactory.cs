using System.Collections.Generic;

namespace BFTIndex
{
    public interface IFullTextIndexFactory
    {
        IFullTextIndex Create(string[] stopWords, Dictionary<char, char> normalizationTable);
    }
}
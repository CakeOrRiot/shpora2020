using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BFTIndex.Models;
using NUnit.Framework;

namespace BFTIndex
{
    public interface IWordsFilter
    {
        IEnumerable<string> Filter(IEnumerable<string> words);
    }

    public class Normalizer : IWordsFilter
    {
        public readonly Dictionary<char, char> normalizationTable;
        public Normalizer(Dictionary<char, char> normalizationTable)
        {
            this.normalizationTable = normalizationTable
                .Select(charPair =>
                        new KeyValuePair<char, char>(char.ToLowerInvariant(charPair.Key), char.ToLowerInvariant(charPair.Value)))
                .ToDictionary(charPair => charPair.Key, charPair => charPair.Value);
        }

        private char NormalizeChar(char ch)
        {
            ch = char.ToLowerInvariant(ch);
            if (normalizationTable.ContainsKey(ch))
                return normalizationTable[ch];
            return ch;
        }

        public static IEnumerable<string> Filter(this IEnumerable<string> words, Dictionary<char,char>normalizationTable)
        {
            return words.Select(word => new string(word
                .Select(NormalizeChar)
                .ToArray())
                .ToLowerInvariant());
        }
    }

    public class StopWordsFilter : IWordsFilter
    {
        public readonly HashSet<string> stopWords;
        public StopWordsFilter(HashSet<string> stopWords)
        {
            this.stopWords = stopWords;
        }
        public IEnumerable<string> Filter(IEnumerable<string> words)
        {
            return words.Where(word => !stopWords.Contains(word));
        }
    }

    public static class ListExtentions
    {
        public static int LowerBound(this List<string> list, string value)
        {
            var left = 0;
            var right = list.Count - 1;
            var ans = -1;

            while (left <= right)
            {
                var mid = (left + right) / 2;

                if (list[mid] == value)
                {
                    ans = mid;
                    left = mid - 1;
                }
                else if (list[mid].CompareTo(value) > 0)
                    right = mid - 1;
                else
                    left = mid + 1;
            }

            return ans;
        }

        public static int UpperBound(this List<string> list, string value)
        {
            var left = 0;
            var right = list.Count - 1;
            var ans = -1;

            while (left <= right)
            {
                var mid = (left + right) / 2;

                if (list[mid] == value)
                {
                    ans = mid;
                    left = mid + 1;
                }
                else if (list[mid].CompareTo(value) > 0)
                    right = mid - 1;
                else
                    left = mid + 1;
            }

            return ans;
        }

        public static int CountElementInSortedArray(this List<string> list, string value)
        {
            var lowerBound = list.LowerBound(value);
            if (lowerBound == -1)
                return 0;
            var upperBound = list.UpperBound(value);
            return upperBound - lowerBound + 1;
        }
    }

    public class FullTextIndex : IFullTextIndex
    {
        private Dictionary<string, List<string>> documents;
        private readonly StopWordsFilter stopWordsFilter;
        private readonly Normalizer normalizer;
        public FullTextIndex()
        {
            stopWordsFilter = new StopWordsFilter(new HashSet<string>());
            normalizer = new Normalizer(new Dictionary<char, char>());
            documents = new Dictionary<string, List<string>>();
        }
        public FullTextIndex(string[] stopWords, Dictionary<char, char> normalizationTable)
        {
            stopWordsFilter = new StopWordsFilter(new HashSet<string>(stopWords));
            normalizer = new Normalizer(normalizationTable);
            documents = new Dictionary<string, List<string>>();
        }

        private IEnumerable<string> GetWords(string text)
        {
            //TODO: сделать через регулярку \w+. Потому что скорее всего не все случаии разобраны.
            var splitSymbols = text
                .Where(character => char.IsPunctuation(character) || character == ' ' || char.IsControl(character))
                .Distinct()
                .ToArray();
            return text.Split(splitSymbols, StringSplitOptions.RemoveEmptyEntries)
                .Select(word => word.Trim(' '))
                .Where(word => word != "");
        }

        private List<string> GetAllowedNormalizedWords(string text)
        {
            var words = GetWords(text);
            words = stopWordsFilter.Filter(words);
            words = normalizer.Filter(words);
            return GetWords(text)
                .Select()
                .Where(word => !stopWords.Contains(word))
                .Select(word => Normalize(word))
                .Select(word => word.ToLowerInvariant())
                .OrderBy(word => word)
                .ToList();
        }

        public void AddOrUpdate(string documentId, string text)
        {
            if (text.Length == 0)
                return;
            var words = GetAllowedNormalizedWords(text);
            if (documents.ContainsKey(documentId))
            {
                //TODO: Сделать merge за O(n)
                documents[documentId].AddRange(words);
                documents.OrderBy(word => word).ToList();
            }
            else
            {
                documents[documentId] = new List<string>();
                documents[documentId].AddRange(words);
            }
        }

        public void Remove(string documentId)
        {
            documents.Remove(documentId);
        }

        public MatchedDocument[] Search(string query)
        {
            var queryWords = GetAllowedNormalizedWords(query);
            return documents
                .Where(doc => queryWords.All(word => doc.Value.Contains(word)))
                .Select(doc => new MatchedDocument(doc.Key, 0))
                .ToArray();
        }
    }

    public class FullTextIndexFactory : IFullTextIndexFactory
    {
        public IFullTextIndex Create()
        {
            return new FullTextIndex();
        }

        public IFullTextIndex Create(string[] stopWords, Dictionary<char, char> normalizationTable)
        {
            return new FullTextIndex(stopWords, normalizationTable);
        }
    }
}
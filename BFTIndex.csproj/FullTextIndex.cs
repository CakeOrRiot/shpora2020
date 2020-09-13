using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BFTIndex.Models;

namespace BFTIndex
{
    public class Normalizer
    {
        public readonly Dictionary<char, char> normalizationTable;
        public Normalizer()
        {
            normalizationTable = new Dictionary<char, char>();
        }
        public Normalizer(Dictionary<char, char> normalizationTable)
        {
            this.normalizationTable = normalizationTable
                .Select(charPair =>
                        new KeyValuePair<char, char>(char.ToLowerInvariant(charPair.Key),
                        char.ToLowerInvariant(charPair.Value)))
                .ToDictionary(charPair => charPair.Key, charPair => charPair.Value);
        }

        public char NormalizeChar(char ch)
        {
            ch = char.ToLowerInvariant(ch);
            if (normalizationTable.ContainsKey(ch))
                return normalizationTable[ch];
            return ch;
        }

        public string Normalize(string word)
        {
            return new string(word
                .Select(NormalizeChar)
                .ToArray())
                .ToLowerInvariant();
        }
    }

    public class StopWordsFilter
    {
        public readonly HashSet<string> stopWords;
        private readonly Normalizer normalizer;
        public StopWordsFilter()
        {
            stopWords = new HashSet<string>();
            normalizer = new Normalizer();
        }
        public StopWordsFilter(HashSet<string> stopWords, Normalizer normalizer)
        {
            this.normalizer = normalizer;
            this.stopWords = stopWords.Select(normalizer.Normalize).ToHashSet();
        }
        public bool IsAllowedWord(string word)
        {
            return !stopWords.Contains(normalizer.Normalize(word));
        }
    }

    public static class ListExtentions
    {
        public static int LowerBound(this List<string> list, string value)
        {
            var left = -1;
            var right = list.Count - 1;

            while (right - left > 1)
            {
                var mid = (left + right) / 2;
                if (list[mid].CompareTo(value) >= 0)
                {
                    right = mid;
                }
                else
                {
                    left = mid;
                }
            }

            return value.Equals(list[right]) ? right : -1;
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
                    left = mid;
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

    interface IMetric<T>
    {
        double Evaluate(T x);
    }

    class IDF : IMetric<string>
    {
        private Dictionary<string, Document> documents;
        public IDF(Dictionary<string, Document> documents)
        {
            this.documents = documents;
        }

        public double Evaluate(string word)
        {
            var docsWithWordCount = documents.Where(doc => doc.Value.Contains(word)).Count();
            return Math.Log10(documents.Count / (docsWithWordCount + 1));
        }
    }

    class TF : IMetric<string>
    {
        private Document document;
        public TF(Document document)
        {
            this.document = document;
        }

        public double Evaluate(string word)
        {
            return document.Count(word) / document.Length;
        }
    }

    class TFIDF : IMetric<string>
    {
        private Dictionary<string, Document> documents;
        private Document doc;
        public TFIDF(Dictionary<string, Document> documents, Document doc)
        {
            this.documents = documents;
            this.doc = doc;
        }

        public double Evaluate(string word)
        {
            var tf = new TF(doc);
            var idf = new IDF(documents);
            return tf.Evaluate(word) * idf.Evaluate(word);
        }
    }

    public class Document
    {
        private List<string> words;
        public double Weight { get; private set; }
        public int Length => words.Count;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="doc">Must be sorted</param>
        public Document(IEnumerable<string> doc)
        {
            words = doc.ToList();
        }

        //MERGE O(N) надо сделать
        public void Add(string word)
        {
            words.Add(word);
            words.OrderBy(w => w);
        }

        //MERGE O(N) надо сделать
        /// <summary>
        /// 
        /// </summary>
        /// <param name="words">Must be sorted</param>
        public void Add(IEnumerable<string> words)
        {
            this.words.AddRange(words);
            this.words.OrderBy(word => word);
        }

        public bool Contains(string word)
        {
            return words.BinarySearch(word) >= 0;
        }

        public int Count(string word)
        {
            return words.CountElementInSortedArray(word);
        }


    }

    public class FullTextIndex : IFullTextIndex
    {
        private Dictionary<string, Document> documents;
        private readonly StopWordsFilter stopWordsFilter;
        private readonly Normalizer normalizer;
        public FullTextIndex()
        {
            stopWordsFilter = new StopWordsFilter();
            normalizer = new Normalizer();
            documents = new Dictionary<string, Document>();
        }
        public FullTextIndex(string[] stopWords, Dictionary<char, char> normalizationTable)
        {
            normalizer = new Normalizer(normalizationTable);
            stopWordsFilter = new StopWordsFilter(new HashSet<string>(stopWords), normalizer);
            documents = new Dictionary<string, Document>();
        }

        private IEnumerable<string> GetWords(string text)
        {
            var matches = Regex.Matches(text, @"\w+");
            return matches.Cast<Match>().Select(match => match.Value);
        }

        //Ќадо декомпозировать на несколько методов
        private IEnumerable<string> GetAllowedNormalizedSortedWords(string text)
        {
            return GetWords(text)
                .Where(stopWordsFilter.IsAllowedWord)
                .Select(word => normalizer.Normalize(word))
                .OrderBy(word => word)
                .ToList();
        }

        public void AddOrUpdate(string documentId, string text)
        {
            if (text.Length == 0)
                return;
            var words = GetAllowedNormalizedSortedWords(text);
            documents[documentId] = new Document(words);
        }

        public void Remove(string documentId)
        {
            documents.Remove(documentId);
        }

        private double TFIDF(IEnumerable<string> words, Document doc)
        {
            var TFIDFEvaluator = new TFIDF(documents, doc);
            return words
                .Select(word => TFIDFEvaluator.Evaluate(word))
                .Sum();
        }

        public MatchedDocument[] Search(string query)
        {
            var queryWords = GetAllowedNormalizedSortedWords(query);
            if (!queryWords.Any())
                return new MatchedDocument[0];

            var documentsWithQueryWords = documents
                .Where(doc => queryWords.All(queryWord => doc.Value.Contains(queryWord)));

            var tfidf = documentsWithQueryWords
                .Select(doc => TFIDF(queryWords, doc.Value))
                .Sum();

            return documentsWithQueryWords
                .Select(doc => new MatchedDocument(doc.Key, tfidf))
                .OrderBy(doc => doc.Weight)
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
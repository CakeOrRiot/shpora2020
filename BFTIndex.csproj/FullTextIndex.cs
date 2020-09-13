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
            return Math.Log10((double)documents.Count / (docsWithWordCount));
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
            return (double)document.Count(word) / document.Length;
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

        public Document(IEnumerable<string> words)
        {
            this.words = words.ToList();
        }

        public bool Contains(string word)
        {
            return words.Contains(word);
        }

        public int Count(string word)
        {
            return words.Where(w => word == w).Count();
        }

        public string this[int index]
        {
            get => words[index];
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

        //Надо декомпозировать на несколько методов
        private IEnumerable<string> GetAllowedNormalizedSortedWords(string text)
        {
            var words = GetWords(text);
            return GetWords(text)
                .Where(stopWordsFilter.IsAllowedWord)
                .Select(word => normalizer.Normalize(word))
                //.OrderBy(word => word)
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

            return documentsWithQueryWords
                .Select(doc => new MatchedDocument(doc.Key, TFIDF(queryWords, doc.Value)))
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
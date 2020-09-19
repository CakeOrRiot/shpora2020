using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BFTIndex.Models;
using NUnit.Framework;

namespace BFTIndex
{
    public interface INormalizer
    {
        string Normalize(string word);
        char Normalize(char ch);
    }

    public class Normalizer : INormalizer
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

        public char Normalize(char ch)
        {
            ch = char.ToLowerInvariant(ch);
            if (normalizationTable.ContainsKey(ch))
                return normalizationTable[ch];
            return ch;
        }

        public string Normalize(string word)
        {
            return new string(word
                .Select(Normalize)
                .ToArray())
                .ToLowerInvariant();
        }
    }

    public interface IStopWordsFilter
    {
        bool IsAllowedWord(string word);
    }

    public class StopWordsFilter : IStopWordsFilter
    {
        public readonly HashSet<string> stopWords;
        private readonly INormalizer normalizer;
        public StopWordsFilter()
        {
            stopWords = new HashSet<string>();
            normalizer = new Normalizer();
        }
        public StopWordsFilter(HashSet<string> stopWords, INormalizer normalizer)
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
            return (double)document.wordFrequency[word] / document.Length;
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

        public Dictionary<string, int> wordFrequency;

        public Document(IEnumerable<string> words)
        {
            this.words = words.ToList();
            wordFrequency = new Dictionary<string, int>();
            CountFrequencies();
        }

        private void CountFrequencies()
        {
            foreach (var word in words)
            {
                if (!wordFrequency.ContainsKey(word))
                    wordFrequency[word] = words.Count(other => other == word);
            }
        }
        public bool Contains(string word)
        {
            return words.Contains(word);
        }

        public bool Contains(IEnumerable<string> phrase)
        {
            var wordsPosition = 0;
            var phrasePosition = 0;
            var phraseList = phrase.ToList();
            while (wordsPosition < words.Count && phrasePosition < phraseList.Count)
            {
                if (words[wordsPosition] == phraseList[phrasePosition])
                {
                    wordsPosition++;
                    phrasePosition++;
                    if (phrasePosition == phraseList.Count)
                        return true;
                }
                else
                {
                    wordsPosition = wordsPosition - phrasePosition + 1;
                    phrasePosition = 0;
                }
            }
            return false;
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

    public interface ITextParser
    {
        IEnumerable<string> GetAllWords(string text);
        IEnumerable<string> GetAllNotWords(string text);
        IEnumerable<string> GetAllPhrases(string text);
        string RemoveDelimeters(string text);
    }

    public class TextParser : ITextParser
    {
        public IEnumerable<string> GetAllWords(string text)
        {
            var matches = Regex.Matches(text, @"\w+");
            return matches.Cast<Match>().Select(match => match.Value);
        }

        public IEnumerable<string> GetAllNotWords(string text)
        {
            var matches = Regex.Matches(text, @"(not )+(\w+)");
            return matches.Cast<Match>().Select(match => match.Groups[2].ToString());
        }

        public IEnumerable<string> GetAllPhrases(string text)
        {
            var matches = Regex.Matches(text, @"""(.*?)""");
            return matches.Cast<Match>().Select(match => match.Groups[1].ToString());
        }

        public string RemoveDelimeters(string text)
        {
            var textWitSpaces = Regex.Replace(text, @"[^\w+ ]", "");
            return Regex.Replace(textWitSpaces, @" {1,}", " ");
        }
    }

    public class FullTextIndex : IFullTextIndex
    {
        private Dictionary<string, Document> documents;
        private readonly IStopWordsFilter stopWordsFilter;
        private readonly INormalizer normalizer;
        private readonly ITextParser parser;
        public FullTextIndex()
        {
            stopWordsFilter = new StopWordsFilter();
            normalizer = new Normalizer();
            documents = new Dictionary<string, Document>();
            parser = new TextParser();
        }
        public FullTextIndex(string[] stopWords, Dictionary<char, char> normalizationTable)
        {
            normalizer = new Normalizer(normalizationTable);
            stopWordsFilter = new StopWordsFilter(new HashSet<string>(stopWords), normalizer);
            documents = new Dictionary<string, Document>();
            parser = new TextParser();
        }

        private IEnumerable<string> GetAllowedNormalizedWords(IEnumerable<string> text)
        {
            return text
                .Where(stopWordsFilter.IsAllowedWord)
                .Select(word => normalizer.Normalize(word))
                .ToList();
        }

        public void AddOrUpdate(string documentId, string text)
        {
            if (text.Length == 0)
                return;
            var words = GetAllowedNormalizedWords(parser.GetAllWords(text));
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
            var queryNotWords = GetAllowedNormalizedWords(parser.GetAllNotWords(query)).ToHashSet<string>();
            queryNotWords.Add("not");

            var queryWords = parser.GetAllWords(query)
                .Where(word => !queryNotWords.Contains(word));
            queryWords = GetAllowedNormalizedWords(queryWords);

            var queryPhrases = GetAllowedNormalizedWords(parser.GetAllPhrases(query))
                .Select(parser.RemoveDelimeters)
                .Select(phrase => GetAllowedNormalizedWords(parser.GetAllWords(phrase)));

            if (!queryWords.Any() && !queryPhrases.Any())
                return new MatchedDocument[0];

            var matchedDocuments = documents
                .Where(doc => queryWords.All(doc.Value.Contains) &&
                       queryNotWords.All(word => !doc.Value.Contains(word)) &&
                       queryPhrases.All(phrase => doc.Value.Contains(phrase)));

            return matchedDocuments
                .Select(doc => new MatchedDocument(doc.Key, TFIDF(queryWords, doc.Value)))
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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace BFTIndex
{
    public static class Program
    {
        private static readonly Dictionary<char, char> NormalizationTable = new Dictionary<char, char>
        {
            ['ё'] = 'е',
            ['й'] = 'и',
            ['щ'] = 'ш',
            ['ъ'] = 'ь',
        };

        private static readonly string[] Stopwords =
        {
            "ибо",
            "и",
            "но",
            "а",
            "или"
        };

        public static void Main()
        {
            var documents = Directory.EnumerateFiles("Documents")
                .Select(path => new FileInfo(path))
                .ToDictionary(file => file.Name.Replace(file.Extension, ""), file => File.ReadAllText(file.FullName));

            var index = new FullTextIndexFactory().Create(Stopwords, NormalizationTable);

            foreach (var document in documents)
                index.AddOrUpdate(document.Key, document.Value);

            while (true)
            {
                var query = ReadQuery();
                if (query.ToLowerInvariant().Trim() == "q")
                    return;

                var sw = Stopwatch.StartNew();
                var result = index.Search(query);
                Console.WriteLine($" Found {result.Length} documents, elapsed {sw.Elapsed}");

                foreach (var document in result.OrderByDescending(d => d.Weight))
                    Console.WriteLine($"  [{document.Weight:E3}]\t{document.Id}");
                Console.WriteLine();
            }
        }

        private static string ReadQuery()
        {
            using (new ConsoleColors(ConsoleColor.Green))
                Console.Write("query (q for exit)");

            Console.Write("$ ");

            using (new ConsoleColors(ConsoleColor.White, ConsoleColor.DarkBlue))
                return Console.ReadLine();
        }

        private class ConsoleColors : IDisposable
        {
            private readonly ConsoleColor oldForeground;
            private readonly ConsoleColor oldBackground;

            public ConsoleColors(ConsoleColor? foreground = null, ConsoleColor? background = null)
            {
                oldForeground = Console.ForegroundColor;
                oldBackground = Console.BackgroundColor;

                Console.ForegroundColor = foreground ?? oldForeground;
                Console.BackgroundColor = background ?? oldBackground;
            }

            public void Dispose()
            {
                Console.ForegroundColor = oldForeground;
                Console.BackgroundColor = oldBackground;
            }
        }
    }
}

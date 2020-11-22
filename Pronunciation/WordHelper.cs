using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Pronunciation
{
    public class PronunciationEngine
    {
        public IEnumerable<PhoneticsWord> GetPhoneticsWords(string text)
        {
            var splits = text.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if(splits.Length == 1)
                return Lookup[splits.Single()].Select(x => x.Value);

            var words = new List<PhoneticsWord>();

            foreach (var split in splits)
            {
                var word = Lookup[split].Select(x=>x.Value).FirstOrDefault(); //todo multiple pronunciations
                if(word == default)
                    return Enumerable.Empty<PhoneticsWord>();

                words.Add(word);
            }

            var newPhoneticsWord = new PhoneticsWord(text, 0, words.SelectMany(x => x.Symbols).ToList());

            return new[] {newPhoneticsWord};
        }

        public PronunciationEngine() => Lookup = TryCreateLookup();

        private ILookup<string, Lazy<PhoneticsWord>> Lookup { get; }

        private static ILookup<string, Lazy<PhoneticsWord>> TryCreateLookup()
        {
            var sw = Stopwatch.StartNew();
            Console.WriteLine(@"Creating Phonetics Lookup");

            var text = PhoeneticsFiles.Dict;
            var lines = text.Split("\n");
            var results = new List<(string text, Lazy<PhoneticsWord> word)>();

            foreach (var line in lines.Where(line => !line.StartsWith(";;;")))
            {
                var r = TryCreateFromLine(line);
                results.Add(r);
            }

            var lookup = results.ToLookup(x => x.text, x=>x.word, StringComparer.OrdinalIgnoreCase);
            Console.WriteLine($@"Phonetics Lookup Created ({sw.ElapsedMilliseconds}ms) ({lookup.Count} rows)");

            return lookup;
        }


        private static (string text, Lazy<PhoneticsWord> word) TryCreateFromLine(string s)
        {
            var spaceIndex = s.IndexOf(' ');

            if(spaceIndex == -1) throw new ArgumentException($"Not enough terms in '{s}'");

            var text = s.Substring(0, spaceIndex);

            var lazyWord = new Lazy<PhoneticsWord>(()=> CreateFromLine(s));

            return (text, lazyWord);

            static PhoneticsWord CreateFromLine(string s)
            {
                var terms = s.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                if (terms.Length < 2)
                    throw new ArgumentException($"Not enough terms in '{s}'");

                var match = VariantRegex.Match(terms[0]);
                if (!match.Success)
                    throw new ArgumentException($"Could not match '{terms[0]}'");

                var word = match.Groups["word"].Value;

                var number = match.Groups["number"].Success ? int.Parse(match.Groups["number"].Value) : 0;

                var symbols = new List<Symbol>();

                foreach (var symbolString in terms.Skip(1))
                {
                    if (Enum.TryParse(symbolString, out Symbol symbol))
                        symbols.Add(symbol);
                    else
                        throw new ArgumentException($"Could not parse symbol {symbolString}");
                }

                return new PhoneticsWord(word, number, symbols);
            }
        }

        private static readonly Regex VariantRegex = new Regex(@"\A(?<word>.+?)(?:\((?<number>\d+)\))?\Z", RegexOptions.Compiled);
    }
}
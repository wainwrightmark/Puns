using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;

namespace CMU
{
    public sealed class Word : IEquatable<Word>
    {
        public static Result<ILookup<string, Word>> TryCreateLookup()
        {
            var sw = Stopwatch.StartNew();
            Console.WriteLine(@"Creating Phonetics Lookup");

            var text = Resource.Dict;
            var lines = text.Split("\r\n");
            var results = new List<Word>();

            foreach (var  line in lines.Where(line=> !line.StartsWith(";;;")))
            {
                var r = TryCreateFromLine(line);
                if (r.IsFailure)
                    return r.ConvertFailure<ILookup<string, Word>>();
                results.Add(r.Value);
            }

            var lookup = results.ToLookup(x => x.Text, StringComparer.OrdinalIgnoreCase);
            Console.WriteLine($@"Phonetics Lookup Created ({sw.ElapsedMilliseconds}ms) ({lookup.Count} rows)");

            return Result.Success(lookup);
        }


        public static Result<Word> TryCreateFromLine(string s)
        {
            var terms = s.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            if(terms.Length < 2)return Result.Failure<Word>("Not enough terms");

            var match = VariantRegex.Match(terms[0]);
            if(!match.Success)
                return Result.Failure<Word>($"Could not match '{terms[0]}'");

            var word = match.Groups["word"].Value;

            var number = match.Groups["number"].Success ? int.Parse(match.Groups["number"].Value) :  0;

            var symbols = new List<Symbol>();

            foreach (var symbolString in terms.Skip(1))
            {
                if(Enum.TryParse(symbolString, out Symbol symbol))
                    symbols.Add(symbol);
                else
                    return Result.Failure<Word>($"Could not parse symbol {symbolString}");
            }

            return new Word(word, number, symbols);
        }

        private static readonly Regex VariantRegex = new Regex(@"\A(?<word>.+?)(?:\((?<number>\d+)\))?\Z", RegexOptions.Compiled);


        public Word(string text, int variant, IReadOnlyList<Symbol> symbols)
        {
            Text = text;
            Variant = variant;
            Symbols = symbols;
        }

        public string Text { get; }

        /// <inheritdoc />
        public override string ToString() => Text;

        public int Variant { get; }

        public IReadOnlyList<Symbol> Symbols { get; }

        /// <inheritdoc />
        public bool Equals(Word other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Text == other.Text && Variant == other.Variant;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Word) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Text, Variant);

        public static bool operator ==(Word left, Word right) => Equals(left, right);

        public static bool operator !=(Word left, Word right) => !Equals(left, right);
    }
}

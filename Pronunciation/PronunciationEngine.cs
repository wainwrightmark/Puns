using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FileDatabase;

namespace Pronunciation
{

public sealed class PronunciationEngine : IDisposable
{
    public IEnumerable<PhoneticsWord> GetAllPhoneticsWords() => _database.GetAll();

    public PhoneticsWord? GetPhoneticsWord(string text) //todo multiple pronunciations
    {
        var splits = text.Split(
            '_',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );

        if (splits.Length == 1)
            return GetSinglePhoneticsWord(text);

        var words = new List<PhoneticsWord>();

        foreach (var split in splits)
        {
            var word = GetSinglePhoneticsWord(split);

            if (word is null)
                return null;

            words.Add(word);
        }

        var newPhoneticsWord = new PhoneticsWord(
            text,
            0,
            true,
            words.SelectMany(x => x.Syllables).ToList()
        );

        return newPhoneticsWord;
    }

    private PhoneticsWord? GetSinglePhoneticsWord(string text)
    {
        var key = (text.ToUpperInvariant().Trim(), 0);
        var r   = _database[key];
        return r;
    }

    public PronunciationEngine() => _database = new Database<PhoneticsWord, (string, int)>(
        PhoeneticsFiles.Pronunciation,
        Encoding.UTF8,
        GetKeyFromLine,
        CreateFromLine
    );

    private readonly Database<PhoneticsWord, (string, int)> _database;

    private static (string word, int number) GetKeyFromLine(string line)
    {
        var spaceIndex = line.IndexOf(' ');
        var firstTerm  = line.Substring(0, spaceIndex);

        var match = VariantRegex.Match(firstTerm);

        if (!match.Success)
            throw new ArgumentException($"Could not match '{firstTerm}'");

        var word = match.Groups["word"].Value;

        var number = match.Groups["number"].Success ? int.Parse(match.Groups["number"].Value) : 0;

        return (word, number);
    }

    private static PhoneticsWord CreateFromLine(string line)
    {
        var terms = line.Split(
            ' ',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
        );

        if (terms.Length < 2)
            throw new ArgumentException($"Not enough terms in '{line}'");

        var match = VariantRegex.Match(terms[0]);

        if (!match.Success)
            throw new ArgumentException($"Could not match '{terms[0]}'");

        var word = match.Groups["word"].Value;

        var number = match.Groups["number"].Success ? int.Parse(match.Groups["number"].Value) : 0;

        var syllables = new List<Syllable>();
        var symbols   = new List<Symbol>();

        foreach (var symbolString in terms.Skip(1))
        {
            if (symbolString == "-")
            {
                if (symbols.Any())
                {
                    syllables.Add(new Syllable(symbols));
                    symbols = new List<Symbol>();
                }
            }
            else if (Enum.TryParse(symbolString, out Symbol symbol))
                symbols.Add(symbol);
            else
                throw new ArgumentException($"Could not parse symbol {symbolString}");
        }

        if (symbols.Any())
            syllables.Add(new Syllable(symbols));

        return new PhoneticsWord(word, number, false, syllables);
    }

    private static readonly Regex VariantRegex = new(
        @"\A(?<word>.+?)(?:\((?<number>\d+)\))?\Z",
        RegexOptions.Compiled
    );

    /// <inheritdoc />
    public void Dispose() => _database.Dispose();
}

}

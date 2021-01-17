using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FileDatabase;

namespace Pronunciation
{

public sealed class SpellingEngine : IDisposable
{
    public IEnumerable<Spelling> GetAllSpellings() => _database.GetAll();

    private readonly Database<Spelling, string> _database;

    public SpellingEngine() => _database = new Database<Spelling, string>(
        PhoeneticsFiles.Spelling,
        Encoding.UTF8,
        GetKeyFromLine,
        CreateFromLine
    );

    public Spelling? GetSpelling(Syllable syllable)
    {
        var key = syllable.ToString();
        var r   = _database[key];
        return r;
    }

    private static string GetKeyFromLine(string line)
    {
        var tabIndex     = line.IndexOf('\t');
        var syllableText = line.Substring(0, tabIndex);
        return syllableText;
    }

    private static Spelling CreateFromLine(string line)
    {
        var tabIndex     = line.IndexOf('\t');
        var syllableText = line.Substring(0, tabIndex);
        var text         = line[(tabIndex + 1)..];

        var symbols = syllableText.Split(' ')
            .Select(
                x => Enum.TryParse(x, out Symbol symbol)
                    ? symbol
                    : throw new Exception($"Could not parse Symbol '{x}' on line '{line}'")
            )
            .ToList();

        var syllable = new Syllable(symbols);
        var spelling = new Spelling(syllable, text);

        return spelling;
    }

    /// <inheritdoc />
    public void Dispose() => _database.Dispose();
}

public record Spelling(Syllable Syllable, string Text);

}

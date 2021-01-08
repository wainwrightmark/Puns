using System.Collections.Generic;
using System.Linq;
using Pronunciation;

namespace Puns.Strategies
{

public class SameConsonantsPunStrategy : PunStrategy
{
    /// <inheritdoc />
    public SameConsonantsPunStrategy(
        SpellingEngine spellingEngine,
        IEnumerable<PhoneticsWord> themeWords) : base(spellingEngine, themeWords) { }

    /// <inheritdoc />
    public override IEnumerable<IReadOnlyList<Syllable>> GetThemeWordSyllables(PhoneticsWord word)
    {
        yield return GetConsonantSyllables(word);
    }

    private static IReadOnlyList<Syllable> GetConsonantSyllables(PhoneticsWord word)
    {
        var syllables = word.Syllables.Select(s => s.GetNoConsonantSyllable).ToList();
        return syllables;
    }

    /// <inheritdoc />
    public override IEnumerable<PunReplacement> GetPossibleReplacements(PhoneticsWord originalWord)
    {
        var sw = GetConsonantSyllables(originalWord);

        foreach (var themeWord in ThemeWordLookup[sw])
        {
            yield return new PunReplacement(
                PunType.SameConsonants,
                themeWord.Text,
                false,
                themeWord.Text
            );
        }
    }
}

}

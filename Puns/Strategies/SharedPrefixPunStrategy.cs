using System;
using System.Collections.Generic;
using System.Linq;
using Pronunciation;

namespace Puns.Strategies
{

public class SharedPrefixPunStrategy : PunStrategy //Both words have the same multi-syllable prefix
{
    /// <inheritdoc />
    public SharedPrefixPunStrategy(
        SpellingEngine spellingEngine,
        IEnumerable<PhoneticsWord> themeWords) : base(spellingEngine, themeWords) { }

    /// <inheritdoc />
    public override IEnumerable<IReadOnlyList<Syllable>> GetThemeWordSyllables(PhoneticsWord word)
    {
        if (word.Syllables.Count > 2)
            yield return word.Syllables.Take(2).ToList();
    }

    /// <inheritdoc />
    public override IEnumerable<PunReplacement> GetPossibleReplacements(PhoneticsWord originalWord)
    {
        if (originalWord.Syllables.Count > 2)
        {
            var firstTwoSyllables = originalWord.Syllables.Take(2).ToList();

            foreach (var themeWord in ThemeWordLookup[firstTwoSyllables])
            {
                if (!themeWord.Text.Equals(originalWord.Text, StringComparison.OrdinalIgnoreCase))
                {
                    yield return new PunReplacement(
                        PunType.SharedPrefix,
                        themeWord.Text,
                        false,
                        themeWord.Text
                    );
                }
            }
        }
    }
}

}

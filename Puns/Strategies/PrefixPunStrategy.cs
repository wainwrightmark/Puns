using System;
using System.Collections.Generic;
using System.Linq;
using Pronunciation;

namespace Puns.Strategies
{

//TODO first syllable rhyme

/// <summary>
/// The original word could be the beginning of the theme word
/// </summary>
public class PrefixPunStrategy : PunStrategy
{
    /// <inheritdoc />
    public PrefixPunStrategy(SpellingEngine spellingEngine, IEnumerable<PhoneticsWord> themeWords) :
        base(spellingEngine, themeWords) { }

    /// <inheritdoc />
    public override IEnumerable<IReadOnlyList<Syllable>> GetThemeWordSyllables(PhoneticsWord word)
    {
        for (var i = 1; i < word.Syllables.Count - 1; i++)
        {
            var syllables = word.Syllables.Take(i).ToList();
            yield return syllables;

            var next = word.Syllables[i];

            if (next.Onset.Any())
            {
                var nextSyllables =  syllables.Take(syllables.Count - 1)
                    .Append(new Syllable(syllables.Last().Symbols.Concat(next.Onset).ToList()))
                    .ToList();

                yield return nextSyllables;
            }
                
        }

        //var extra = 
    }

    /// <inheritdoc />
    public override IEnumerable<PunReplacement> GetPossibleReplacements(PhoneticsWord originalWord)
    {
        foreach (var themeWord in ThemeWordLookup[originalWord.Syllables])
        {
            if (!themeWord.Text.Equals(originalWord.Text, StringComparison.OrdinalIgnoreCase))
            {
                yield return new PunReplacement(
                    PunType.Prefix,
                    themeWord.Text,
                    false,
                    themeWord.Text
                );
            }
        }
    }
}

}

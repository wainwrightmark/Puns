using System.Collections.Generic;
using System.Linq;
using Pronunciation;

namespace Puns.Strategies
{

/// <summary>
/// The theme word rhymes with part of the original word TODO: Maybe I'm arayzed
/// </summary>
public class InfixRhymePunStrategy : PunStrategy
{
    /// <inheritdoc />
    public InfixRhymePunStrategy(
        SpellingEngine spellingEngine,
        IEnumerable<PhoneticsWord> themeWords) : base(spellingEngine, themeWords) { }

    /// <inheritdoc />
    public override IEnumerable<IReadOnlyList<Syllable>> GetThemeWordSyllables(PhoneticsWord word)
    {
        if (word.Syllables.Count == 1)
            yield return word.Syllables.Select(x => x.GetRhymeSyllable).ToList();
    }

    /// <inheritdoc />
    public override IEnumerable<PunReplacement> GetPossibleReplacements(PhoneticsWord originalWord)
    {
        for (var index = 1; index < originalWord.Syllables.Count - 1; index++)
        {
            var syllable = originalWord.Syllables[index];

            if (syllable.Nucleus.IsStressedVowel())
            {
                var rhymeSyllable = syllable.GetRhymeSyllable;

                foreach (var themeWord in ThemeWordLookup[new[] { rhymeSyllable }])
                {
                    if (themeWord.Syllables.Count == 1
                     && !themeWord.Syllables.Single().Equals(syllable))
                    {
                        var spelling = GetSpelling(originalWord.Syllables.Take(index))
                                     + themeWord.Text + GetSpelling(
                                           originalWord.Syllables.Skip(index + 1)
                                       );

                        yield return new PunReplacement(
                            PunType.Infix,
                            spelling,
                            true,
                            themeWord.Text
                        );
                    }
                }
            }
        }
    }
}

}

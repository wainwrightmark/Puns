using System.Collections.Generic;
using System.Linq;
using Pronunciation;

namespace Puns.Strategies
{

/// <summary>
/// The theme word rhymes with original word
/// </summary>
public class PerfectRhymePunStrategy : PunStrategy
{
    public PerfectRhymePunStrategy(
        SpellingEngine spellingEngine,
        IEnumerable<PhoneticsWord> themeWords) : base(spellingEngine, themeWords) { }

    /// <inheritdoc />
    public override IEnumerable<IReadOnlyList<Syllable>> GetThemeWordSyllables(PhoneticsWord word)
    {
        var lastStressedVowelIndex = word.Syllables.LastIndexOf(x => x.Nucleus.IsStressedVowel());

        if (lastStressedVowelIndex < 0)
            yield break; //No stressed vowel

        var syllables = word.Syllables.Skip(lastStressedVowelIndex)
            .Select((x, i) => i == 0 ? x.GetRhymeSyllable : x)
            .ToList();

        yield return syllables;
    }

    public override IEnumerable<PunReplacement> GetPossibleReplacements(PhoneticsWord originalWord)
    {
        var lastStressedVowelIndex =
            originalWord.Syllables.LastIndexOf(x => x.Nucleus.IsStressedVowel());

        if (lastStressedVowelIndex < 0)
            yield break; //No stressed vowel

        var syllables = originalWord.Syllables.Skip(lastStressedVowelIndex)
            .Select((x, i) => i == 0 ? x.GetRhymeSyllable : x)
            .ToList();

        foreach (var themeWord in ThemeWordLookup[syllables])
        {
            if (originalWord.Text.Contains(themeWord.Text))
                yield break;

            if (themeWord.Syllables.Count == originalWord.Syllables.Count)
            {
                yield return new PunReplacement(
                    PunType.PerfectRhyme,
                    themeWord.Text,
                    false,
                    themeWord.Text
                );
            }
            else if (themeWord.Syllables.Count < originalWord.Syllables.Count)
            {
                var replacement = GetSpelling(originalWord.Syllables.Take(lastStressedVowelIndex))
                                + themeWord.Text;

                yield return new PunReplacement(
                    PunType.PerfectRhyme,
                    replacement,
                    false,
                    themeWord.Text
                );
            }
        }
    }
}

}

using System.Collections.Generic;
using System.Linq;
using Pronunciation;

namespace Puns.Strategies
{
    public class PrefixRhymePunStrategy : PunStrategy //The last syllable of the themeword rhymes with the first syllable of the original word
    {
        /// <inheritdoc />
        public PrefixRhymePunStrategy(SpellingEngine spellingEngine, IEnumerable<PhoneticsWord> themeWords) : base(spellingEngine, themeWords) {}

        /// <inheritdoc />
        public override IEnumerable<PhoneticsWord> GetThemeWordSubwords(PhoneticsWord word)
        {
            if (word.Syllables[^1].Nucleus.IsStressedVowel())
            {
                var rhymeSyllable = word.Syllables[^1].GetRhymeSyllable;
                yield return new PhoneticsWord(rhymeSyllable.ToString(), 0, true, new []{rhymeSyllable});
            }
        }

        /// <inheritdoc />
        public override IEnumerable<PunReplacement> GetPossibleReplacements(PhoneticsWord originalWord)
        {
            if (originalWord.Syllables.Count > 1 && originalWord.Syllables[0].Nucleus.IsStressedVowel())
            {
                var rhymeSyllable = originalWord.Syllables[0].GetRhymeSyllable;
                var rhymeWord = new PhoneticsWord(rhymeSyllable.ToString(), 0, true, new []{rhymeSyllable});

                foreach (var themeWord in ThemeWordLookup[rhymeWord])
                {
                    if (themeWord.Syllables.Count > 1 || themeWord.Syllables[^1] != originalWord.Syllables[0])
                    {
                        var suffix = GetSpelling(originalWord.Syllables.Skip(1));

                        yield return new PunReplacement(PunType.PrefixRhyme, themeWord + suffix, true, themeWord.Text);
                    }
                }
            }
        }
    }
}
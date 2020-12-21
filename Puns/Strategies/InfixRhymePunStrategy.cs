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
        public InfixRhymePunStrategy(SpellingEngine spellingEngine, IEnumerable<PhoneticsWord> themeWords) : base(spellingEngine, themeWords)
        {
        }

        /// <inheritdoc />
        public override IEnumerable<PhoneticsWord> GetThemeWordSubwords(PhoneticsWord word)
        {
            if (word.Syllables.Count == 1)
                return word.Syllables.Select(x => x.GetRhymeSyllable)
                    .Select(x => new PhoneticsWord(word.Text, 0, false, new[] {x}));
            return Enumerable.Empty<PhoneticsWord>();
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

                    foreach (var themeWord in ThemeWordLookup[new PhoneticsWord(rhymeSyllable.ToString(), 0, false, new []{rhymeSyllable})])
                    {
                        if (themeWord.Syllables.Count == 1 && !themeWord.Syllables.Single().Equals(syllable))
                        {
                            var newSyllables = originalWord.Syllables.Take(index)
                                .Concat(themeWord.Syllables)
                                .Concat(originalWord.Syllables.Skip(index + 1)).ToList();

                            var spelling = GetSpelling(newSyllables);

                            yield return new PunReplacement(PunType.Infix, spelling, true, themeWord.Text);
                        }
                    }
                }
            }
        }
    }
}
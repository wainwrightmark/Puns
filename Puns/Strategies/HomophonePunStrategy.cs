using System;
using System.Collections.Generic;
using Pronunciation;

namespace Puns.Strategies
{
    public class HomophonePunStrategy : PunStrategy
    {
        /// <inheritdoc />
        public HomophonePunStrategy(SpellingEngine spellingEngine, IEnumerable<PhoneticsWord> themeWords) : base(spellingEngine, themeWords) {}

        /// <inheritdoc />
        public override IEnumerable<IReadOnlyList<Syllable>> GetThemeWordSyllables(PhoneticsWord word)
        {
            yield return word.Syllables;
        }


        /// <inheritdoc />
        public override IEnumerable<PunReplacement> GetPossibleReplacements(PhoneticsWord originalWord)
        {

            foreach (var themeWord in ThemeWordLookup[originalWord.Syllables])
            {
                var punType = originalWord.Text.Equals(themeWord.Text, StringComparison.OrdinalIgnoreCase)? PunType.SameWord : PunType.Identity;

                yield return new PunReplacement(punType, themeWord.Text, false, themeWord.Text);
            }
        }
    }
}
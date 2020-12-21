using System.Collections.Generic;
using System.Linq;
using Pronunciation;

namespace Puns.Strategies
{
    public class SameConsonantsPunStrategy : PunStrategy
    {
        /// <inheritdoc />
        public SameConsonantsPunStrategy(IEnumerable<PhoneticsWord> themeWords) : base(themeWords) {}

        /// <inheritdoc />
        public override IEnumerable<PhoneticsWord> GetThemeWordSubwords(PhoneticsWord word)
        {
            yield return GetSubWord(word);
        }

        private static PhoneticsWord GetSubWord(PhoneticsWord word)
        {
            var syllables = word.Syllables.Select(s => s.GetNoConsonantSyllable).ToList();

            return new PhoneticsWord(word.Text, 0, true, syllables);
        }

        /// <inheritdoc />
        public override IEnumerable<PunReplacement> GetPossibleReplacements(PhoneticsWord originalWord)
        {
            var sw = GetSubWord(originalWord);

            foreach (var themeWord in ThemeWordLookup[sw])
            {
                yield return new PunReplacement(PunType.SameConsonants, themeWord.Text, false, themeWord.Text);
            }
        }
    }
}
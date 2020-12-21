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
        public PrefixPunStrategy(SpellingEngine spellingEngine, IEnumerable<PhoneticsWord> themeWords) : base(spellingEngine, themeWords) {}

        /// <inheritdoc />
        public override IEnumerable<PhoneticsWord> GetThemeWordSubwords(PhoneticsWord word)
        {
            for (var i = 1; i < word.Syllables.Count - 1; i++)
            {
                var syllables = word.Syllables.Take(i).ToList();

                var cluster = new PhoneticsWord(string.Join("", syllables), 0, true, syllables);
                yield return cluster;
            }
        }


        /// <inheritdoc />
        public override IEnumerable<PunReplacement> GetPossibleReplacements(PhoneticsWord originalWord)
        {
            foreach (var themeWord in ThemeWordLookup[originalWord])
            {
                if (!themeWord.Text.StartsWith(originalWord.Text))
                {
                    yield return new PunReplacement(PunType.Prefix, themeWord.Text, false, themeWord.Text);
                }
            }
        }
    }
}
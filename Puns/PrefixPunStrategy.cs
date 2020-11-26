using System.Collections.Generic;
using System.Linq;
using Pronunciation;

namespace Puns
{

    //TODO first syllable rhyme

    /// <summary>
    /// The original word could be the beginning of the theme word
    /// </summary>
    public class PrefixPunStrategy : PunStrategy
    {
        /// <inheritdoc />
        public PrefixPunStrategy(IEnumerable<PhoneticsWord> themeWords) : base(themeWords) {}


        /// <inheritdoc />
        public override IEnumerable<SymbolCluster> GetThemeWordSymbolClusters(PhoneticsWord word)
        {
            for (var i = 2; i < word.Symbols.Count - 1; i++)
            {
                var cluster = new SymbolCluster(word.Symbols.Take(i).ToList());
                yield return cluster;
            }
        }

        /// <inheritdoc />
        public override IEnumerable<PunReplacement> GetPossibleReplacements(PhoneticsWord originalWord)
        {
            var cluster = new SymbolCluster(originalWord.Symbols);

            foreach (var themeWord in ThemeWordLookup[cluster])
            {
                if (!themeWord.Text.StartsWith(originalWord.Text))
                {
                    yield return new PunReplacement(PunType.Prefix, themeWord.Text, false);
                }
            }
        }
    }
}
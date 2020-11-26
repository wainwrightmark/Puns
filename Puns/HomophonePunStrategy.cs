using System;
using System.Collections.Generic;
using Pronunciation;

namespace Puns
{
    public class HomophonePunStrategy : PunStrategy
    {
        /// <inheritdoc />
        public HomophonePunStrategy(IEnumerable<PhoneticsWord> themeWords) : base(themeWords) {}

        /// <inheritdoc />
        public override IEnumerable<SymbolCluster> GetThemeWordSymbolClusters(PhoneticsWord word)
        {
            yield return new SymbolCluster(word.Symbols);
        }

        /// <inheritdoc />
        public override IEnumerable<PunReplacement> GetPossibleReplacements(PhoneticsWord originalWord)
        {
            var symbolCluster = new SymbolCluster(originalWord.Symbols);

            foreach (var themeWord in ThemeWordLookup[symbolCluster])
            {
                var punType = originalWord.Text.Equals(themeWord.Text, StringComparison.OrdinalIgnoreCase)? PunType.SameWord : PunType.Identity;

                yield return new PunReplacement(punType, themeWord.Text, false, themeWord.Text);
            }
        }
    }
}
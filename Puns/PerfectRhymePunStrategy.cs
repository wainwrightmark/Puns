using System.Collections.Generic;
using System.Linq;
using Pronunciation;

namespace Puns
{
    /// <summary>
    /// The theme word rhymes with original word
    /// </summary>
    public class PerfectRhymePunStrategy : PunStrategy
    {
        public PerfectRhymePunStrategy(IEnumerable<PhoneticsWord> themeWords) : base(themeWords) { }

        public override IEnumerable<SymbolCluster> GetThemeWordSymbolClusters(PhoneticsWord word)
        {
            var lastStressedVowelIndex = word.Symbols.LastIndexOf(x => x.IsStressedVowel());

            if (lastStressedVowelIndex >= 0)
            {
                var vowel = new SymbolCluster(word.Symbols.Skip(lastStressedVowelIndex).ToList());
                yield return vowel;
            }
        }

        public override IEnumerable<PunReplacement> GetPossibleReplacements(PhoneticsWord originalWord)
        {
            var lastStressedVowel = GetThemeWordSymbolClusters(originalWord).FirstOrDefault();

            if(lastStressedVowel is null) yield break;


            foreach (var themeWord in ThemeWordLookup[lastStressedVowel])
            {
                var insert = false;

                if (themeWord.Text.Length < originalWord.Text.Length)
                {
                    var originalWordStressedVowels = originalWord.Symbols.Count(x => x.IsStressedVowel());
                    if (originalWordStressedVowels > 1)
                    {
                        var themeWordStressedVowels = themeWord.Symbols.Count(x => x.IsStressedVowel());
                        if (originalWordStressedVowels > themeWordStressedVowels)
                            insert = true;
                    }
                }

                string replacement = insert
                    ? originalWord.Text.Substring(0, originalWord.Text.Length - themeWord.Text.Length) + themeWord.Text//TODO improve this replacement
                    : themeWord.Text;

                yield return new PunReplacement(PunType.PerfectRhyme, replacement, insert, themeWord.Text);
            }
        }


    }
}
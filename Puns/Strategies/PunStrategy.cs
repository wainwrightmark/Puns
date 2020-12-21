using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation;

namespace Puns.Strategies
{
    public abstract class PunStrategy
    {
        protected PunStrategy(SpellingEngine spellingEngine, IEnumerable<PhoneticsWord> themeWords)
        {
            SpellingEngine = spellingEngine;
            ThemeWordLookup = themeWords.SelectMany(word =>
                    GetThemeWordSubwords(word).Select(cluster=> (word, cluster)))
                .OrderBy(x=>x.word.Text.Length)
                .ToLookup(x => x.cluster, x => x.word, WordPronunciationComparer.Instance);
        }

        public SpellingEngine SpellingEngine { get; }

        public ILookup<PhoneticsWord, PhoneticsWord> ThemeWordLookup { get; }

        public abstract IEnumerable<PhoneticsWord> GetThemeWordSubwords(PhoneticsWord word);

        public abstract IEnumerable<PunReplacement> GetPossibleReplacements(PhoneticsWord originalWord);

        protected string CreateSpelling(IEnumerable<Syllable> syllables)
        {
            var sb = new StringBuilder();

            foreach (var syllable in syllables)
            {
                var spelling = SpellingEngine.GetSpelling(syllable);
                if (spelling != null)
                    sb.Append(spelling.Text);
                else sb.Append(new string(syllable.ToString().Where(char.IsLetter).ToArray()));
            }

            return sb.ToString();

        }
    }
}
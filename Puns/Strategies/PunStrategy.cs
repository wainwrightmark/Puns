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

        ThemeWordLookup = themeWords.SelectMany(
                word =>
                    GetThemeWordSyllables(word).Select(cluster => (word, cluster))
            )
            .OrderBy(x => x.word.Text.Length)
            .ToLookup(x => x.cluster, x => x.word, ListComparer<Syllable>.Instance);
    }

    public SpellingEngine SpellingEngine { get; }

    public ILookup<IReadOnlyList<Syllable>, PhoneticsWord> ThemeWordLookup { get; }

    public abstract IEnumerable<IReadOnlyList<Syllable>> GetThemeWordSyllables(PhoneticsWord word);

    public abstract IEnumerable<PunReplacement> GetPossibleReplacements(PhoneticsWord originalWord);

    protected string GetSpelling(IEnumerable<Syllable> syllables)
    {
        var sb = new StringBuilder();

        foreach (var syllable in syllables)
        {
            var spelling = SpellingEngine.GetSpelling(syllable);

            if (spelling != null)
                sb.Append(spelling.Text);
            else
                sb.Append(new string(syllable.ToString().Where(char.IsLetter).ToArray()));
        }

        return sb.ToString();
    }
}

}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using MoreLinq;
using Pronunciation;
using Puns.Strategies;
using WordNet;

namespace Puns
{

public readonly struct SynsetWithGloss
{
    public SynsetWithGloss(SynSet synSet, string gloss, int index)
    {
        SynSet     = synSet;
        Gloss      = gloss;
        Index = index;
    }
    public SynSet SynSet { get; }

    public string Gloss { get; }
    public int Index { get; }

    /// <inheritdoc />
    public override string ToString() => Gloss;
}

public static class PunHelper
{

    public static IEnumerable<Pun> GetPuns(
        PunCategory category,
        string theme,
        IReadOnlyCollection<SynSet> synSets,
        WordNetEngine wordNetEngine,
        PronunciationEngine pronunciationEngine,
        SpellingEngine spellingEngine,
        IReadOnlyList<PunStrategyFactory> strategies)
    {
        var sw = Stopwatch.StartNew();
#if Debug
        Console.WriteLine(@"Getting Puns");
#endif

        var resultCount = 0;

        var phrases = GetPhrases(category);

        var themeWords =
            synSets.SelectMany(
                    synSet => GetRelatedWords(theme, synSet, wordNetEngine)
                        .Select(x => x.Word)
                )
                .Where(x => !x.Contains('_'))
                .Prepend(theme)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Except(CommonWords.Value, StringComparer.OrdinalIgnoreCase)
                .Where(x=>x.Length > 1)
                .Select(pronunciationEngine.GetPhoneticsWord)
                .Where(x => x is not null)
                .Cast<PhoneticsWord>()
                .Where(x => x.Syllables.Count > 1 || x.Syllables[0].Symbols.Count > 1)
                .Distinct(WordPronunciationComparer.Instance)
                .ToList();
#if Debug
        Console.WriteLine($@"Got Theme Words ({sw.Elapsed}");
#endif

        var cache = new Dictionary<PhoneticsWord, PunReplacement>();

        var punStrategies =
            strategies.Select(x => x.GetStrategy(spellingEngine, themeWords)).ToList();

        #if Debug
        Console.WriteLine($@"Built Strategies ({sw.Elapsed}");
#endif

        //TODO run in parallel
        foreach (var phrase in phrases)
        {

            var words = phrase
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var wordList         = new List<string>();
            var punWords         = new HashSet<string>();
            var containsOriginal = false;
            var containsPun      = false;

            foreach (var word in words)
            {
                var bestReplacement = BestReplacement(
                    word,
                    pronunciationEngine,
                    cache,
                    punStrategies
                );

                if (bestReplacement != null)
                {
                    var casing    = DetectCasing(word);
                    var newString = ToCase(bestReplacement.Value.ReplacementString, casing);
                    wordList.Add(newString);
                    containsOriginal |= bestReplacement.Value.IsAmalgam;
                    containsPun      =  true;
                    punWords.Add(bestReplacement.Value.PunWord);
                }
                else
                {
                    wordList.Add(word);
                    containsOriginal = true;
                }
            }

            if (containsPun && (words.Length > 1 || containsOriginal))
            {
                var pun = new Pun(wordList.ToDelimitedString(" "), phrase, punWords);

                #if Debug
                if (resultCount == 0)
                    Console.WriteLine($@"{pun.NewPhrase} ({sw.Elapsed})");
                #endif

                yield return pun;

                resultCount++;
            }
        }


        #if Debug
        Console.WriteLine($@"{resultCount} Puns Got ({sw.Elapsed})");
        #endif

        static PunReplacement? BestReplacement(
            string word,
            PronunciationEngine pronunciationEngine,
            IDictionary<PhoneticsWord, PunReplacement> cache,
            IEnumerable<PunStrategy> punStrategies)
        {
            if (CommonWords.Value.Contains(word))
                return null;

            var cmuWord = pronunciationEngine.GetPhoneticsWord(word);

            if (cmuWord is null)
                return null;

            if (cmuWord.Syllables.Count < 2 && cmuWord.Syllables[0].Symbols.Count < 3)
                return null;

            if (!cache.TryGetValue(cmuWord, out var bestReplacement))
            {
                bestReplacement = punStrategies
                    .SelectMany(x => x.GetPossibleReplacements(cmuWord))
                    .FirstOrDefault()!;

                cache.Add(cmuWord, bestReplacement);
            }

            if (string.IsNullOrWhiteSpace(bestReplacement.ReplacementString))
                return null;

            return bestReplacement;
        }
    }

    private static readonly Lazy<IReadOnlySet<string>> CommonWords = new(
        () => WordData.CommonWords.Split(
                "\n",
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            )
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
    );

    public static IReadOnlyCollection<string> GetPhrases(PunCategory category)
    {
        return category switch
        {
            PunCategory.Movies => WordData.Movies.Split(
                "\n",
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            ),
            PunCategory.Musicals => WordData.Musicals.Split(
                "\n",
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            ),
            PunCategory.Idiom => WordData.Idioms.Split(
                "\n",
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            ),
            PunCategory.Bands => WordData.Bands.Split(
                "\n",
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            ),
            PunCategory.Books => WordData.Books.Split(
                "\n",
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            ),
            PunCategory.Brands => WordData.Brands.Split(
                "\n",
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            ),
            PunCategory.Celebs => WordData.Celebs.Split(
                "\n",
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            ),
            PunCategory.Countries => WordData.Countries.Split(
                "\n",
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            ),
            PunCategory.Artists => WordData.Artists.Split(
                "\n",
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            ),
            PunCategory.Songs => WordData.Songs.Split(
                "\n",
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            ),
            PunCategory.CountrySongs => WordData.CountrySongs.Split(
                "\n",
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            ),
            PunCategory.ChristmasSongs => WordData.ChristmasSongs.Split(
                "\n",
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            ),
            PunCategory.TVShows => WordData.TVShows.Split(
                "\n",
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            ),
            PunCategory.Wedding => WordData.Wedding.Split(
                "\n",
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            ),
            PunCategory.MovieQuotes => WordData.MovieQuotes.Split(
                "\n",
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            ),
            _                   => throw new ArgumentOutOfRangeException(nameof(category), category, null)
        };
    }

    public static Casing DetectCasing(string s)
    {
        if (s.All(char.IsLower))
            return Casing.Lower;

        if (s.All(char.IsUpper))
            return Casing.Upper;

        return Casing.Title; //Not 100% perfect
    }

    public static string ToCase(string s, Casing casing)
    {
        return casing switch
        {
            Casing.Lower => s.ToLower(),
            Casing.Upper => s.ToUpper(),
            Casing.Title => Thread.CurrentThread.CurrentCulture.TextInfo
                .ToTitleCase(s.ToLower()),
            _ => throw new ArgumentOutOfRangeException(nameof(casing), casing, null)
        };
    }

    public static IEnumerable<SynsetWithGloss> GetRelativeGloss(
        IEnumerable<SynSet> synSets,
        int maxGlossWords,
        WordNetEngine wordNetEngine)
    {


        var dictionary = synSets.Distinct()
            .ToDictionary(
                x => x,
                x =>
                    //x.Words

                    GetPunSynSets(x, wordNetEngine, false).SelectMany(y => y.Words)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase)
            );

        var index = 0;

        foreach (var (synSet, words) in dictionary)
        {
            var otherSets = dictionary.Where(x => x.Key != synSet)
                .Select(x => x.Value)
                .ToList();

            var uniqueWords = words.Where(word => !otherSets.Any(x => x.Contains(word)))
                .Take(maxGlossWords)
                .ToList();

            if (uniqueWords.Any())
            {
                string newGloss = string.Join(", ", uniqueWords.Select(Format));
                yield return new SynsetWithGloss(synSet, newGloss, index);

                index++;
            }
        }

        static string Format(string word)
        {
            return word.Replace('_', ' ');//.ToUpperInvariant();
        }
    }

    public static IEnumerable<RelatedWord> GetRelatedWords(
        string relatedToWord,
        SynSet synSet,
        WordNetEngine wordNetEngine)
    {
        var synSets = GetPunSynSets(synSet, wordNetEngine, true);

        foreach (var set in synSets)
        foreach (var word in set.Words)
            yield return new RelatedWord(word, relatedToWord, "...", set.Gloss);
    }

    public static IEnumerable<SynSet> GetPunSynSets(SynSet synSet, WordNetEngine engine, bool includeRecursive)
    {
        var oneStepSets   = synSet.GetRelatedSynSets(SingleStepRelations, false, engine);
        var multiStepSets = synSet.GetRelatedSynSets(RecursiveRelations,  true,  engine);

        var sets = oneStepSets.Prepend(synSet);

        if (includeRecursive)
            sets = sets.Concat(multiStepSets);


        return sets.Distinct();
    }

    /// <summary>
    /// Relations that can be followed recursively
    /// </summary>
    private static readonly IReadOnlySet<SynSetRelation> RecursiveRelations =
        new HashSet<SynSetRelation>()
        {
            SynSetRelation.Hyponym,
            SynSetRelation.InstanceHyponym,
            SynSetRelation.RegionDomainMember,
            SynSetRelation.TopicDomainMember,
            SynSetRelation.UsageDomainMember,
        };

    /// <summary>
    /// Relations that should only be followed a single step
    /// </summary>
    private static readonly IReadOnlySet<SynSetRelation> SingleStepRelations =
        new HashSet<SynSetRelation>()
        {
            SynSetRelation.Hypernym,
            SynSetRelation.SimilarTo,
            SynSetRelation.MemberMeronym,
            SynSetRelation.SubstanceHolonym,
            SynSetRelation.PartMeronym,
            SynSetRelation.PartHolonym,
            SynSetRelation.SubstanceHolonym,
            SynSetRelation.MemberHolonym,
            SynSetRelation.RegionDomain,
            SynSetRelation.TopicDomain,
            SynSetRelation.UsageDomain,
            SynSetRelation.AlsoSee,
            SynSetRelation.Cause,
            SynSetRelation.Attribute,
            SynSetRelation.Entailment,
            SynSetRelation.DerivedFromAdjective,
            SynSetRelation.ParticipleOfVerb,
        };
}

}

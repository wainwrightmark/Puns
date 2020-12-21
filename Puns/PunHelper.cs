using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using Pronunciation;
using Puns.Strategies;
using WordNet;

namespace Puns
{
    public static class PunHelper
    {
        public static IReadOnlyList<PunStrategy> GetPunStrategies(SpellingEngine spellingEngine, IReadOnlyList<PhoneticsWord> themeWords)
        {
            var punStrategies = new List<PunStrategy>
            {
                new HomophonePunStrategy(spellingEngine, themeWords),
                new PerfectRhymePunStrategy(spellingEngine,themeWords),
                new PrefixPunStrategy(spellingEngine,themeWords),
                new PrefixRhymePunStrategy(spellingEngine,themeWords),
                new SameConsonantsPunStrategy(spellingEngine,themeWords),
                new InfixRhymePunStrategy(spellingEngine, themeWords)
            };

            return punStrategies;
        }


        public static IReadOnlyCollection<Pun> GetPuns(PunCategory category,
            string theme,
            IReadOnlyCollection<SynSet> synSets,
            WordNetEngine wordNetEngine,
            PronunciationEngine pronunciationEngine,
            SpellingEngine spellingEngine)
        {
            var phrases = GetPhrases(category);

            var themeWords =
                synSets.SelectMany(synSet => GetRelatedWords(theme, synSet, wordNetEngine)
                .Select(x => x.Word))
                .Where(x=>!x.Contains('_'))
                .Prepend(theme)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Except(CommonWords.Value, StringComparer.OrdinalIgnoreCase)
                .Select(pronunciationEngine.GetPhoneticsWord)
                .Where(x=>x is not null)
                .Cast<PhoneticsWord>()
                .Where(x=>x.Syllables.Count > 1 || x.Syllables[0].Symbols.Count > 1)
                .Distinct(WordPronunciationComparer.Instance)
                .ToList();

            var cache = new Dictionary<PhoneticsWord, PunReplacement>();

            var puns = new List<Pun>();

            var punStrategies = GetPunStrategies(spellingEngine, themeWords);


            foreach (var phrase in phrases)
            {
                var words = phrase
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                var wordList = new List<string>();
                var punWords = new HashSet<string>();
                var containsOriginal = false;
                var containsPun = false;

                foreach (var word in words)
                {
                    var bestReplacement = GetBestReplacement(word, pronunciationEngine, cache, punStrategies);

                    if (bestReplacement != null)
                    {
                        var casing = DetectCasing(word);
                        var newString = ToCase(bestReplacement.Value.ReplacementString, casing);
                        wordList.Add(newString);
                        containsOriginal |= bestReplacement.Value.IsAmalgam;
                        containsPun = true;
                        punWords.Add(bestReplacement.Value.PunWord);
                    }
                    else
                    {
                        wordList.Add(word);
                        containsOriginal = true;
                    }
                }

                if(containsPun && (words.Length > 1 || containsOriginal))
                    puns.Add(new Pun(wordList.ToDelimitedString(" "), phrase, punWords));
            }

            return puns;

            static PunReplacement? GetBestReplacement(string word,
                PronunciationEngine pronunciationEngine,
                IDictionary<PhoneticsWord, PunReplacement> cache,
                IEnumerable<PunStrategy> punStrategies)
            {
                if (CommonWords.Value.Contains(word)) return null;
                var cmuWord = pronunciationEngine.GetPhoneticsWord(word);
                if (cmuWord is null) return null;
                if (cmuWord.Syllables.Count < 2 && cmuWord.Syllables[0].Symbols.Count < 3) return null;



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

        private static readonly Lazy<IReadOnlySet<string>> CommonWords = new Lazy<IReadOnlySet<string>>(
            ()=> WordData.CommonWords.Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase));


        public static IReadOnlyCollection<string> GetPhrases(PunCategory category)
        {
            return category switch
            {

                PunCategory.Movies => WordData.Movies.Split("\n",
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                PunCategory.Musicals => WordData.Musicals.Split("\n",
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                PunCategory.Idiom => WordData.Idioms.Split("\n",
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                PunCategory.Bands => WordData.Bands.Split("\n",
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                PunCategory.Books => WordData.Books.Split("\n",
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                PunCategory.Brands => WordData.Brands.Split("\n",
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                PunCategory.Celebs => WordData.Celebs.Split("\n",
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                PunCategory.Countries => WordData.Countries.Split("\n",
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                PunCategory.Artists => WordData.Artists.Split("\n",
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                PunCategory.Songs => WordData.Songs.Split("\n",
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                PunCategory.CountrySongs => WordData.CountrySongs.Split("\n",
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                PunCategory.ChristmasSongs => WordData.ChristmasSongs.Split("\n",
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                PunCategory.TVShows => WordData.TVShows.Split("\n",
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
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
                Casing.Title => System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(s.ToLower()),
                _ => throw new ArgumentOutOfRangeException(nameof(casing), casing, null)
            };
        }

        public static bool IsSameWord(string s1, string s2)
        {
            //TODO improve
            return s1.Equals(s2, StringComparison.OrdinalIgnoreCase) ||
                   (s1 + "s").Equals(s2, StringComparison.OrdinalIgnoreCase) ||
                   (s2 + "s").Equals(s1, StringComparison.OrdinalIgnoreCase);
        }


        public static IEnumerable<RelatedWord> GetRelatedWords(string relatedToWord, SynSet synSet,
            WordNetEngine wordNetEngine)
        {
            var synSets = GetPunSynSets(synSet, wordNetEngine);

            foreach (var set in synSets)
            foreach (var word in set.Words)
                yield return new RelatedWord(word, relatedToWord, "...", set.Gloss);
        }

        public static IEnumerable<SynSet> GetPunSynSets(SynSet synSet, WordNetEngine engine)
        {

            var oneStepSets = synSet.GetRelatedSynSets(SingleStepRelations, false, engine);
            var multiStepSets = synSet.GetRelatedSynSets(RecursiveRelations, true, engine);



            return oneStepSets.Concat(multiStepSets).Prepend(synSet).Distinct();

        }

        /// <summary>
        /// Relations that can be followed recursively
        /// </summary>
        private static readonly IReadOnlySet<SynSetRelation> RecursiveRelations = new HashSet<SynSetRelation>()
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
        private static readonly IReadOnlySet<SynSetRelation> SingleStepRelations = new HashSet<SynSetRelation>()
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

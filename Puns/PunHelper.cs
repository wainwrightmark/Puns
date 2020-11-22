using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using Pronunciation;
using WordNet;

namespace Puns
{
    public static class PunHelper
    {
        public static bool IsPun(PhoneticsWord originalPhoneticsWord, PhoneticsWord replacementPhoneticsWord)
        {
            if (IsSameWord(originalPhoneticsWord.Text, replacementPhoneticsWord.Text))
                return false;

            if (originalPhoneticsWord.Symbols.Count < 2 || replacementPhoneticsWord.Symbols.Count < 2)
                return false;

            if (originalPhoneticsWord.Symbols.Count == replacementPhoneticsWord.Symbols.Count
            ) //same number of syllables
            {
                if (originalPhoneticsWord.Symbols[0] != replacementPhoneticsWord.Symbols[0] &&
                    originalPhoneticsWord.Symbols[^1] != replacementPhoneticsWord.Symbols[^1])
                    return false;

                return originalPhoneticsWord.Symbols.Select(x => x.GetSyllableType())
                    .SequenceEqual(replacementPhoneticsWord.Symbols.Select(x => x.GetSyllableType()));
            }
            else
            {
                if (replacementPhoneticsWord.Symbols.StartsWith(originalPhoneticsWord.Symbols))
                    return true;

                if (originalPhoneticsWord.Symbols.StartsWith(replacementPhoneticsWord.Symbols))
                    return true;

                return false;
            }
        }

        public static IReadOnlyCollection<Pun> GetPuns(PunCategory category,
            string theme,
            SynSet synSet,
            WordNetEngine wordNetEngine,
            PronunciationEngine pronunciationEngine)
        {
            var phrases = GetPhrases(category);

            var themeWords = GetRelatedWords(theme, synSet, wordNetEngine)
                .Select(x => x.Word).Prepend(theme)

                .Distinct(StringComparer.OrdinalIgnoreCase)
                .SelectMany(pronunciationEngine.GetPhoneticsWords)
                .Distinct(WordPronunciationComparer.Instance)
                .ToList();

            var cache = new Dictionary<PhoneticsWord, IReadOnlyCollection<PhoneticsWord>>();

            var puns = new List<Pun>();

            foreach (var phrase in phrases)
            {
                var words = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (words.Length == 1)
                    continue;

                foreach (var word in words)
                {
                    var cmuWord = pronunciationEngine.GetPhoneticsWords(word).FirstOrDefault();
                    if (cmuWord is null) continue;

                    var casing = DetectCasing(word);

                    if (!cache.TryGetValue(cmuWord, out var punWords))
                    {
                        punWords = themeWords.Where(x => IsPun(x, cmuWord)).ToList();
                        cache.Add(cmuWord, punWords);
                    }

                    foreach (var punWord in punWords)
                    {
                        var newString = ToCase(punWord.Text, casing);

                        var newPhrase = phrase.Replace(word, newString);
                        puns.Add(new Pun(newPhrase, phrase, punWord.Text, synSet));
                    }
                }
            }

            return puns;
        }


        public static IReadOnlyCollection<string> GetPhrases(PunCategory category)
        {
            return category switch
            {

                PunCategory.Movies => CategoryLists.Movies.Split("\n",
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                PunCategory.Idiom => CategoryLists.Idioms.Split("\n",
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                PunCategory.Bands => CategoryLists.Bands.Split("\n",
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                PunCategory.Books => CategoryLists.Books.Split("\n",
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

        private static readonly List<SynSetRelation> Relations = new List<SynSetRelation>()
        {
            SynSetRelation.Hyponym,
            SynSetRelation.TopicDomainMember
        };

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

using System;
using System.Collections.Generic;
using System.Linq;
using Pronunciation;
using WordNet;

namespace Puns
{
    public static class PunHelper
    {
        /*
        public static int? GetPunScore(PhoneticsWord originalPhoneticsWord, PhoneticsWord replacementPhoneticsWord)
        {
            if (IsSameWord(originalPhoneticsWord.Text, replacementPhoneticsWord.Text))
                return null;

            static int GetScore(PhoneticsWord shortWord, PhoneticsWord longWord, int offSet)
            {
                var longestStreak = 0;
                var currentStreak = 0;
                var unmatchedSymbols = 0;
                var otherMatches = 0;

                void EndStreak()
                {
                    if (longestStreak < currentStreak)
                    {
                        otherMatches += longestStreak;
                        longestStreak = currentStreak;
                    }
                    else
                        otherMatches += currentStreak;

                    currentStreak = 0;
                }

                for (var i = 0; i < shortWord.Symbols.Count; i++)
                {
                    if (shortWord.Symbols[i] == longWord.Symbols[i + offSet])
                        currentStreak++;
                    else if (shortWord.SyllableTypes.Value[i] == longWord.SyllableTypes.Value[i + offSet])
                    {
                        EndStreak();
                        otherMatches++;
                    }
                    else
                    {
                        EndStreak();
                        unmatchedSymbols++;
                    }
                }

                EndStreak();

                var score = 2 *(Triangle(longestStreak) - Triangle(unmatchedSymbols))  ;

                return score;
            }


            var minLength = Math.Min(originalPhoneticsWord.Symbols.Count, replacementPhoneticsWord.Symbols.Count);

            if (minLength < 3)
                return null;




            var (shortWord, longWord) = originalPhoneticsWord.Symbols.Count <= replacementPhoneticsWord.Symbols.Count
                ? (originalPhoneticsWord, replacementPhoneticsWord)
                : (replacementPhoneticsWord, originalPhoneticsWord);

            var bestScoreSoFar = 0;

            for (var offset = 0; offset <= longWord.Symbols.Count - shortWord.Symbols.Count; offset++)
            {
                var score = GetScore(shortWord, longWord, offset);
                if (score > bestScoreSoFar) bestScoreSoFar = score;
            }



            if (bestScoreSoFar > 2) return bestScoreSoFar;
            return null;
            static int Triangle(int n) => n * (n + 1) / 2; //returns the nth triangular number
        }
        */


        public static IReadOnlyCollection<Pun> GetPuns(PunCategory category,
            string theme,
            IReadOnlyCollection<SynSet> synSets,
            WordNetEngine wordNetEngine,
            PronunciationEngine pronunciationEngine)
        {
            var phrases = GetPhrases(category);

            var themeWords =
                synSets.SelectMany(synSet => GetRelatedWords(theme, synSet, wordNetEngine)
                .Select(x => x.Word))
                .Prepend(theme)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Except(CommonWords.Value, StringComparer.OrdinalIgnoreCase)
                .Select(pronunciationEngine.GetPhoneticsWord)
                .Where(x=>x is not null)
                .Cast<PhoneticsWord>()
                .Where(x=>x.Symbols.Count > 2)
                .Distinct(WordPronunciationComparer.Instance)
                .ToList();

            var cache = new Dictionary<PhoneticsWord, (PhoneticsWord word, PunType type)>();

            var puns = new List<Pun>();

            foreach (var phrase in phrases)
            {
                var words = phrase
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (words.Length == 1)
                    continue;

                foreach (var word in words)
                {
                    if(CommonWords.Value.Contains(word)) continue;
                    var cmuWord = pronunciationEngine.GetPhoneticsWord(word);
                    if (cmuWord is null) continue;
                    if (cmuWord.Symbols.Count < 3) continue;

                    var casing = DetectCasing(word);

                    if (!cache.TryGetValue(cmuWord, out var bestPunWord))
                    {
                        var bestPuns = themeWords.Select(themeWord =>
                            {
                                var punType = PunClassifier.Classify(themeWord, cmuWord);
                                var score = punType?.GetPunScore();

                                return (themeWord, punType, score );
                            })
                            .Where(x => x.score.HasValue)
                            .Where(x=> !(x.themeWord.IsCompound && (x.punType == PunType.Infix || x.punType == PunType.Prefix))) //prevent compound word prefix / infix
                            .OrderByDescending(x => x.score!.Value)
                            .ThenBy(x => x.themeWord.Text.Length);//Shorter words better

                        bestPunWord = bestPuns.Select(x=>(x.themeWord,x.punType!.Value)).FirstOrDefault();
                        cache.Add(cmuWord, bestPunWord);
                    }

                    if (bestPunWord == default) continue;

                    var newString = ToCase(bestPunWord.word.Text, casing);

                    var newPhrase = phrase.Replace(word, newString);
                    puns.Add(new Pun(newPhrase, phrase, bestPunWord.word.Text, bestPunWord.type));

                }
            }

            return puns;
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
                PunCategory.Idiom => WordData.Idioms.Split("\n",
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                PunCategory.Bands => WordData.Bands.Split("\n",
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                PunCategory.Books => WordData.Books.Split("\n",
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

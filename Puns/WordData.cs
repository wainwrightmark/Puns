using System;
using System.Collections.Generic;
using System.Linq;
using WordNet;

namespace Puns
{
    public static class WordData
    {
        public static IEnumerable<SynSet> GetSynsets(string word) => WordNetEngine.Value.GetSynSets(word);

        private static readonly Lazy<WordNetEngine> WordNetEngine = new Lazy<WordNetEngine>(() =>
        {

            var wordNetEngine = new WordNetEngine();
            return wordNetEngine;
            });



        /// <summary>
        /// Is this a noun, verb, adjective, or adverb
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsGoodWord(string s)
        {
            var result = WordNetEngine.Value.GetSynSets(s);

            return result.Any();
        }

        public static bool IsSameWord(string s1, string s2)
        {
            //TODO improve
            return s1.Equals(s2, StringComparison.OrdinalIgnoreCase) || (s1 + "s").Equals(s2, StringComparison.OrdinalIgnoreCase)  || (s2 + "s").Equals(s1, StringComparison.OrdinalIgnoreCase);
        }

        private static readonly List<SynSetRelation> Relations = new List<SynSetRelation>()
        {
            SynSetRelation.Hyponym,
            SynSetRelation.TopicDomainMember
        };

        public static IEnumerable<RelatedWord> GetRelatedWords2(string relatedToWord, SynSet synSet)
        {
            var synSets = synSet.GetRelatedSynSets(Relations, true, WordNetEngine.Value).Prepend(synSet);

            foreach (var set in synSets)
            foreach (var word in set.Words)
                yield return new RelatedWord(word, relatedToWord, "...", set.Gloss);
        }


        public static IEnumerable<RelatedWord> GetRelatedWords(string relatedToWord, SynSet synSet)
        {
            foreach (var w in synSet.Words)
                yield return new RelatedWord(w, relatedToWord, "Synonym", synSet.Gloss);


            foreach (var synSetRelation in synSet.SemanticRelations.Concat(synSet.LexicalRelations))
            {

                var reason = $"{synSetRelation}".Trim();

                foreach (var relatedSynSet in synSet.GetRelatedSynSets(synSetRelation, false, WordNetEngine.Value))
                {
                    foreach (var word in relatedSynSet.Words)
                        yield return new RelatedWord(word, relatedToWord, reason, relatedSynSet.Gloss);

                    if (synSetRelation != SynSetRelation.TopicDomain) continue;

                    foreach (var sameTopicDomainSynSet in relatedSynSet.GetRelatedSynSets(SynSetRelation.TopicDomainMember, false, WordNetEngine.Value))
                    foreach (var word in sameTopicDomainSynSet.Words)
                    {
                        if (IsSingleWord(word))
                        {
                                var topicReason = $"Member of topic: {sameTopicDomainSynSet.Words.First()}";

                                yield return new RelatedWord(word, relatedToWord, topicReason,
                                    sameTopicDomainSynSet.Gloss);
                        }

                    }



                }
            }


        }


        private static bool IsSingleWord(string s) => s.Length > 2 && s.All(char.IsLetter);
    }
}

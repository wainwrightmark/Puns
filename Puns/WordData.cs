using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Syn.Oryzer.LanguageProcessing.WordNet;

namespace Puns
{
    public static class WordData
    {
        public static IEnumerable<SynSet> GetSynsets(string word) => WordNetEngine.Value.GetSynSets(word);

        private static readonly Lazy<WordNetEngine> WordNetEngine = new Lazy<WordNetEngine>(() =>
        {
            var wordNetEngine = new WordNetEngine();

            wordNetEngine.AddDataSource(new StreamReader(new MemoryStream(WordNet.dataAdj)), PartOfSpeech.Adjective);
            wordNetEngine.AddDataSource(new StreamReader(new MemoryStream(WordNet.dataAdv)), PartOfSpeech.Adverb);
            wordNetEngine.AddDataSource(new StreamReader(new MemoryStream(WordNet.dataNoun)), PartOfSpeech.Noun);
            wordNetEngine.AddDataSource(new StreamReader(new MemoryStream(WordNet.dataVerb)), PartOfSpeech.Verb);

            wordNetEngine.AddIndexSource(new StreamReader(new MemoryStream(WordNet.indexAdj)), PartOfSpeech.Adjective);
            wordNetEngine.AddIndexSource(new StreamReader(new MemoryStream(WordNet.indexAdv)), PartOfSpeech.Adverb);
            wordNetEngine.AddIndexSource(new StreamReader(new MemoryStream(WordNet.indexNoun)), PartOfSpeech.Noun);
            wordNetEngine.AddIndexSource(new StreamReader(new MemoryStream(WordNet.indexVerb)), PartOfSpeech.Verb);

            Console.WriteLine(@"Loading word net engine");
            wordNetEngine.Load();

            Console.WriteLine(@"Loaded word net engine");

            return wordNetEngine;
            });



        /// <summary>
        /// Is this a noun, verb, adjective, or adverb
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsGoodWord(string s)
        {
            var result =
            WordNetEngine.Value.GetSynSets(s, PartOfSpeech.Adjective, PartOfSpeech.Adverb, PartOfSpeech.Noun,
                PartOfSpeech.Verb);

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
            var synSets = synSet.GetRelatedSynSets(Relations, true).Prepend(synSet);

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

                foreach (var relatedSynSet in synSet.GetRelatedSynSets(synSetRelation, false))
                {
                    foreach (var word in relatedSynSet.Words)
                        yield return new RelatedWord(word, relatedToWord, reason, relatedSynSet.Gloss);

                    if (synSetRelation != SynSetRelation.TopicDomain) continue;

                    foreach (var sameTopicDomainSynSet in relatedSynSet.GetRelatedSynSets(SynSetRelation.TopicDomainMember, false))
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

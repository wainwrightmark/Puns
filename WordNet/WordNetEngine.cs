using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace WordNet
{
    /// <summary>
    /// SynSet relations
    /// </summary>
    public enum SynSetRelation
    {
        None,
        AlsoSee,
        Antonym,
        Attribute,
        Cause,
        DerivationallyRelated,
        DerivedFromAdjective,
        Entailment,
        Hypernym,
        Hyponym,
        InstanceHypernym,
        InstanceHyponym,
        MemberHolonym,
        MemberMeronym,
        PartHolonym,
        ParticipleOfVerb,
        PartMeronym,
        Pertainym,
        RegionDomain,
        RegionDomainMember,
        SimilarTo,
        SubstanceHolonym,
        SubstanceMeronym,
        TopicDomain,
        TopicDomainMember,
        UsageDomain,
        UsageDomainMember,
        VerbGroup,
    }

    /// <summary>
    /// WordNet parts-of-speech
    /// </summary>
    public enum PartOfSpeech
    {
        None,
        Noun,
        Verb,
        Adjective,
        Adverb
    }

    /// <summary>
    /// Lexicographer file names
    /// </summary>
    public enum LexicographerFileName
    {
        None,
        AdjAll,
        AdjPert,
        AdvAll,
        NounTops,
        NounAct,
        NounAnimal,
        NounArtifact,
        NounAttribute,
        NounBody,
        NounCognition,
        NounCommunication,
        NounEvent,
        NounFeeling,
        NounFood,
        NounGroup,
        NounLocation,
        NounMotive,
        NounObject,
        NounPerson,
        NounPhenomenon,
        NounPlant,
        NounPossession,
        NounProcess,
        NounQuantity,
        NounRelation,
        NounShape,
        NounState,
        NounSubstance,
        NounTime,
        VerbBody,
        VerbChange,
        VerbCognition,
        VerbCommunication,
        VerbCompetition,
        VerbConsumption,
        VerbContact,
        VerbCreation,
        VerbEmotion,
        VerbMotion,
        VerbPerception,
        VerbPossession,
        VerbSocial,
        VerbStative,
        VerbWeather,
        AdjPpl
    }


    public static class Helpers
    {
        /// <summary>
        /// SynSet relation symbols that are available for each POS
        /// </summary>
        public static IReadOnlyDictionary<PartOfSpeech, IReadOnlyDictionary<string, SynSetRelation>> PartOfSpeechSymbolRelationDictionary { get; } = CreatePosSymbolRelationDictionary();

        /// <summary>
        /// Static constructor
        /// </summary>
        private static IReadOnlyDictionary<PartOfSpeech, IReadOnlyDictionary<string, SynSetRelation>> CreatePosSymbolRelationDictionary()
        {
            var dict = new Dictionary<PartOfSpeech, IReadOnlyDictionary<string, SynSetRelation>>();

            // noun relations
            var nounSymbolRelation = new Dictionary<string, SynSetRelation>
            {
                {"!", SynSetRelation.Antonym},
                {"@", SynSetRelation.Hypernym},
                {"@i", SynSetRelation.InstanceHypernym},
                {"~", SynSetRelation.Hyponym},
                {"~i", SynSetRelation.InstanceHyponym},
                {"#m", SynSetRelation.MemberHolonym},
                {"#s", SynSetRelation.SubstanceHolonym},
                {"#p", SynSetRelation.PartHolonym},
                {"%m", SynSetRelation.MemberMeronym},
                {"%s", SynSetRelation.SubstanceMeronym},
                {"%p", SynSetRelation.PartMeronym},
                {"=", SynSetRelation.Attribute},
                {"+", SynSetRelation.DerivationallyRelated},
                {";c", SynSetRelation.TopicDomain},
                {"-c", SynSetRelation.TopicDomainMember},
                {";r", SynSetRelation.RegionDomain},
                {"-r", SynSetRelation.RegionDomainMember},
                {";u", SynSetRelation.UsageDomain},
                {"-u", SynSetRelation.UsageDomainMember},
                {@"\", SynSetRelation.DerivedFromAdjective}
            };
            // appears in WordNet 3.1
            dict.Add(PartOfSpeech.Noun, nounSymbolRelation);

            // verb relations
            var verbSymbolRelation = new Dictionary<string, SynSetRelation>
            {
                {"!", SynSetRelation.Antonym},
                {"@", SynSetRelation.Hypernym},
                {"~", SynSetRelation.Hyponym},
                {"*", SynSetRelation.Entailment},
                {">", SynSetRelation.Cause},
                {"^", SynSetRelation.AlsoSee},
                {"$", SynSetRelation.VerbGroup},
                {"+", SynSetRelation.DerivationallyRelated},
                {";c", SynSetRelation.TopicDomain},
                {";r", SynSetRelation.RegionDomain},
                {";u", SynSetRelation.UsageDomain}
            };
            dict.Add(PartOfSpeech.Verb, verbSymbolRelation);

            // adjective relations
            var adjectiveSymbolRelation = new Dictionary<string, SynSetRelation>
            {
                {"!", SynSetRelation.Antonym},
                {"&", SynSetRelation.SimilarTo},
                {"<", SynSetRelation.ParticipleOfVerb},
                {@"\", SynSetRelation.Pertainym},
                {"=", SynSetRelation.Attribute},
                {"^", SynSetRelation.AlsoSee},
                {";c", SynSetRelation.TopicDomain},
                {";r", SynSetRelation.RegionDomain},
                {";u", SynSetRelation.UsageDomain},
                {"+", SynSetRelation.DerivationallyRelated}
            };
            // not in documentation
            dict.Add(PartOfSpeech.Adjective, adjectiveSymbolRelation);

            // adverb relations
            var adverbSymbolRelation = new Dictionary<string, SynSetRelation>
            {
                {"!", SynSetRelation.Antonym},
                {@"\", SynSetRelation.DerivedFromAdjective},
                {";c", SynSetRelation.TopicDomain},
                {";r", SynSetRelation.RegionDomain},
                {";u", SynSetRelation.UsageDomain},
                {"+", SynSetRelation.DerivationallyRelated}
            };
            // not in documentation
            dict.Add(PartOfSpeech.Adverb, adverbSymbolRelation);

            return dict;
        }

        /// <summary>
        /// Gets the relation for a given POS and symbol
        /// </summary>
        /// <param name="partOfSpeech">POS to get relation for</param>
        /// <param name="symbol">Symbol to get relation for</param>
        /// <returns>SynSet relation</returns>
        public static SynSetRelation GetSynSetRelation(this PartOfSpeech partOfSpeech, string symbol) => PartOfSpeechSymbolRelationDictionary[partOfSpeech][symbol];

    }

    /// <summary>
    /// Provides access to the WordNet resource via two alternative methods, in-memory and disk-based. The latter is blazingly
    /// fast but also hugely inefficient in terms of memory consumption. The latter uses essentially zero memory but is slow
    /// because all searches have to be conducted on-disk.
    /// </summary>
    public class WordNetEngine
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public WordNetEngine()
        {
            var dataFiles = new (byte[] bytes, PartOfSpeech partOfSpeech) []
            {
                (DictionaryFiles.dataAdj, PartOfSpeech.Adjective),
                (DictionaryFiles.dataAdv, PartOfSpeech.Adverb),
                (DictionaryFiles.dataNoun, PartOfSpeech.Noun),
                (DictionaryFiles.dataVerb, PartOfSpeech.Verb),
            };

            var indexFiles = new (byte[] bytes, PartOfSpeech partOfSpeech)[]
            {
                (DictionaryFiles.indexAdj, PartOfSpeech.Adjective),
                (DictionaryFiles.indexAdv, PartOfSpeech.Adverb),
                (DictionaryFiles.indexNoun, PartOfSpeech.Noun),
                (DictionaryFiles.indexVerb, PartOfSpeech.Verb),
            };

            var sw1 = Stopwatch.StartNew();

            Console.WriteLine("Loading wordnet data");

            SynSetDictionary = dataFiles.SelectMany(x => GetSynsetsFromData(x.bytes, x.partOfSpeech))
                .ToDictionary(x => x.id, x => x.sets);

            Console.WriteLine($"Loaded wordnet data ({sw1.ElapsedMilliseconds}ms)");

            static IEnumerable<(string id, Lazy<SynSet> sets)> GetSynsetsFromData(byte[] bytes, PartOfSpeech partOfSpeech)
            {
                using var stream = new MemoryStream(bytes);
                using var reader = new StreamReader(stream, Encoding.UTF8);

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line == null || line.StartsWith(' ')) continue;
                    var pair = SynSet.InstantiateLazy(line, partOfSpeech);
                    yield return pair;
                }
            }

            var sw2 = Stopwatch.StartNew();
            Console.WriteLine("Loading wordnet index");

            WordLookup = indexFiles.SelectMany(x => GetReferencesFromIndex(x.bytes, x.partOfSpeech))
                .ToLookup(x => x.word, x => (x.partOfSpeech, x.synsetId));

            Console.WriteLine($"Loaded wordnet index ({sw2.ElapsedMilliseconds}ms)");

            static IEnumerable<(string word, PartOfSpeech partOfSpeech, string synsetId)> GetReferencesFromIndex(byte[] bytes, PartOfSpeech partOfSpeech)
            {
                using var stream = new MemoryStream(bytes);
                using var reader = new StreamReader(stream, Encoding.UTF8);

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line == null || line.StartsWith(' ')) continue;
                    var fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);


                    var word = fields[0];
                    var numberOfSynSets = int.Parse(fields[2]);

                    var normalizedWord = NormalizeWord(word);

                    for (var i = 0; i < numberOfSynSets; i++)
                    {
                        var id = fields[fields.Length - 1 - i];
                        var synsetId = $"{partOfSpeech}:{id}";
                        yield return (normalizedWord, partOfSpeech, synsetId);
                    }
                }
            }

        }


        /// <summary>
        /// Dictionary mapping synset ids to lazy synsets
        /// </summary>
        public IReadOnlyDictionary<string, Lazy<SynSet>> SynSetDictionary { get; }

        /// <summary>
        /// Lookup mapping words to SynSet ids.
        /// </summary>
        public ILookup<string, (PartOfSpeech partOfSpeech, string synsetId)> WordLookup { get; }

        private static string NormalizeWord(string word) => word.ToLower().Replace(' ', '_');

        #region synset retrieval

        /// <summary>
        /// Gets all synsets for a word, optionally restricting the returned synsets to one or more parts of speech. This
        /// method does not perform any morphological analysis to match up the given word. It does, however, replace all
        /// spaces with underscores and call String.ToLower to normalize case.
        /// </summary>
        /// <param name="word">Word to get SynSets for. This method will replace all spaces with underscores and
        /// call ToLower() to normalize the word's case.</param>
        /// <param name="posRestriction">POSs to search. Cannot contain POS.None. Will search all POSs if no restriction
        /// is given.</param>
        /// <returns>Set of SynSets that contain word</returns>
        public IEnumerable<SynSet> GetSynSets(string word, params PartOfSpeech[] posRestriction)
        {
            var niceWord = NormalizeWord(word);

            foreach (var (partOfSpeech, synsetId) in WordLookup[niceWord])
            {
                if (posRestriction.Any() && !posRestriction.Contains(partOfSpeech)) continue;

                var synSet =  SynSetDictionary[synsetId].Value;

                yield return synSet;
            }
        }

        /*
        /// <summary>
        /// Gets the most common synset for a given word/pos pair. This is only available for memory-based
        /// engines (see constructor).
        /// </summary>
        /// <param name="word">Word to get SynSets for. This method will replace all spaces with underscores and
        /// will call String.ToLower to normalize case.</param>
        /// <param name="partOfSpeech">Part of speech to find</param>
        /// <returns>Most common synset for given word/pos pair</returns>
        public SynSet GetMostCommonSynSet(string word, PartOfSpeech partOfSpeech)
        {
            // all words are lower case and space-replaced...we need to do this here, even though it gets done in GetSynSets (we use it below)
            word = word.ToLower().Replace(' ', '_');

            // get synsets for word-pos pair
            var synsets = GetSynSets(word, partOfSpeech);

            // get most common synset
            SynSet mostCommon = null;
            if (synsets.Count == 1)
                return synsets.First();
            else if (synsets.Count > 1)
            {
                // one (and only one) of the synsets should be flagged as most common
                foreach (var synset in synsets)
                    if (synset.IsMostCommonSynsetFor(word))
                        if (mostCommon == null)
                            mostCommon = synset;
                        else
                            throw new Exception("Multiple most common synsets found");

                if (mostCommon == null)
                    throw new NullReferenceException("Failed to find most common synset");
            }

            return mostCommon;
        }
        */

        #endregion
    }
}
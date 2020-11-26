using System.Collections.Generic;

namespace WordNet
{
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
                {@"\", SynSetRelation.DerivedFromAdjective},
                {"^", SynSetRelation.AlsoSee},
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
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WordNet
{

    //public static class SynSetExtension
    //{
    //    public static string GetShortDescription(this SynSet synSet, IReadOnlyCollection<SynSet> allSynSets)
    //    {
    //        //var uniqueWords =
    //        //    synSet.Words.Where(word => allSynSets.All(s => s == synSet || !s.Words.Contains(word, StringComparer.OrdinalIgnoreCase))).ToHashSet();

    //        //if (uniqueWords.Any())
    //        //    return $"e.g. {string.Join(", ", uniqueWords)}";

    //        return synSet.Gloss;
    //    }
    //}

    /// <summary>
    /// Represents a WordNet synset
    /// </summary>
    public class SynSet
    {
        /// <summary>
        /// Checks whether two synsets are equal
        /// </summary>
        /// <param name="synset1">First synset</param>
        /// <param name="synset2">Second synset</param>
        /// <returns>True if synsets are equal, false otherwise</returns>
        public static bool operator ==(SynSet? synset1, SynSet? synset2)
        {
            // check object reference
            if (ReferenceEquals(synset1, synset2))
                return true;

            if (synset1 is null && synset2 is null)
                return true;
            if (synset2 is null || synset1 is null)
                return false;

            return synset1.Equals(synset2);
        }

        /// <summary>
        /// Checks whether two synsets are unequal
        /// </summary>
        /// <param name="synset1">First synset</param>
        /// <param name="synset2">Second synset</param>
        /// <returns>True if synsets are unequal, false otherwise</returns>
        public static bool operator !=(SynSet synset1, SynSet synset2) => !(synset1 == synset2);

        private SynSet(
            SynsetId id,
            IReadOnlyList<string> words,
            string gloss,
            LexicographerFileName lexicographerFileName,
            ILookup<SynSetRelation, SynsetId> relationSynSets,
            ILookup<SynSetRelation, (SynsetId synSetId, int sourceWordIndex, int targetWordIndex)> lexicalRelations)
        {
            Id = id;
            Words = words;
            Gloss = gloss;
            LexicographerFileName = lexicographerFileName;
            _relationSynSets = relationSynSets;
            _lexicalRelations = lexicalRelations;
        }


        /// <summary>
        /// Gets the ID of this synset in the form POS:Offset
        /// </summary>
        public SynsetId Id { get; }

        /// <summary>
        /// Gets semantic relations that exist between this synset and other synsets
        /// </summary>
        public IEnumerable<SynSetRelation> SemanticRelations => _relationSynSets.Select(x=>x.Key);

        /// <summary>
        /// Gets lexical relations that exist between words in this synset and words in another synset
        /// </summary>
        public IEnumerable<SynSetRelation> LexicalRelations => _lexicalRelations.Select(x=>x.Key);

        /// <summary>
        /// Gets the lexicographer file name for this synset (see the lexnames file in the WordNet distribution).
        /// </summary>
        public LexicographerFileName LexicographerFileName { get; }


        /// <summary>
        /// Gets the POS of the current synset
        /// </summary>
        public PartOfSpeech PartOfSpeech => Id.PartOfSpeech;


        /// <summary>
        /// Gets the gloss of the current SynSet
        /// </summary>
        public string Gloss { get; }

        /// <summary>
        /// Gets the words in the current SynSet
        /// </summary>
        public IReadOnlyList<string> Words { get; }

        private readonly ILookup<SynSetRelation, SynsetId> _relationSynSets;
        private readonly ILookup<SynSetRelation, (SynsetId synSetId, int sourceWordIndex, int targetWordIndex)> _lexicalRelations;


        /// <summary>
        /// Instantiates the current synset. If idSynset is non-null, related synsets references are set to those from
        /// idSynset; otherwise, related synsets are created as shells.
        /// </summary>
        /// <param name="definition">Definition line of synset from data file</param>
        /// <param name="partOfSpeech">Part of speech to use</param>
        public static SynSet Instantiate(string definition, PartOfSpeech partOfSpeech)
        {
            var offset = int.Parse(GetField(definition, 0));
            var id = new SynsetId(partOfSpeech, offset);

            /* get lexicographer file name...the enumeration lines up precisely with the wordnet spec (see the lexnames file) except that
             * it starts with None, so we need to add 1 to the definition line's value to get the correct file name */
            var lexicographerFileNumber = int.Parse(GetField(definition, 1)) + 1;
            if (lexicographerFileNumber <= 0)
                throw new Exception("Invalid lexicographer file name number. Should be >= 1.");

            var lexicographerFileName = (LexicographerFileName)lexicographerFileNumber;

            // get number of words in the synset and the start character of the word list
            var numWords = int.Parse(GetField(definition, 3, out var wordStart), NumberStyles.HexNumber);
            wordStart = definition.IndexOf(' ', wordStart) + 1;

            // get words in synset
            var words = new List<string>(numWords);
            for (var i = 0; i < numWords; ++i)
            {
                var wordEnd = definition.IndexOf(' ', wordStart + 1) - 1;
                var wordLen = wordEnd - wordStart + 1;
                var word = definition.Substring(wordStart, wordLen);
                if (word.Contains(' '))
                    throw new Exception("Unexpected space in word:  " + word);

                words.Add(word);

                // skip lex_id field
                wordStart = definition.IndexOf(' ', wordEnd + 2) + 1;
            }

            // get gloss
            var  gloss = definition[(definition.IndexOf('|') + 1)..].Trim();

            // get number and start of relations
            var relationCountField = 3 + (words.Count * 2) + 1;

            var numRelationString = GetField(definition, relationCountField, out var relationFieldStart);
            var numRelations = int.Parse(numRelationString);
            relationFieldStart = definition.IndexOf(' ', relationFieldStart) + 1;

            // grab each related synset
            var relationSynSets = new List<(SynSetRelation relation, SynsetId relatedSetId)>();
            var lexicalRelationSynSets = new List<(SynSetRelation relation, SynsetId relatedSetId, int sourceWordId, int targetWordId) >();
            for (var relationNum = 0; relationNum < numRelations; ++relationNum)
            {
                static string GetNextFieldValue(string definition, ref int fieldStart)
                {
                    var fieldEnd = definition.IndexOf(' ', fieldStart + 1) - 1;
                    var fieldLen = fieldEnd - fieldStart + 1;

                    var fieldValue = definition.Substring(fieldStart, fieldLen);

                    fieldStart = definition.IndexOf(' ', fieldStart + 1) + 1;
                    return fieldValue;
                }
                var relationSymbol = GetNextFieldValue(definition, ref relationFieldStart);
                var relatedSynSetOffset = int.Parse(GetNextFieldValue(definition, ref relationFieldStart));
                var relatedSynSetPartOfSpeech = GetPartOfSpeech(GetNextFieldValue(definition, ref relationFieldStart) );
                var indexes = GetNextFieldValue(definition, ref relationFieldStart);
                var sourceWordIndex = int.Parse(indexes.Substring(0, 2), NumberStyles.HexNumber);
                var targetWordIndex = int.Parse(indexes[2..], NumberStyles.HexNumber);


                var relatedSynSetId = new SynsetId(relatedSynSetPartOfSpeech, relatedSynSetOffset);

                // get relation
                var relation = partOfSpeech.GetSynSetRelation(relationSymbol);

                // add semantic relation if we have neither a source nor a target word index
                if (sourceWordIndex == 0 && targetWordIndex == 0)
                    relationSynSets.Add((relation, relatedSynSetId));
                // add lexical relation
                else
                    lexicalRelationSynSets.Add((relation, relatedSynSetId, sourceWordIndex, targetWordIndex));
            }

            var relationSynSetsLookup = relationSynSets.ToLookup(x => x.relation, x => x.relatedSetId);
            var lexicalSynSetsLookup = lexicalRelationSynSets.ToLookup(x=>x.relation, x=> (x.relatedSetId, x.sourceWordId, x.targetWordId));

            return new SynSet(id, words, gloss, lexicographerFileName, relationSynSetsLookup, lexicalSynSetsLookup);
        }

        /// <summary>
        /// Gets a space-delimited field from a synset definition line
        /// </summary>
        /// <param name="line">SynSet definition line</param>
        /// <param name="fieldNum">Number of field to get</param>
        /// <returns>Field value</returns>
        private static string GetField(string line, int fieldNum) => GetField(line, fieldNum, out _);

        /// <summary>
        /// Gets a space-delimited field from a synset definition line
        /// </summary>
        /// <param name="line">SynSet definition line</param>
        /// <param name="fieldNum">Number of field to get</param>
        /// <param name="startIndex">Start index of field within the line</param>
        /// <returns>Field value</returns>
        private static string GetField(string line, int fieldNum, out int startIndex)
        {
            try
            {
                if (fieldNum < 0)
                    throw new Exception("Invalid field number:  " + fieldNum);

                // scan fields until we hit the one we want
                var currentField = 0;
                startIndex = 0;
                while (true)
                {
                    if (currentField == fieldNum)
                    {
                        // get the end of the field
                        var endIndex = line.IndexOf(' ', startIndex + 1) - 1;

                        // watch out for end of line
                        if (endIndex < 0)
                            endIndex = line.Length - 1;

                        // get length of field
                        var fieldLen = endIndex - startIndex + 1;

                        // return field value
                        return line.Substring(startIndex, fieldLen);
                    }

                    // move to start of next field (one beyond next space)
                    startIndex = line.IndexOf(' ', startIndex) + 1;

                    // if there are no more spaces and we haven't found the field, the caller requested an invalid field
                    if (startIndex == 0)
                        throw new Exception("Failed to get field number:  " + fieldNum);

                    ++currentField;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine(@$"Error in GetField('{line}', '{fieldNum}')");
                throw;
            }
        }

        /// <summary>
        /// Gets the POS from its code
        /// </summary>
        /// <param name="pos">POS code</param>
        /// <returns>POS</returns>
        private static PartOfSpeech GetPartOfSpeech(string pos)
        {
            return pos switch
            {
                "n" => PartOfSpeech.Noun,
                "v" => PartOfSpeech.Verb,
                "a" => PartOfSpeech.Adjective,
                "s" => PartOfSpeech.Adjective,
                "r" => PartOfSpeech.Adverb,
                _ => throw new Exception("Unexpected POS:  " + pos)
            };
        }


        /// <summary>
        /// Gets the number of synsets related to the current one by the given relation
        /// </summary>
        /// <param name="relation">Relation to check</param>
        /// <returns>Number of synset related to the current one by the given relation</returns>
        public int GetRelatedSynSetCount(SynSetRelation relation) => _relationSynSets[relation].Count();

        /// <summary>
        /// Gets synsets related to the current synset
        /// </summary>
        /// <param name="relation">Synset relation to follow</param>
        /// <param name="recursive">Whether or not to follow the relation recursively for all related synsets</param>
        /// <param name="engine">Word net engine</param>
        /// <returns>Synsets related to the given one by the given relation</returns>
        public IEnumerable<SynSet> GetRelatedSynSets(SynSetRelation relation, bool recursive, WordNetEngine engine) => GetRelatedSynSets(new[] { relation }, recursive, engine);

        /// <summary>
        /// Gets synsets related to the current synset
        /// </summary>
        /// <param name="relations">Synset relations to follow</param>
        /// <param name="recursive">Whether or not to follow the relations recursively for all related synsets</param>
        /// <param name="engine">Word net engine</param>
        /// <returns>Synsets related to the given one by the given relations</returns>
        public IEnumerable<SynSet> GetRelatedSynSets(IReadOnlyCollection<SynSetRelation> relations, bool recursive, WordNetEngine engine)
        {
            if (relations.Count == 0)
            {
                relations = Enum.GetValues<SynSetRelation>();
                recursive = false; //Don't allow recursion for all relations
            }

            var visited = new HashSet<SynsetId>{Id};
            var toDo = new Stack<SynSet>();
            toDo.Push(this);

            while (toDo.TryPop(out var ss))
            {
                // try each relation
                foreach (var relation in relations)
                    foreach (var relatedSynset in ss._relationSynSets[relation])
                        // only add synset if it isn't already present (wordnet contains cycles)
                        if (visited.Add(relatedSynset))
                        {
                            var ss2 = engine.GetSynset(relatedSynset);
                            yield return ss2;

                            if (recursive) toDo.Push(ss2);
                        }
            }
        }



        ///// <summary>
        ///// Gets the shortest path from the current synset to another, following the given synset relations.
        ///// </summary>
        ///// <param name="destination">Destination synset</param>
        ///// <param name="relations">Relations to follow, or null for all relations.</param>
        ///// <returns>Synset path, or null if none exists.</returns>
        //public List<SynSet> GetShortestPathTo(SynSet destination, IEnumerable<SynSetRelation> relations)
        //{
        //    if (relations == null)
        //        relations = Enum.GetValues(typeof(SynSetRelation)) as SynSetRelation[];

        //    // make sure the backpointer on the current synset is null - can't predict what other functions might do
        //    _searchBackPointer = null;

        //    // avoid cycles
        //    var synsetsEncountered = new HashSet<SynSet>();
        //    synsetsEncountered.Add(this);

        //    // start search queue
        //    var searchQueue = new Queue<SynSet>();
        //    searchQueue.Enqueue(this);

        //    // run search
        //    List<SynSet> path = null;
        //    while (searchQueue.Count > 0 && path == null)
        //    {
        //        var currSynSet = searchQueue.Dequeue();

        //        // see if we've finished the search
        //        if (currSynSet == destination)
        //        {
        //            // gather synsets along path
        //            path = new List<SynSet>();
        //            while (currSynSet != null)
        //            {
        //                path.Add(currSynSet);
        //                currSynSet = currSynSet.SearchBackPointer;
        //            }

        //            // reverse for the correct order
        //            path.Reverse();
        //        }
        //        // expand the search one level
        //        else
        //            foreach (var synset in currSynSet.GetRelatedSynSets(relations, false))
        //                if (!synsetsEncountered.Contains(synset))
        //                {
        //                    synset.SearchBackPointer = currSynSet;
        //                    searchQueue.Enqueue(synset);

        //                    synsetsEncountered.Add(synset);
        //                }
        //    }

        //    // null-out all search backpointers
        //    foreach (var synset in synsetsEncountered)
        //        synset.SearchBackPointer = null;

        //    return path;
        //}

        ///// <summary>
        ///// Gets the closest synset that is reachable from the current and another synset along the given relations. For example,
        ///// given two synsets and the Hypernym relation, this will return the lowest synset that is a hypernym of both synsets. If
        ///// the hypernym hierarchy forms a tree, this will be the lowest common ancestor.
        ///// </summary>
        ///// <param name="synset">Other synset</param>
        ///// <param name="relations">Relations to follow</param>
        ///// <returns>Closest mutually reachable synset</returns>
        //public SynSet GetClosestMutuallyReachableSynset(SynSet synset, IEnumerable<SynSetRelation> relations)
        //{
        //    // avoid cycles
        //    var synsetsEncountered = new HashSet<SynSet>();
        //    synsetsEncountered.Add(this);

        //    // start search queue
        //    var searchQueue = new Queue<SynSet>();
        //    searchQueue.Enqueue(this);

        //    // run search
        //    SynSet closest = null;
        //    while (searchQueue.Count > 0 && closest == null)
        //    {
        //        var currSynSet = searchQueue.Dequeue();

        //        /* check for a path between the given synset and the current one. if such a path exists, the current
        //         * synset is the closest mutually reachable synset. */
        //        if (synset.GetShortestPathTo(currSynSet, relations) != null)
        //            closest = currSynSet;
        //        // otherwise, expand the search along the given relations
        //        else
        //            foreach (var relatedSynset in currSynSet.GetRelatedSynSets(relations, false))
        //                if (!synsetsEncountered.Contains(relatedSynset))
        //                {
        //                    searchQueue.Enqueue(relatedSynset);
        //                    synsetsEncountered.Add(relatedSynset);
        //                }
        //    }

        //    return closest;
        //}

        /// <summary>
        /// Computes the depth of the current synset following a set of relations. Returns the minimum of all possible depths. Root nodes
        /// have a depth of zero.
        /// </summary>
        /// <param name="relations">Relations to follow</param>
        /// <param name="engine">Word net engine</param>
        /// <returns>Depth of current SynSet</returns>
        public int GetDepth(IReadOnlyCollection<SynSetRelation> relations, WordNetEngine engine)
        {
            var synsets = new HashSet<SynSet> {this};

            return GetDepth(relations, ref synsets, engine);
        }

        /// <summary>
        /// Computes the depth of the current synset following a set of relations. Returns the minimum of all possible depths. Root
        /// nodes have a depth of zero.
        /// </summary>
        /// <param name="relations">Relations to follow</param>
        /// <param name="synsetsEncountered">Synsets that have already been encountered. Prevents cycles from being entered.</param>
        /// <param name="engine">Word net engine</param>
        /// <returns>Depth of current SynSet</returns>
        private int GetDepth(IReadOnlyCollection<SynSetRelation> relations, ref HashSet<SynSet> synsetsEncountered, WordNetEngine engine)
        {
            // get minimum depth through all relatives
            var minimumDepth = -1;
            foreach (var relatedSynset in GetRelatedSynSets(relations, false, engine))
                if (!synsetsEncountered.Contains(relatedSynset))
                {
                    // add this before recursing in order to avoid cycles
                    synsetsEncountered.Add(relatedSynset);

                    // get depth from related synset
                    var relatedDepth = relatedSynset.GetDepth(relations, ref synsetsEncountered, engine);

                    // use depth if it's the first or it's less than the current best
                    if (minimumDepth == -1 || relatedDepth < minimumDepth)
                        minimumDepth = relatedDepth;
                }

            // depth is one plus minimum depth through any relative synset...for synsets with no related synsets, this will be zero
            return minimumDepth + 1;
        }

        ///// <summary>
        ///// Gets lexically related words for the current synset. Many of the relations in WordNet are lexical instead of semantic. Whereas
        ///// the latter indicate relations between entire synsets (e.g., hypernym), the former indicate relations between specific
        ///// words in synsets. This method retrieves all lexical relations and the words related thereby.
        ///// </summary>
        ///// <returns>Mapping from relations to mappings from words in the current synset to related words in the related synsets</returns>
        //public Dictionary<SynSetRelation, Dictionary<string, HashSet<string>>> GetLexicallyRelatedWords()
        //{
        //    var relatedWords = new Dictionary<SynSetRelation, Dictionary<string, HashSet<string>>>();
        //    foreach (var relation in _lexicalRelations.Keys)
        //    {
        //        relatedWords.EnsureContainsKey(relation, typeof(Dictionary<string, HashSet<string>>));

        //        foreach (var relatedSynSet in _lexicalRelations[relation].Keys)
        //        {
        //            // make sure related synset is initialized
        //            if (!relatedSynSet.Instantiated)
        //                relatedSynSet.Instantiate();

        //            foreach (var sourceWordIndex in _lexicalRelations[relation][relatedSynSet].Keys)
        //            {
        //                var sourceWord = _words[sourceWordIndex - 1];

        //                relatedWords[relation].EnsureContainsKey(sourceWord, typeof(HashSet<string>), false);

        //                foreach (var targetWordIndex in _lexicalRelations[relation][relatedSynSet][sourceWordIndex])
        //                {
        //                    var targetWord = relatedSynSet.Words[targetWordIndex - 1];
        //                    relatedWords[relation][sourceWord].Add(targetWord);
        //                }
        //            }
        //        }
        //    }

        //    return relatedWords;
        //}

        /// <summary>
        /// Gets hash code for this synset
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode() => Id.GetHashCode();

        /// <summary>
        /// Checks whether the current synset equals another
        /// </summary>
        /// <param name="obj">Other synset</param>
        /// <returns>True if equal, false otherwise</returns>
        public override bool Equals(object? obj)
        {
            if (obj is SynSet synSet) return Id == synSet.Id;
            return false;
        }

        /// <summary>
        /// Gets description of synset
        /// </summary>
        /// <returns>Description</returns>
        public override string ToString()
        {
            var desc = new StringBuilder();

            desc.Append('{');
            var prependComma = false;
            foreach (var word in Words)
            {
                desc.Append((prependComma ? ", " : "") + word);
                prependComma = true;
            }

            desc.Append("}:  " + Gloss);

            return desc.ToString();
        }

    }
}
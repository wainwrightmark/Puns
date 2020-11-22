using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace WordNet
{
    public readonly struct SynsetId : IEquatable<SynsetId>
    {
        public SynsetId(PartOfSpeech partOfSpeech, uint id)
        {
            PartOfSpeech = partOfSpeech;
            Id = id;
        }

        public PartOfSpeech PartOfSpeech { get; }

        public uint Id { get; }

        /// <inheritdoc />
        public bool Equals(SynsetId other) => PartOfSpeech == other.PartOfSpeech && Id == other.Id;

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is SynsetId other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine((int) PartOfSpeech, Id);

        public static bool operator ==(SynsetId left, SynsetId right) => left.Equals(right);

        public static bool operator !=(SynsetId left, SynsetId right) => !left.Equals(right);

        /// <inheritdoc />
        public override string ToString() => $"{PartOfSpeech}:{Id}";
    }

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

            Console.WriteLine(@"Loading wordnet data");

            SynSetDictionary = dataFiles.SelectMany(x => GetSynsetsFromData(x.bytes, x.partOfSpeech))
                .ToDictionary(x => x.id, x => x.sets);

            Console.WriteLine(@$"Loaded wordnet data ({sw1.ElapsedMilliseconds}ms)");

            static IEnumerable<(SynsetId id, Lazy<SynSet> sets)> GetSynsetsFromData(byte[] bytes, PartOfSpeech partOfSpeech)
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
            Console.WriteLine(@"Loading wordnet index");

            WordLookup = indexFiles.SelectMany(x => GetReferencesFromIndex(x.bytes, x.partOfSpeech))
                .GroupBy(x=>x.word, x=>x.ids)
                .ToDictionary(x=>x.Key, GroupLazy);

            Console.WriteLine(@$"Loaded wordnet index ({sw2.ElapsedMilliseconds}ms)");

            static IEnumerable<(string word, Lazy<IReadOnlyCollection<SynsetId>> ids)> GetReferencesFromIndex(byte[] bytes, PartOfSpeech partOfSpeech)
            {
                using var stream = new MemoryStream(bytes);
                using var reader = new StreamReader(stream, Encoding.UTF8);

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line == null || line.StartsWith(' ')) continue;

                    var spaceIndex = line.IndexOf(' ');
                    if(spaceIndex == -1) continue;

                    var word = line.Substring(0, spaceIndex);
                    var normalizedWord = NormalizeWord(word);

                    var list = new Lazy<IReadOnlyCollection<SynsetId>>(()=> GetIds(line, partOfSpeech));

                    yield return (normalizedWord, list);
                }

                static IReadOnlyCollection<SynsetId> GetIds(string l, PartOfSpeech partOfSpeech)
                {
                    var fields = l.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    var numberOfSynSets = int.Parse(fields[2]);

                    var ids = new List<SynsetId>();

                    for (var i = 0; i < numberOfSynSets; i++)
                    {
                        var id = uint.Parse(fields[fields.Length - 1 - i]);
                        var synsetId = new SynsetId(partOfSpeech, id);
                        ids.Add(synsetId);
                    }

                    return ids;
                }
            }

        }

        private static Lazy<IReadOnlyCollection<T>> GroupLazy<T>(IEnumerable<Lazy<IReadOnlyCollection<T>>> stuff)
        {
            using var enumerator = stuff.GetEnumerator();

            if(!enumerator.MoveNext())//zero elements
                return new Lazy<IReadOnlyCollection<T>>(Array.Empty<T>);

            var first = enumerator.Current;

            if (!enumerator.MoveNext()) // one element
                return first;

            //many
            var list = new List<Lazy<IReadOnlyCollection<T>>>()
            {
                first, enumerator.Current
            };

            while (enumerator.MoveNext()) list.Add(enumerator.Current);


            return new Lazy<IReadOnlyCollection<T>>(()=> list.SelectMany(x=>x.Value).ToList());
        }


        /// <summary>
        /// Dictionary mapping synset ids to lazy synsets
        /// </summary>
        private IReadOnlyDictionary<SynsetId, Lazy<SynSet>> SynSetDictionary { get; }

        /// <summary>
        /// Lookup mapping words to SynSet ids.
        /// </summary>
        private IReadOnlyDictionary<string, Lazy<IReadOnlyCollection<SynsetId>>> WordLookup { get; }

        private static string NormalizeWord(string word) => word.ToLower().Replace(' ', '_');

        #region synset retrieval

        public SynSet GetSynset(SynsetId id) => SynSetDictionary[id].Value;

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

            if (!WordLookup.TryGetValue(niceWord, out var sets)) yield break;

            foreach (var  synsetId in sets.Value)
            {
                if (posRestriction.Any() && !posRestriction.Contains(synsetId.PartOfSpeech)) continue;

                var synSet = SynSetDictionary[synsetId].Value;
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
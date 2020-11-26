using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WordNet
{
    public sealed class WordNetEngine : IDisposable
    {
        public WordNetEngine()
        {
            var dataFiles = new (byte[] bytes, PartOfSpeech partOfSpeech)[]
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

            SynSetDictionary = dataFiles.ToDictionary(x=>x.partOfSpeech,
                x=> new FileDatabase.Database<SynSet, int>(
                    x.bytes,
                    Encoding.UTF8,
                    s=>s.Id.Id,
                    s=>SynSet.Instantiate(s, x.partOfSpeech) ));

            IndexDictionary = indexFiles.ToDictionary(x => x.partOfSpeech,

                x => new FileDatabase.Database<IndexEntry, (string, PartOfSpeech)>(x.bytes,
                    Encoding.UTF8,
                    i => (i.Word, i.PartOfSpeech),
                    s => IndexEntry.CreateFromLine(s, x.partOfSpeech)
                )
            );

        }

        public readonly IReadOnlyDictionary<PartOfSpeech, FileDatabase.Database<SynSet, int>> SynSetDictionary;

        public readonly IReadOnlyDictionary<PartOfSpeech, FileDatabase.Database<IndexEntry, (string, PartOfSpeech)>> IndexDictionary;


        private static string NormalizeWord(string word) => word.ToLower().Replace(' ', '_');

        public SynSet GetSynset(SynsetId id)
        {
            var db = SynSetDictionary[id.PartOfSpeech];

            var r = db[id.Id];

            if(r is null)
                throw new Exception();

            return r;
        }
        public IEnumerable<SynSet> GetSynSets(string word)
        {
            var normWord = NormalizeWord(word);
            var ids = new HashSet<SynsetId>();

            foreach (var (partOfSpeech, database) in IndexDictionary)
            {
                var indexEntry = database[(normWord, partOfSpeech)];
                if (indexEntry is null) continue;

                foreach (var indexEntrySynsetId in indexEntry.SynsetIds)
                    ids.Add(indexEntrySynsetId);
            }

            foreach (var synsetId in ids)
            {
                var ssdb = SynSetDictionary[synsetId.PartOfSpeech];
                var synSet = ssdb[synsetId.Id];

                if (synSet is not null)
                    yield return synSet;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var db in IndexDictionary.Values)
                db.Dispose();
            foreach (var db in SynSetDictionary.Values) db.Dispose();
        }
    }
}
﻿using System;
using System.Collections.Generic;

namespace WordNet
{
    public sealed class IndexEntry
    {
        public static IndexEntry CreateFromLine(string l, PartOfSpeech partOfSpeech)
        {
            var fields = l.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var numberOfSynSets = int.Parse(fields[2]);

            var ids = new HashSet<SynsetId>();

            for (var i = 0; i < numberOfSynSets; i++)
            {
                var id = int.Parse(fields[fields.Length - 1 - i]);
                var synsetId = new SynsetId(partOfSpeech, id);
                ids.Add(synsetId);
            }

            var word = fields[0];

            return new IndexEntry(word, partOfSpeech, ids);
        }


        private IndexEntry(string word, PartOfSpeech partOfSpeech, IReadOnlySet<SynsetId> synsetIds)
        {
            Word = word;
            PartOfSpeech = partOfSpeech;
            SynsetIds = synsetIds;
        }

        public string Word { get; }
        public PartOfSpeech PartOfSpeech { get; }

        public IReadOnlySet<SynsetId> SynsetIds { get; }

        /// <inheritdoc />
        public override string ToString() => Word;
    }
}
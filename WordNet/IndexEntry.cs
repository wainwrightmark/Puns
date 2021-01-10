using System;
using System.Collections.Generic;

namespace WordNet
{

public sealed class IndexEntry
{
    public static IndexEntry CreateFromLine(string l, PartOfSpeech partOfSpeech)
    {
        var fields = l.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var numberOfSynSets = int.Parse(fields[2]);

        var ids = new List<SynsetId>();

        for (var i = numberOfSynSets - 1; i >= 0; i--)
        {
            var id       = int.Parse(fields[fields.Length - 1 - i]);
            var synsetId = new SynsetId(partOfSpeech, id);
            ids.Add(synsetId);
        }

        var word = fields[0];

        return new IndexEntry(word, partOfSpeech, ids);
    }

    public static string GetKeyFromLine(string definition)
    {
        var spaceIndex = definition.IndexOf(' ');
        var s = definition.Substring(0, spaceIndex);
        return s;
    }

    private IndexEntry(string word, PartOfSpeech partOfSpeech, IReadOnlyList<SynsetId> synsetIds)
    {
        Word         = word;
        PartOfSpeech = partOfSpeech;
        SynsetIds    = synsetIds;
    }

    public string Word { get; }
    public PartOfSpeech PartOfSpeech { get; }

    public IReadOnlyList<SynsetId> SynsetIds { get; }

    /// <inheritdoc />
    public override string ToString() => Word;
}

}

using System;
using System.Collections.Generic;
using System.Linq;

namespace CMU
{
    public sealed class WordPronunciationComparer : IEqualityComparer<PhoneticsWord>
    {
        private WordPronunciationComparer() {}

        public static IEqualityComparer<PhoneticsWord> Instance { get; } = new WordPronunciationComparer();

        public bool Equals(PhoneticsWord x, PhoneticsWord y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            return x.Symbols.SequenceEqual(y.Symbols);
        }

        public int GetHashCode(PhoneticsWord phoneticsWord) => HashCode.Combine(phoneticsWord.Symbols.Count, phoneticsWord.Symbols[0], phoneticsWord.Symbols[^1]);
    }
}
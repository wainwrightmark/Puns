using System;
using System.Collections.Generic;
using System.Linq;

namespace CMU
{
    public sealed class WordPronunciationComparer : IEqualityComparer<Word>
    {
        private WordPronunciationComparer() {}

        public static IEqualityComparer<Word> Instance { get; } = new WordPronunciationComparer();

        public bool Equals(Word x, Word y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            return x.Symbols.SequenceEqual(y.Symbols);
        }

        public int GetHashCode(Word word) => HashCode.Combine(word.Symbols.Count, word.Symbols[0], word.Symbols[^1]);
    }
}
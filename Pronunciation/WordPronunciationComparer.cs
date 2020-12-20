using System;
using System.Collections.Generic;
using System.Linq;

namespace Pronunciation
{
    public sealed class WordPronunciationComparer : IEqualityComparer<PhoneticsWord>
    {
        private WordPronunciationComparer() {}

        public static IEqualityComparer<PhoneticsWord> Instance { get; } = new WordPronunciationComparer();

        public bool Equals(PhoneticsWord? x, PhoneticsWord? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            return x.Syllables.SequenceEqual(y.Syllables);
        }

        public int GetHashCode(PhoneticsWord phoneticsWord) => HashCode.Combine(phoneticsWord.Syllables.Count, phoneticsWord.Syllables[0], phoneticsWord.Syllables[^1]);
    }
}
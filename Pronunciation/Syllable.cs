using System;
using System.Collections.Generic;
using System.Linq;

namespace Pronunciation
{
    public sealed class Syllable : IEquatable<Syllable>
    {
        public Syllable(IReadOnlyList<Symbol> symbols) => Symbols = symbols;

        public IReadOnlyList<Symbol> Symbols { get; }

        public IEnumerable<Symbol> Onset => Symbols.TakeWhile(x => x.GetSyllableType() != SyllableType.Vowel);
        public Symbol Nucleus => Symbols.First(x => x.GetSyllableType() == SyllableType.Vowel);
        public IEnumerable<Symbol> Coda => Symbols.SkipWhile(x => x.GetSyllableType() != SyllableType.Vowel).Skip(1);

        public bool RhymesWith(Syllable syllable)
        {
            if (Equals(syllable))
                return false;

            return Nucleus == syllable.Nucleus && Coda.SequenceEqual(syllable.Coda);
        }

        public Syllable GetRhymeSyllable => new(Coda.Prepend(Nucleus).ToList());


        /// <inheritdoc />
        public override string ToString() => string.Join(" ", Symbols.Select(x => x.ToString()));

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Symbols.Count, Symbols[0], Symbols[^1]);

        /// <inheritdoc />
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Syllable other && Equals(other);

        /// <inheritdoc />
        public bool Equals(Syllable? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Symbols.SequenceEqual(other.Symbols);
        }

        public static bool operator ==(Syllable? left, Syllable? right) => Equals(left, right);

        public static bool operator !=(Syllable? left, Syllable? right) => !Equals(left, right);
    }
}
using System;
using System.Collections.Generic;

namespace CMU
{
    public sealed class Word : IEquatable<Word>
    {

        public Word(string text, int variant, IReadOnlyList<Symbol> symbols)
        {
            Text = text;
            Variant = variant;
            Symbols = symbols;
        }

        public string Text { get; }

        /// <inheritdoc />
        public override string ToString() => Text;

        public int Variant { get; }

        public IReadOnlyList<Symbol> Symbols { get; }

        /// <inheritdoc />
        public bool Equals(Word other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Text == other.Text && Variant == other.Variant;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Word) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Text, Variant);

        public static bool operator ==(Word left, Word right) => Equals(left, right);

        public static bool operator !=(Word left, Word right) => !Equals(left, right);
    }
}

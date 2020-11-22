using System;
using System.Collections.Generic;
using System.Linq;

namespace Pronunciation
{
    public sealed class PhoneticsWord : IEquatable<PhoneticsWord>
    {

        public PhoneticsWord(string text, int variant, IReadOnlyList<Symbol> symbols)
        {
            Text = text;
            Variant = variant;
            Symbols = symbols;
            SyllableTypes= new Lazy<IReadOnlyList<SyllableType>>(()=> Symbols.Select(x=>x.GetSyllableType()).ToList());
        }

        public string Text { get; }

        /// <inheritdoc />
        public override string ToString() => Text;

        public int Variant { get; }

        public IReadOnlyList<Symbol> Symbols { get; }

        public Lazy<IReadOnlyList<SyllableType>> SyllableTypes { get; }

        /// <inheritdoc />
        public bool Equals(PhoneticsWord other)
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
            return Equals((PhoneticsWord) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Text, Variant);

        public static bool operator ==(PhoneticsWord left, PhoneticsWord right) => Equals(left, right);

        public static bool operator !=(PhoneticsWord left, PhoneticsWord right) => !Equals(left, right);
    }
}

using System;
using System.Collections.Generic;

namespace Pronunciation
{
    public sealed class ListComparer<T> : IEqualityComparer<IReadOnlyList<T>>
    {
        private ListComparer() {}

        public static IEqualityComparer<IReadOnlyList<T>> Instance { get; } = new ListComparer<T>();

        public bool Equals(IReadOnlyList<T>? x, IReadOnlyList<T>? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            if (x.Count == 0)
                return y.Count == 0;

            if (x.Count == 1)
                return y.Count == 1 && x[0]!.Equals(y[0]);

            return x.Count == y.Count && x[0]!.Equals(y[0]) && x[^1]!.Equals(y[^1]);
        }

        public int GetHashCode(IReadOnlyList<T> obj)
        {
            if (obj.Count == 0) return 0;

            return HashCode.Combine(obj.Count, obj[0], obj[^1]);
        }
    }

    public sealed class PhoneticsWord : IEquatable<PhoneticsWord>
    {

        public PhoneticsWord(string text, int variant, bool isCompound, IReadOnlyList<Syllable> syllables)
        {
            Text = text;
            Variant = variant;
            Syllables = syllables;
            IsCompound = isCompound;
        }

        public string Text { get; }

        /// <summary>
        /// Is this a compound word
        /// </summary>
        public bool IsCompound { get; }

        /// <inheritdoc />
        public override string ToString() => Text;

        public int Variant { get; }

        public IReadOnlyList<Syllable> Syllables { get; }

        /// <inheritdoc />
        public bool Equals(PhoneticsWord? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Text == other.Text && Variant == other.Variant;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
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

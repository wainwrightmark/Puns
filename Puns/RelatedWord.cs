using System;

namespace Puns
{
    public readonly struct RelatedWord : IEquatable<RelatedWord>
    {

        public RelatedWord(string word, string relatedTo, string reason, string meaning)
        {
            Word = word;
            RelatedTo = relatedTo;
            Reason = reason;
            Meaning = meaning;
        }

        public string Word { get; }

        public string RelatedTo { get; }

        public string Reason { get; }

        public string Meaning { get; }

        /// <inheritdoc />
        public override string ToString() => $"{Word} ({Reason}) ({Meaning})";

        /// <inheritdoc />
        public bool Equals(RelatedWord other) => Word == other.Word;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is RelatedWord other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => Word.GetHashCode();

        public static bool operator ==(RelatedWord left, RelatedWord right) => left.Equals(right);

        public static bool operator !=(RelatedWord left, RelatedWord right) => !left.Equals(right);
    }
}
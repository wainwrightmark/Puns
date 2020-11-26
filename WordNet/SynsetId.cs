using System;

namespace WordNet
{
    public readonly struct SynsetId : IEquatable<SynsetId>
    {
        public SynsetId(PartOfSpeech partOfSpeech, int id)
        {
            PartOfSpeech = partOfSpeech;
            Id = id;
        }

        public PartOfSpeech PartOfSpeech { get; }

        public int Id { get; }

        /// <inheritdoc />
        public bool Equals(SynsetId other) => PartOfSpeech == other.PartOfSpeech && Id == other.Id;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is SynsetId other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine((int) PartOfSpeech, Id);

        public static bool operator ==(SynsetId left, SynsetId right) => left.Equals(right);

        public static bool operator !=(SynsetId left, SynsetId right) => !left.Equals(right);

        /// <inheritdoc />
        public override string ToString() => $"{PartOfSpeech}:{Id}";
    }
}
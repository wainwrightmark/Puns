using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using Pronunciation;

namespace Puns
{
    public class SymbolCluster : IEquatable<SymbolCluster>
    {
        public SymbolCluster(IReadOnlyList<Symbol> symbols) => Symbols = symbols;

        public IReadOnlyList<Symbol> Symbols { get; }

        /// <inheritdoc />
        public override string ToString() => Symbols.ToDelimitedString(".");

        public bool Equals(SymbolCluster? other) => other is not null && Symbols.SequenceEqual(other.Symbols);

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((SymbolCluster) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Symbols.Count, Symbols.First(), Symbols.Last());

        public static bool operator ==(SymbolCluster? left, SymbolCluster? right) => Equals(left, right);

        public static bool operator !=(SymbolCluster? left, SymbolCluster? right) => !Equals(left, right);
    }
}

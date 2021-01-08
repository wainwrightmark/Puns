using System;
using System.Collections.Generic;

namespace Puns
{

public readonly struct Pun : IEquatable<Pun>
{
    public Pun(string newPhrase, string oldPhrase, IReadOnlySet<string> punWords)
    {
        NewPhrase = newPhrase;
        OldPhrase = oldPhrase;
        PunWords  = punWords;
    }

    public string NewPhrase { get; }
    public string OldPhrase { get; }

    public IReadOnlySet<string> PunWords { get; }

    /// <inheritdoc />
    public override string ToString() => NewPhrase;

    /// <inheritdoc />
    public bool Equals(Pun other) => string.Equals(
        NewPhrase,
        other.NewPhrase,
        StringComparison.OrdinalIgnoreCase
    );

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Pun other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(NewPhrase);

    public static bool operator ==(Pun left, Pun right) => left.Equals(right);

    public static bool operator !=(Pun left, Pun right) => !(left == right);
}

}

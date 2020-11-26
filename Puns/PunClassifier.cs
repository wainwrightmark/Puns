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

    public readonly struct PunReplacement
    {
        public PunReplacement(PunType punType, string replacementString)
        {
            PunType = punType;
            ReplacementString = replacementString.Replace('_', ' ');
        }

        public PunType PunType { get; }

        public string ReplacementString { get; }

        /// <inheritdoc />
        public override string ToString() => ReplacementString;
    }


    public abstract class PunStrategy
    {
        protected PunStrategy(IEnumerable<PhoneticsWord> themeWords)
        {
            ThemeWordLookup = themeWords.SelectMany(word =>
                    GetThemeWordSymbolClusters(word).Select(cluster=> (word, cluster)))
                .OrderBy(x=>x.word.Text.Length)
                .ToLookup(x => x.cluster, x => x.word);
        }

        public ILookup<SymbolCluster, PhoneticsWord> ThemeWordLookup { get; }

        public abstract IEnumerable<SymbolCluster> GetThemeWordSymbolClusters(PhoneticsWord word);

        public abstract IEnumerable<PunReplacement> GetPossibleReplacements(PhoneticsWord originalWord);
    }

    public class HomophonePunStrategy : PunStrategy
    {
        /// <inheritdoc />
        public HomophonePunStrategy(IEnumerable<PhoneticsWord> themeWords) : base(themeWords) {}

        /// <inheritdoc />
        public override IEnumerable<SymbolCluster> GetThemeWordSymbolClusters(PhoneticsWord word)
        {
            yield return new SymbolCluster(word.Symbols);
        }

        /// <inheritdoc />
        public override IEnumerable<PunReplacement> GetPossibleReplacements(PhoneticsWord originalWord)
        {
            var symbolCluster = new SymbolCluster(originalWord.Symbols);

            foreach (var phoneticsWord in ThemeWordLookup[symbolCluster])
            {
                var punType = originalWord.Text.Equals(phoneticsWord.Text, StringComparison.OrdinalIgnoreCase)? PunType.SameWord : PunType.Identity;

                yield return new PunReplacement(punType, phoneticsWord.Text);
            }
        }
    }

    /// <summary>
    /// The original word could be the beginning of the theme word
    /// </summary>
    public class PrefixPunStrategy : PunStrategy
    {
        /// <inheritdoc />
        public PrefixPunStrategy(IEnumerable<PhoneticsWord> themeWords) : base(themeWords) {}


        /// <inheritdoc />
        public override IEnumerable<SymbolCluster> GetThemeWordSymbolClusters(PhoneticsWord word)
        {
            for (var i = 2; i < word.Symbols.Count - 1; i++)
            {
                var cluster = new SymbolCluster(word.Symbols.Take(i).ToList());
                yield return cluster;
            }
        }

        /// <inheritdoc />
        public override IEnumerable<PunReplacement> GetPossibleReplacements(PhoneticsWord originalWord)
        {
            var cluster = new SymbolCluster(originalWord.Symbols);

            foreach (var themeWord in ThemeWordLookup[cluster])
            {
                if (!themeWord.Text.StartsWith(originalWord.Text))
                {
                    var replacementString = originalWord + themeWord.Text.Substring(originalWord.Text.Length); //TODO improve this

                    yield return new PunReplacement(PunType.Prefix, replacementString);
                }
            }
        }
    }

    /// <summary>
    /// The theme word rhymes with original word
    /// </summary>
    public class PerfectRhymePunStrategy : PunStrategy
    {
        public PerfectRhymePunStrategy(IEnumerable<PhoneticsWord> themeWords) : base(themeWords) { }

        public override IEnumerable<SymbolCluster> GetThemeWordSymbolClusters(PhoneticsWord word)
        {
            var lastStressedVowelIndex = word.Symbols.LastIndexOf(x => x.IsStressedVowel());

            if (lastStressedVowelIndex >= 0)
            {
                var vowel = new SymbolCluster(word.Symbols.Skip(lastStressedVowelIndex).ToList());
                yield return vowel;
            }
        }

        public override IEnumerable<PunReplacement> GetPossibleReplacements(PhoneticsWord originalWord)
        {
            var lastStressedVowel = GetThemeWordSymbolClusters(originalWord).FirstOrDefault();

            if(lastStressedVowel is null) yield break;


            foreach (var themeWord in ThemeWordLookup[lastStressedVowel])
            {


                var insert = false;

                if (themeWord.Text.Length < originalWord.Text.Length)
                {
                    var originalWordStressedVowels = originalWord.Symbols.Count(x => x.IsStressedVowel());
                    if (originalWordStressedVowels > 1)
                    {
                        var themeWordStressedVowels = themeWord.Symbols.Count(x => x.IsStressedVowel());
                        if (originalWordStressedVowels > themeWordStressedVowels)
                            insert = true;
                    }
                }

                string replacement = insert
                    ? originalWord.Text.Substring(0, originalWord.Text.Length - themeWord.Text.Length) + themeWord.Text//TODO improve this replacement
                    : themeWord.Text;

                yield return new PunReplacement(PunType.PerfectRhyme, replacement);
            }
        }


}



    public static class Extensions
    {
        public static int LastIndexOf<T>(this IReadOnlyList<T> source, Func<T, bool> predicate)
        {
            for (var i = source.Count - 1; i >= 0; i--)
            {
                if (predicate(source[i]))
                    return i;
            }

            return -1;
        }
    }

    public enum PunType
    {
        /// <summary>
        /// The exact same word - not really a pun
        /// </summary>
        SameWord,

        /// <summary>
        /// Bass / Base
        /// </summary>
        Identity,

        /// <summary>
        /// Multiple vowel sounds and all subsequent syllables match
        /// </summary>
        RichRhyme,

        /// <summary>
        /// Final vowel sound and all subsequent syllables match
        /// </summary>
        PerfectRhyme,

        /// <summary>
        /// Final vowel segments are different while the consonants are identical, or vice versa
        /// </summary>
        ImperfectRhyme, //Worse than prefix and infix

        /// <summary>
        /// One word is a prefix to the other
        /// </summary>
        Prefix,

        /// <summary>
        /// One word is contained within the other
        /// </summary>
        Infix,

        /// <summary>
        /// Both words share at least four syllables of prefix
        /// </summary>
        SharedPrefix


    }

}

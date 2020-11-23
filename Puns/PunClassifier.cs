using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using Pronunciation;

namespace Puns
{
    public static class PunClassifier
    {

        public static PunType? Classify(PhoneticsWord word1, PhoneticsWord word2)
        {
            var (shortWord, longWord) = word1.Symbols.Count <= word2.Symbols.Count
                ? (word1, word2)
                : (word2, word1);

            var r = Classify1(shortWord, longWord);

            return r;
        }




        private static PunType? Classify1(PhoneticsWord shortWord, PhoneticsWord longWord)
        {
            if (shortWord.Text.Equals(longWord.Text, StringComparison.OrdinalIgnoreCase))
                return PunType.SameWord;

            if (shortWord.Symbols.SequenceEqual(longWord.Symbols))
                return PunType.Identity;

            var rhymeType = GetRhymeType(shortWord, longWord);

            if (rhymeType == PunType.RichRhyme || rhymeType == PunType.PerfectRhyme)
                return rhymeType;


            if (longWord.Symbols.StartsWith(shortWord.Symbols))
                return PunType.Prefix;


            for (var skip = 1; skip < longWord.Symbols.Count - shortWord.Symbols.Count; skip++)
            {
                if (longWord.Symbols.Skip(skip).StartsWith(shortWord.Symbols))
                    return PunType.Infix;
            }

            if (shortWord.Symbols.Count >= 4 && longWord.Symbols.StartsWith(shortWord.Symbols.Take(4)))
                return PunType.SharedPrefix;

            return rhymeType; //Might be imperfect rhyme

            static PunType? GetRhymeType(PhoneticsWord shortWord, PhoneticsWord longWord)
            {

                var shortLastStressedVowel = shortWord.Symbols.LastIndexOf(x => x.IsStressedVowel());
                if (shortLastStressedVowel < 0)
                    return null;

                var longLastStressedVowel = longWord.Symbols.LastIndexOf(x => x.IsStressedVowel());
                if (longLastStressedVowel < 1)
                    return null;

                var shortSymbolsLeft = shortWord.Symbols.Count - shortLastStressedVowel;
                var longSymbolsLeft = longWord.Symbols.Count - longLastStressedVowel;

                if (shortSymbolsLeft != longSymbolsLeft)
                    return null;


                var imperfects = 0;
                for (var i = 0; i < shortSymbolsLeft; i++)
                {
                    var s = shortWord.Symbols[i + shortLastStressedVowel];
                    var l = longWord.Symbols[i + longLastStressedVowel];

                    if(s == l) continue;

                    if (imperfects > 0) return null; //give up

                    if (s.GetSyllableType() == l.GetSyllableType())
                        imperfects++;
                    else return null;
                }

                if (imperfects > 0) return PunType.ImperfectRhyme;

                else return PunType.PerfectRhyme;

                //TODO rich rhyme
            }
        }

        public static int? GetPunScore(this PunType punType)
        {
            return punType switch
            {
                PunType.SameWord => null,
                PunType.Identity => 8,
                PunType.RichRhyme => 7,
                PunType.PerfectRhyme => 6,
                PunType.ImperfectRhyme => null,
                PunType.Prefix => 5,
                PunType.Infix => 5,
                PunType.SharedPrefix => 5,
                _ => throw new ArgumentOutOfRangeException(nameof(punType), punType, null)
            };
        }

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

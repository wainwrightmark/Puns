using System;
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
                var matchingConsonants = 0;
                var matchingVowels = 0;

                for (var i = 0; i < shortWord.Symbols.Count; i++)
                {
                    var sSyllable = shortWord.Symbols[^(1+i)];
                    var lSyllable = longWord.Symbols[^(1+i)];

                    if (sSyllable == lSyllable)
                    {
                        if (sSyllable.GetSyllableType().IsVowel())
                            matchingVowels++;
                        else matchingConsonants++;

                        if (matchingConsonants >= 2 && matchingVowels >= 2)
                            return PunType.RichRhyme;
                    }
                    else if (matchingConsonants >= 1 && matchingVowels >= 1 && matchingConsonants + matchingVowels >= 3)
                        return PunType.PerfectRhyme;

                    else if (sSyllable.GetSyllableType() == lSyllable.GetSyllableType())
                    {

                        if (sSyllable.GetSyllableType().IsVowel())
                            matchingVowels++;
                        else matchingConsonants++;

                        if (matchingConsonants >= 1 && matchingVowels >= 1 && matchingConsonants + matchingVowels >= 3)
                            return PunType.ImperfectRhyme;

                        return null;
                    }
                    else
                        return null;
                }

                return null;

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

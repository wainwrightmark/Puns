using System;
using System.Collections.Generic;
using System.Linq;

namespace Pronunciation
{
    public static class SymbolHelper
    {
        public static SyllableType GetSyllableType(this Symbol symbol) => SyllableTypeDictionary.Value[symbol];

        private static readonly IReadOnlyDictionary<string, SyllableType> TextToSyllableTypeDictionary = new Dictionary<string, SyllableType>()
        {
            {"AA", SyllableType.Vowel},
            {"AE", SyllableType.Vowel},
            {"AH", SyllableType.Vowel},
            {"AO", SyllableType.Vowel},
            {"AW", SyllableType.Vowel},
            {"AY", SyllableType.Vowel},
            {"B", SyllableType.Stop},
            {"CH", SyllableType.Affricate},
            {"D", SyllableType.Stop},
            {"DH", SyllableType.Fricative},
            {"EH", SyllableType.Vowel},
            {"ER", SyllableType.Vowel},
            {"EY", SyllableType.Vowel},
            {"F", SyllableType.Fricative},
            {"G", SyllableType.Stop},
            {"HH", SyllableType.Aspirate},
            {"IH", SyllableType.Vowel},
            {"IY", SyllableType.Vowel},
            {"JH", SyllableType.Affricate},
            {"K", SyllableType.Stop},
            {"L", SyllableType.Liquid},
            {"M", SyllableType.Nasal},
            {"N", SyllableType.Nasal},
            {"NG", SyllableType.Nasal},
            {"OW", SyllableType.Vowel},
            {"OY", SyllableType.Vowel},
            {"P", SyllableType.Stop},
            {"R", SyllableType.Liquid},
            {"S", SyllableType.Fricative},
            {"SH", SyllableType.Fricative},
            {"T", SyllableType.Stop},
            {"TH", SyllableType.Fricative},
            {"UH", SyllableType.Vowel},
            {"UW", SyllableType.Vowel},
            {"V", SyllableType.Fricative},
            {"W", SyllableType.Semivowel},
            {"Y", SyllableType.Semivowel},
            {"Z", SyllableType.Fricative},
            {"ZH", SyllableType.Fricative}

        };

        private static SyllableType GetSyllableType1(Symbol s)
        {
            if (s.ToString().Length > 1 && TextToSyllableTypeDictionary.TryGetValue(s.ToString().Substring(0, 2), out var st))
                return st;

            return TextToSyllableTypeDictionary[s.ToString().Substring(0,1)];
        }

        public static readonly Lazy<IReadOnlyDictionary<Symbol, SyllableType>>

             SyllableTypeDictionary = new Lazy<IReadOnlyDictionary<Symbol, SyllableType>>(() =>
             {
                 return Enum.GetValues<Symbol>().ToDictionary(x => x, GetSyllableType1);
             });
    }
}
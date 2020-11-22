﻿using System;
using System.Collections.Generic;
using System.Linq;
using CMU;
using MoreLinq;
using WordNet;

namespace Puns
{
    public static class PunHelper
    {
        public static bool IsPun(Word originalWord, Word replacementWord)
        {
            if (IsSameWord(originalWord.Text, replacementWord.Text))
                return false;

            if (originalWord.Symbols.Count < 2 || replacementWord.Symbols.Count < 2)
                return false;

            if (originalWord.Symbols.Count == replacementWord.Symbols.Count) //same number of syllables
            {
                if (originalWord.Symbols[0] != replacementWord.Symbols[0] && originalWord.Symbols[^1] != replacementWord.Symbols[^1])
                    return false;

                return originalWord.Symbols.Select(x => x.GetSyllableType())
                    .SequenceEqual(replacementWord.Symbols.Select(x => x.GetSyllableType()));
            }
            else
            {
                if (replacementWord.Symbols.StartsWith(originalWord.Symbols))
                    return true;

                if (originalWord.Symbols.StartsWith(replacementWord.Symbols))
                    return true;

                return false;
            }
        }

        public static IReadOnlyCollection<Pun> GetPuns(PunCategory category,
            string theme,
            SynSet synSet,
            WordNetEngine wordNetEngine,
            ILookup<string, Word> cmuLookup)
        {
            var phrases = GetPhrases(category);

            var themeWords = GetRelatedWords(theme, synSet, wordNetEngine)
                .Select(x=>x.Word).Prepend(theme)

                .Distinct(StringComparer.OrdinalIgnoreCase)
                .SelectMany(x=> cmuLookup[x])
                .Distinct(WordPronunciationComparer.Instance)
                .ToList();

            var puns = new List<Pun>();

            foreach (var phrase in phrases)
            {
                var words = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if(words.Length == 1)
                    continue;

                foreach (var word in words)
                {
                    var casing = DetectCasing(word);

                    //if (!wordNetEngine.WordLookup.Contains(word)) continue;

                    var cmuWord = cmuLookup[word].FirstOrDefault();

                    if (cmuWord is null) continue;

                    var punWords = themeWords.Where(x => IsPun(x, cmuWord));

                    foreach (var punWord in punWords)
                    {
                        var newString = ToCase(punWord.Text, casing);

                        var newPhrase = phrase.Replace(word, newString);
                        puns.Add(new Pun(newPhrase, phrase, punWord.Text, synSet));
                    }
                }
            }

            return puns;
        }

        public static IReadOnlyCollection<string>  GetPhrases(PunCategory category)
        {
            return category switch
            {

                PunCategory.Movies => CategoryLists.Movies.Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                PunCategory.Idiom => CategoryLists.Idioms.Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                PunCategory.Bands => CategoryLists.Bands.Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                PunCategory.Books => CategoryLists.Books.Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
            };
        }


        public static Casing DetectCasing(string s)
        {
            if (s.All(char.IsLower))
                return Casing.Lower;
            if (s.All(char.IsUpper))
                return Casing.Upper;

            return Casing.Title; //Not 100% perfect
        }

        public static string ToCase(string s, Casing casing)
        {
            return casing switch
            {
                Casing.Lower => s.ToLower(),
                Casing.Upper => s.ToUpper(),
                Casing.Title => System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(s.ToLower()),
                _ => throw new ArgumentOutOfRangeException(nameof(casing), casing, null)
            };
        }

        public static bool IsSameWord(string s1, string s2)
        {
            //TODO improve
            return s1.Equals(s2, StringComparison.OrdinalIgnoreCase) ||
                   (s1 + "s").Equals(s2, StringComparison.OrdinalIgnoreCase) ||
                   (s2 + "s").Equals(s1, StringComparison.OrdinalIgnoreCase);
        }

        private static readonly List<SynSetRelation> Relations = new List<SynSetRelation>()
        {
            SynSetRelation.Hyponym,
            SynSetRelation.TopicDomainMember
        };

        public static IEnumerable<RelatedWord> GetRelatedWords(string relatedToWord, SynSet synSet, WordNetEngine wordNetEngine)
        {
            var synSets = synSet.GetRelatedSynSets(Relations, true, wordNetEngine).Prepend(synSet);

            foreach (var set in synSets)
                foreach (var word in set.Words)
                    yield return new RelatedWord(word, relatedToWord, "...", set.Gloss);
        }

    }

    public enum Casing
    {
        Lower,
        Upper,
        Title
    }

    public enum PunCategory
    {
        Idiom,
        Movies,
        Bands,
        Books
    }
}

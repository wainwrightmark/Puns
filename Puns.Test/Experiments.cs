﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using WordNet;
using Xunit;
using Xunit.Abstractions;

namespace Puns.Test
{
    public class Experiments
    {
        public Experiments(ITestOutputHelper testOutputHelper) => TestOutputHelper = testOutputHelper;

        public ITestOutputHelper TestOutputHelper { get; }

        /*
         * bla
         * ckc
         * ock
         */

        [Theory]
        [InlineData(9,1)]
        [InlineData(16,1)]
        [InlineData(9, 2)]
        [InlineData(16, 2)]
        public void FindSquares(int totalLength, int maxWords)
        {
            var wordNetEngine = new WordNetEngine();

            var synSets = wordNetEngine.GetAllSynSets().ToList();


            TestOutputHelper.WriteLine($"Hello {synSets.Count} synSets");

            var allWords = synSets.SelectMany(x => x.Words)
                .Select(x => x.Replace("_", null).Replace("-", null))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var wordsByLength = allWords.ToLookup(x => x.Length);

            var wordsToCheck = wordsByLength[totalLength].ToHashSet(StringComparer.OrdinalIgnoreCase);

            for (var word1Length = 3; word1Length <= totalLength -3; word1Length++)
            {
                var word2Length = totalLength - word1Length;

                foreach (var w1 in wordsByLength[word1Length])
                {
                    foreach (var w2 in wordsByLength[word2Length])
                    {
                        wordsToCheck.Add(w1.ToUpper() + w2.ToLower());
                        wordsToCheck.Add(w2.ToUpper() + w1.ToLower());
                    }
                }
            }




            foreach (var word in wordsToCheck)
            {
                var convertedForms = GetWordConversions(word);

                foreach (var (form, newWord) in convertedForms)
                {

                    if (ContainsCompleteWords(newWord, maxWords, out var list))
                    {
                        var c = string.Join(" ", list.Reverse());

                        TestOutputHelper.WriteLine($"{word} * {form} = {c}");
                    }
                }



            }

            bool ContainsCompleteWords(string s, int maxNumber, out ImmutableList<string>? list)
            {
                if (maxNumber > 0)
                {
                    for (var substringLength = 3; substringLength <= s.Length; substringLength++)
                    {
                        var substring = s.Substring(0, substringLength);

                        if (allWords.Contains(substring))
                            if (substringLength == s.Length)
                            {
                                list = ImmutableList<string>.Empty.Add(substring);
                                return true;
                            }
                            else if (ContainsCompleteWords(s.Substring(substringLength), maxNumber - 1, out var l))
                            {
                                list = l.Add(substring);
                                return true;
                            }
                    }
                }



                list = null;

                return false;
            }


            static IEnumerable<(string form, string newWord)> GetWordConversions(string s)
            {
                foreach (var (form, pattern) in WordForms[s.Length])
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    foreach (var i in pattern)
                    {
                        stringBuilder.Append(s[i]);
                    }

                    yield return (form, stringBuilder.ToString());
                }
            }

        }

        private static IReadOnlyDictionary<int, IReadOnlyCollection<(string name,  IReadOnlyList<int> pattern) >>



             WordForms = new Dictionary<int, IReadOnlyCollection<(string name, IReadOnlyList<int> pattern)>>()
             {
                 {9,new (string name, IReadOnlyList<int> pattern)[]
        /*
         * 012
         * 345
         * 678
         */
        {
            ("Rotated", new List<int>() {0,3,6,1,4,7,2,5,8}),
            ("AntiClockwise Spiral", new List<int>() {0,3,6,7,8,5,2,1,4}),
            ("Clockwise Spiral", new List<int>() {0,1,2,5,8,7,6,3,4}),
            ("Down up down", new List<int>() {0,3,6,7,4,1,2,5,8}),
        } },
                 {16,new (string name, IReadOnlyList<int> pattern)[]
        /*
         * 00 01 02 03
         * 04 05 06 07
         * 08 09 10 11
         * 12 13 14 15
         */
        {
            ("Rotated", new List<int>() {0,4,8,12,1,5,9,13,2,6,10,14,3,7,11,15}),
            ("AntiClockwise Spiral", new List<int>() {0,4,8,12,13,14,15,11,7,3,2,1,5,9,10,6}),
            ("Clockwise Spiral", new List<int>() {0,1,2,3,7,11,15,14,13,12,8,4,5,6,10,9}),
            ("Down up down", new List<int>() {0,4,8,12,13,9,5,1,2,6,10,14,15,11,7,3}),
        } },};

    }
}

﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Pronunciation;
using WordNet;
using Xunit;
using Xunit.Abstractions;

namespace Puns.Test
{

public class SyllableSpellingHelper
{
    public SyllableSpellingHelper(ITestOutputHelper testOutputHelper) =>
        TestOutputHelper = testOutputHelper;

    public ITestOutputHelper TestOutputHelper { get; }

    [Fact]
    public void FindSyllableSpellings()
    {
        var engine = new PronunciationEngine();
        var words  = engine.GetAllPhoneticsWords().ToList();

        var remainingWords =
            new Stack<PhoneticsWord>(words.OrderByDescending(x => x.Syllables.Count));

        var dict = new ConcurrentDictionary<Syllable, SyllableSpelling>();

        while (remainingWords.Any())
        {
            var anyChanged     = false;
            var wordsToPutBack = new List<PhoneticsWord>();

            while (remainingWords.TryPop(out var word))
            {
                if (!TryEat(word.Syllables.ToArray(), word.Text, dict))
                    wordsToPutBack.Add(word);
                else
                    anyChanged = true;
            }

            remainingWords = new Stack<PhoneticsWord>(wordsToPutBack);

            if (!anyChanged)
                break;
        }

        TestOutputHelper.WriteLine($"Found {dict.Count} syllables");

        if (remainingWords.Any())
            TestOutputHelper.WriteLine($"Could not find {remainingWords.Count} words");

        foreach (var (syllable, spelling) in dict.OrderBy(x => x.Key.ToString()))
        {
            var sl = $"{syllable}\t{spelling.GetBestDetail()}";
            TestOutputHelper.WriteLine(sl);
        }

        //foreach (var remainingWord in remainingWords)
        //{
        //    TestOutputHelper.WriteLine(remainingWord.Text + " Not Found");
        //}

        static bool TryEat(
            ReadOnlySpan<Syllable> syllables,
            string text,
            ConcurrentDictionary<Syllable, SyllableSpelling> dictionary)
        {
            switch (syllables.Length)
            {
                case 0: return true;
                case 1:
                    dictionary.AddOrUpdate(
                        syllables[0],
                        s =>
                        {
                            var ss = new SyllableSpelling();
                            ss.AddSpelling(text);
                            return ss;
                        },
                        (_, ss) =>
                        {
                            ss.AddSpelling(text);
                            return ss;
                        }
                    );

                    return true;
                default:
                {
                    if (dictionary.TryGetValue(syllables[0], out var spelling))
                    {
                        if (spelling.GetPrefix(text, out var prefix))
                        {
                            var suffix = text.Substring(prefix.Length);

                            if (TryEat(syllables[1..], suffix, dictionary))
                            {
                                spelling.AddSpelling(prefix);
                                return true;
                            }
                        }
                    }

                    if (dictionary.TryGetValue(syllables[^1], out var spelling2))
                    {
                        if (spelling2.GetSuffix(text, out var suffix))
                        {
                            var prefix = text.Substring(0, text.Length - suffix.Length);

                            if (TryEat(syllables[..^1], prefix, dictionary))
                            {
                                spelling2.AddSpelling(suffix);
                                return true;
                            }
                        }
                    }

                    break;
                }
            }

            return false;
        }
    }

    class SyllableSpelling
    {
        private readonly ConcurrentDictionary<string, int> _spellings =
            new(StringComparer.OrdinalIgnoreCase);

        public void AddSpelling(string s)
        {
            _spellings.AddOrUpdate(s, 1, (_, i) => i + 1);
        }

        public int Count => _spellings.Count;

        public bool GetPrefix(string text, out string prefix)
        {
            var maxLength = Math.Min(5, text.Length - 1);

            for (var length = maxLength; length > 0; length--)
            {
                var p = text[..length];

                if (_spellings.ContainsKey(p))
                {
                    prefix = p;
                    return true;
                }
            }

            prefix = null;
            return false;
        }

        public bool GetSuffix(string text, out string suffix)
        {
            var maxLength = Math.Min(5, text.Length - 1);

            for (var length = maxLength; length > 0; length--)
            {
                var s = text.Substring(text.Length - length);

                if (_spellings.ContainsKey(s))
                {
                    suffix = s;
                    return true;
                }
            }

            suffix = null;
            return false;
        }

        public string GetBestDetail()
        {
            var count = 0;
            var best  = (count: 0, text: "");

            foreach (var (text, c) in _spellings)
            {
                count += c;

                if (best.count < c)
                    best = (c, text);
            }

            return $"{best.text}"; // ({best.count}/{count})";
        }
    }

    [Fact]
    public void FindFirstSyllableSpellings()
    {
        var engine = new PronunciationEngine();

        var words = engine.GetAllPhoneticsWords().ToList();

        var syllables = words.SelectMany(x => x.Syllables).Distinct().Count();

        var wordsBySyllable = words
            .SelectMany(
                word =>
                {
                    var first = (word, syllable: word.Syllables[0], start: true);
                    var last  = (word, syllable: word.Syllables[^1], start: false);

                    return new[] { first, last };
                }
            )
            .GroupBy(x => x.syllable)
            .OrderByDescending(x => x.Count())
            .ToList();

        TestOutputHelper.WriteLine($"{wordsBySyllable.Count}/{syllables}  syllables");

        var results =
            new List<(string Syllable, string spelling, int totalInstances, int spellingInstances)
            >();

        foreach (var grouping in wordsBySyllable)
        {
            var syllable = grouping.Key;

            var prefixes = grouping.SelectMany(
                    x =>
                    {
                        var (word, _, start) = x;

                        if (word.Syllables.Count == 1)
                            return new[] { word.Text };

                        if (start)
                        {
                            return
                                Enumerable.Range(1, 5)
                                    .Where(l => word.Text.Length > l)
                                    .Select(l => word.Text.Substring(0, l));
                        }
                        else
                        {
                            return
                                Enumerable.Range(1, 5)
                                    .Where(l => word.Text.Length > l)
                                    .Select(l => word.Text[^l..]);
                        }
                    }
                )
                .GroupBy(x => x);

            var mostCommonPrefix = prefixes.OrderByDescending(GetScore).First();

            results.Add(
                (syllable.ToString(), mostCommonPrefix.Key, grouping.Count(),
                 mostCommonPrefix.Count())
            );

            static double GetScore(IGrouping<string, string> grouping)
            {
                return grouping.Count() * Math.Log2(grouping.Key.Length); //TODO improve
            }
        }

        foreach (var (syllable, spelling, totalInstances, spellingInstances) in results.OrderBy(
            x => x.Syllable
        ))
        {
            TestOutputHelper.WriteLine(
                $"{syllable}: {spelling} ({spellingInstances}/{totalInstances})"
            );
        }
    }
}

public class DoubleStraights
{
    public DoubleStraights(ITestOutputHelper testOutputHelper) =>
        TestOutputHelper = testOutputHelper;

    public ITestOutputHelper TestOutputHelper { get; }

    [Fact]
    public void FindDoubleStraights()
    {
        var wordNetEngine = new WordNetEngine();

        var synSets = wordNetEngine.GetAllSynSets().ToList();

        var doubleWords = synSets
            .SelectMany(x => x.Words)
            .Where(s => s.Count(x => x == '_') == 1)
            .Select(
                doubleWord =>
                {
                    var split = doubleWord.Split('_');

                    return (word1: split[0], word2: split[1], doubleWord);
                }
            )
            .Where(x => !x.word1.Equals(x.word2, StringComparison.OrdinalIgnoreCase))
            .ToList();

        TestOutputHelper.WriteLine($"{doubleWords.Count} double words");

        var results = new HashSet<string>();

        foreach (var (word1, word2, doubleWord) in doubleWords)
        {
            var word1Sets = wordNetEngine.GetSynSets(word1);

            var word1Words = word1Sets.SelectMany(x => x.Words)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (word1Words.Contains(word2))
                continue;

            var word2Sets  = wordNetEngine.GetSynSets(word2);
            var word2Words = word2Sets.SelectMany(x => x.Words).ToList();

            var commonWords = word1Words.Intersect(word2Words)
                .Except(new[] { word1, word2, doubleWord })
                .ToHashSet();

            if (commonWords.Any())
            {
                var doubleWordSynonyms =
                    wordNetEngine.GetSynSets(doubleWord).SelectMany(x => x.Words);

                commonWords = commonWords.Except(doubleWordSynonyms).ToHashSet();
            }

            foreach (var commonWord in commonWords)
            {
                results.Add($"{word1}, {word2}: {commonWord}");
            }
        }

        TestOutputHelper.WriteLine($"{results.Count} found");

        foreach (var result in results)
        {
            TestOutputHelper.WriteLine(result);
        }
    }
}

public class WordSquares
{
    public WordSquares(ITestOutputHelper testOutputHelper) => TestOutputHelper = testOutputHelper;

    public ITestOutputHelper TestOutputHelper { get; }

    /*
     * bla
     * ckc
     * ock
     */

    [Theory(Skip = "true")]
    [InlineData(9,  1)]
    [InlineData(16, 1)]
    [InlineData(9,  2)]
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

        for (var word1Length = 3; word1Length <= totalLength - 3; word1Length++)
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
                        else if (ContainsCompleteWords(
                            s.Substring(substringLength),
                            maxNumber - 1,
                            out var l
                        ))
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

    private static readonly IReadOnlyDictionary<int,
            IReadOnlyCollection<(string name, IReadOnlyList<int> pattern)>>
        WordForms =
            new Dictionary<int, IReadOnlyCollection<(string name, IReadOnlyList<int> pattern)>>()
            {
                {
                    9, new (string name, IReadOnlyList<int> pattern)[]
                        /*
                         * 012
                         * 345
                         * 678
                         */
                        {
                            ("Rotated", new List<int>()
                            {
                                0,
                                3,
                                6,
                                1,
                                4,
                                7,
                                2,
                                5,
                                8
                            }),
                            ("AntiClockwise Spiral", new List<int>()
                            {
                                0,
                                3,
                                6,
                                7,
                                8,
                                5,
                                2,
                                1,
                                4
                            }),
                            ("Clockwise Spiral", new List<int>()
                            {
                                0,
                                1,
                                2,
                                5,
                                8,
                                7,
                                6,
                                3,
                                4
                            }),
                            ("Down up down", new List<int>()
                            {
                                0,
                                3,
                                6,
                                7,
                                4,
                                1,
                                2,
                                5,
                                8
                            }),
                        }
                },
                {
                    16, new (string name, IReadOnlyList<int> pattern)[]
                        /*
                         * 00 01 02 03
                         * 04 05 06 07
                         * 08 09 10 11
                         * 12 13 14 15
                         */
                        {
                            ("Rotated", new List<int>()
                            {
                                0,
                                4,
                                8,
                                12,
                                1,
                                5,
                                9,
                                13,
                                2,
                                6,
                                10,
                                14,
                                3,
                                7,
                                11,
                                15
                            }),
                            ("AntiClockwise Spiral", new List<int>()
                            {
                                0,
                                4,
                                8,
                                12,
                                13,
                                14,
                                15,
                                11,
                                7,
                                3,
                                2,
                                1,
                                5,
                                9,
                                10,
                                6
                            }),
                            ("Clockwise Spiral", new List<int>()
                            {
                                0,
                                1,
                                2,
                                3,
                                7,
                                11,
                                15,
                                14,
                                13,
                                12,
                                8,
                                4,
                                5,
                                6,
                                10,
                                9
                            }),
                            ("Down up down", new List<int>()
                            {
                                0,
                                4,
                                8,
                                12,
                                13,
                                9,
                                5,
                                1,
                                2,
                                6,
                                10,
                                14,
                                15,
                                11,
                                7,
                                3
                            }),
                        }
                },
            };
}

}

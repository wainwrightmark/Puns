using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Pronunciation;
using WordNet;
using Xunit;
using Xunit.Abstractions;

namespace Puns.Test
{

public class WordFixture
{
    public WordFixture()
    {
        SpellingEngine      = new SpellingEngine();
        WordNetEngine       = new WordNetEngine();
        PronunciationEngine = new PronunciationEngine();
    }

    public PronunciationEngine PronunciationEngine { get; }
    public WordNetEngine WordNetEngine { get; }

    public SpellingEngine SpellingEngine { get; }
}

public class PunTests : IClassFixture<WordFixture>
{
    public PunTests(ITestOutputHelper testOutputHelper, WordFixture wordFixture)
    {
        TestOutputHelper = testOutputHelper;
        WordFixture      = wordFixture;
    }

    public ITestOutputHelper TestOutputHelper { get; }
    public WordFixture WordFixture { get; }

    public PronunciationEngine PronunciationEngine => WordFixture.PronunciationEngine;

    public WordNetEngine WordNetEngine => WordFixture.WordNetEngine;

    public SpellingEngine SpellingEngine => WordFixture.SpellingEngine;

    [Fact]
    public void TestSynSets()
    {
        var synSets = WordNetEngine.GetSynSets("Fish").ToList();

        synSets.Should().HaveCountGreaterThan(2);

        foreach (var synSet in synSets)
            TestOutputHelper.WriteLine(synSet.Gloss);
    }

    [Theory]
    [InlineData("fish")]
    [InlineData("food")]
    [InlineData("vegetable")]
    [InlineData("night")]
    [InlineData("chocolate")]
    public void TestRelatedWords(string word)
    {
        var synSets = WordNetEngine.GetSynSets(word).ToList();

        foreach (var synSet in synSets)
        foreach (var relatedSynSet in PunHelper.GetPunSynSets(synSet, WordNetEngine, true))
        foreach (var word1 in relatedSynSet.Words)
            TestOutputHelper.WriteLine((word1, synSet.Gloss, relatedSynSet.Gloss).ToString());
    }

    [Fact]
    public void TestPronunciation()
    {
        PronunciationEngine.GetPhoneticsWord("fish").Should().NotBeNull();
        PronunciationEngine.GetPhoneticsWord("fish")!.Syllables.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("colt",         "bolt",         PunType.PerfectRhyme,   "colt")]
    [InlineData("far",          "carnage",      PunType.PrefixRhyme,    "farnage")]
    [InlineData("butterscotch", "butterfield",  PunType.SharedPrefix,   "butterscotch")]
    [InlineData("bear",         "bare",         PunType.Identity,       "bear")]
    [InlineData("beard",        "weird",        PunType.PerfectRhyme,   "beard")]
    [InlineData("bovine",       "valentine",    PunType.PerfectRhyme,   "vallenbovine")]
    [InlineData("pisces",       "pieces",       PunType.SameConsonants, "pisces")]
    [InlineData("pieces",       "pisces",       PunType.SameConsonants, "pieces")]
    [InlineData("ray",          "relationship", PunType.Infix,          "reraytionship")]
    [InlineData("ray",          "away",         PunType.PerfectRhyme,   "aray")]
    public void TestPunClassification(
        string themeWord,
        string wordToReplace,
        PunType? expectedPunType,
        string expectedReplacementString)

    {
        var theme        = PronunciationEngine.GetPhoneticsWord(themeWord)!;
        var originalWord = PronunciationEngine.GetPhoneticsWord(wordToReplace)!;

        var themeWords = new List<PhoneticsWord>() { theme };

        var punStrategies = PunHelper.GetPunStrategies(SpellingEngine, themeWords);

        var bestReplacement = punStrategies
            .SelectMany(x => x.GetPossibleReplacements(originalWord))
            .FirstOrDefault()!;

        bestReplacement.ReplacementString.Should().NotBeNull();

        bestReplacement.PunType.Should().Be(expectedPunType);

        bestReplacement.ReplacementString.ToLowerInvariant()
            .Should()
            .Be(expectedReplacementString.ToLowerInvariant());
    }

    [Theory]
    [InlineData("vegetable", PunCategory.Idiom)]
    [InlineData("vegetable", PunCategory.Bands)]
    [InlineData("vegetable", PunCategory.Movies)]
    [InlineData("vegetable", PunCategory.Books)]
    [InlineData("fish",      PunCategory.Idiom)]
    [InlineData("fish",      PunCategory.Bands)]
    [InlineData("fish",      PunCategory.Movies)]
    [InlineData("fish",      PunCategory.Books)]
    [InlineData("fish",      PunCategory.ChristmasSongs)]
    [InlineData("chocolate", PunCategory.Idiom)]
    [InlineData("chocolate", PunCategory.Bands)]
    [InlineData("chocolate", PunCategory.Movies)]
    //[InlineData("chocolate", PunCategory.Books)]
    [InlineData("candy",  PunCategory.Idiom)]
    [InlineData("candy",  PunCategory.Bands)]
    [InlineData("candy",  PunCategory.Movies)]
    [InlineData("candy",  PunCategory.Books)]
    [InlineData("animal", PunCategory.Idiom)]
    [InlineData("animal", PunCategory.Bands)]
    [InlineData("animal", PunCategory.Movies)]
    [InlineData("animal", PunCategory.Books)]
    [InlineData("house",  PunCategory.Idiom)]
    [InlineData("house",  PunCategory.Bands)]
    [InlineData("house",  PunCategory.Movies)]
    [InlineData("house",  PunCategory.Books)]
    [InlineData("Green",  PunCategory.Idiom)]
    public async Task TestPunHelper(string theme, PunCategory category)
    {
        var synSets = WordNetEngine.GetSynSets(theme).ToList();

        var puns = await PunHelper.GetPuns(
            category,
            theme,
            synSets,
            WordNetEngine,
            PronunciationEngine,
            SpellingEngine,
            new Progress<(double amount, bool typesLoaded)>((_)=>{}),
            CancellationToken.None
        ).ToListAsync();

        puns.Should().HaveCountGreaterThan(2);

        TestOutputHelper.WriteLine(puns.Count + " puns");

        foreach (var (newPhrase, oldPhrase) in puns.Select(x => (x.NewPhrase, x.OldPhrase))
            .Distinct())
        {
            TestOutputHelper.WriteLine($"{newPhrase} ({oldPhrase})");
        }
    }
}

}

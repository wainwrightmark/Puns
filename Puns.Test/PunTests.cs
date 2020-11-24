using System.Linq;
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
            WordNetEngine = new WordNetEngine();
            PronunciationEngine = new PronunciationEngine();
        }

        public PronunciationEngine PronunciationEngine { get; }
        public WordNetEngine WordNetEngine { get; }
    }


    public class PunTests : IClassFixture<WordFixture>
    {
        public PunTests(ITestOutputHelper testOutputHelper, WordFixture wordFixture)
        {
            TestOutputHelper = testOutputHelper;
            WordFixture = wordFixture;
        }

        public ITestOutputHelper TestOutputHelper { get; }
        public WordFixture WordFixture { get; }

        public PronunciationEngine PronunciationEngine => WordFixture.PronunciationEngine;

        public WordNetEngine WordNetEngine => WordFixture.WordNetEngine;

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
            foreach (var relatedSynSet in PunHelper.GetPunSynSets(synSet, WordNetEngine))
            foreach (var word1 in relatedSynSet.Words)
                TestOutputHelper.WriteLine((word1, synSet.Gloss, relatedSynSet.Gloss).ToString());

        }



        [Fact]
        public void TestPronunciation()
        {
            PronunciationEngine.GetPhoneticsWords("fish").Should().NotBeEmpty();
            PronunciationEngine.GetPhoneticsWords("fish").First().Symbols.Should().NotBeEmpty();
        }

        [Theory]
        [InlineData("colt", "bolt", PunType.PerfectRhyme)]
        [InlineData("smelt", "bolt", PunType.ImperfectRhyme)]
        [InlineData("car", "carnage", PunType.Prefix)]
        [InlineData("car", "incarcerate", PunType.Infix)]
        [InlineData("butterfield", "butterscotch", PunType.SharedPrefix)]
        [InlineData("bear", "bare", PunType.Identity)]
        public void TestPunClassification(string themeWord,  string wordToReplace, PunType? expectedPunType)

        {
            var theme = PronunciationEngine.GetPhoneticsWords(themeWord).First();
            var replacementWord = PronunciationEngine.GetPhoneticsWords(wordToReplace).First();


            var realPunType = PunClassifier.Classify(theme, replacementWord);

            realPunType.Should().Be(expectedPunType);

        }

        [Theory]
        [InlineData("vegetable", PunCategory.Idiom)]
        [InlineData("vegetable", PunCategory.Bands)]
        [InlineData("vegetable", PunCategory.Movies)]
        [InlineData("vegetable", PunCategory.Books)]
        [InlineData("fish", PunCategory.Idiom)]
        [InlineData("fish", PunCategory.Bands)]
        [InlineData("fish", PunCategory.Movies)]
        [InlineData("fish", PunCategory.Books)]
        [InlineData("chocolate", PunCategory.Idiom)]
        [InlineData("chocolate", PunCategory.Bands)]
        [InlineData("chocolate", PunCategory.Movies)]
        //[InlineData("chocolate", PunCategory.Books)]
        [InlineData("candy", PunCategory.Idiom)]
        [InlineData("candy", PunCategory.Bands)]
        [InlineData("candy", PunCategory.Movies)]
        [InlineData("candy", PunCategory.Books)]
        [InlineData("animal", PunCategory.Idiom)]
        [InlineData("animal", PunCategory.Bands)]
        [InlineData("animal", PunCategory.Movies)]
        [InlineData("animal", PunCategory.Books)]
        public void TestPunHelper(string theme, PunCategory category)
        {

            var synSets = WordNetEngine.GetSynSets(theme).ToList();

            var puns =  PunHelper.GetPuns(category, theme, synSets, WordNetEngine, PronunciationEngine);

            puns.Should().HaveCountGreaterThan(2);

            TestOutputHelper.WriteLine(puns.Count + " puns");

            foreach (var (newPhrase, oldPhrase) in puns.Select(x=>(x.NewPhrase, x.OldPhrase)).Distinct())
            {
                TestOutputHelper.WriteLine($"{newPhrase} ({oldPhrase})");
            }
        }

    }
}

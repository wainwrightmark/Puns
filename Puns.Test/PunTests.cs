using System.Linq;
using CMU;
using FluentAssertions;
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

            Lookup = WordHelper.TryCreateLookup().Value;
        }

        public ILookup<string, Word> Lookup { get; }

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

        public ILookup<string, Word> Lookup => WordFixture.Lookup;

        public WordNetEngine WordNetEngine => WordFixture.WordNetEngine;

        [Fact]
        public void TestSynSets()
        {
            var synSets = WordNetEngine.GetSynSets("Fish").ToList();

            synSets.Should().HaveCountGreaterThan(2);

            foreach (var synSet in synSets)
                TestOutputHelper.WriteLine(synSet.Gloss);

        }

        [Fact]
        public void TestPronunciation()
        {

            Lookup["fish"].Should().NotBeEmpty();

            Lookup["fish"].First().Symbols.Should().NotBeEmpty();
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

            var puns = synSets.SelectMany(synSet=> PunHelper.GetPuns(category, theme, synSet, WordNetEngine, Lookup)).ToList();

            puns.Should().HaveCountGreaterThan(2);

            TestOutputHelper.WriteLine(puns.Count + " puns");

            foreach (var (newPhrase, oldPhrase) in puns.Select(x=>(x.NewPhrase, x.OldPhrase)).Distinct())
            {
                TestOutputHelper.WriteLine($"{newPhrase} ({oldPhrase})");
            }
        }

    }
}

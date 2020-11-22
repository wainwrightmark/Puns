using System.Linq;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Puns.Test
{
    public class PunTests
    {
        public PunTests(ITestOutputHelper testOutputHelper) => TestOutputHelper = testOutputHelper;

        public ITestOutputHelper TestOutputHelper { get; }

        [Fact]
        public void TestSynSets()
        {
            var engine = new WordNet.WordNetEngine();
            var synSets = engine.GetSynSets("Fish").ToList();

            synSets.Should().HaveCountGreaterThan(2);

            foreach (var synSet in synSets)
                TestOutputHelper.WriteLine(synSet.Gloss);

        }

        [Fact]
        public void TestPronunciation()
        {
            var lookup = CMU.WordHelper.TryCreateLookup();

            if(lookup.IsFailure) throw new XunitException(lookup.Error);

            lookup.Value["fish"].Should().NotBeEmpty();

            lookup.Value["fish"].First().Symbols.Should().NotBeEmpty();
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
            var engine = new WordNet.WordNetEngine();
            var lookupResult = CMU.WordHelper.TryCreateLookup();
            if(lookupResult.IsFailure) throw new XunitException(lookupResult.Error);

            var synSets = engine.GetSynSets(theme).ToList();

            var puns = synSets.SelectMany(synSet=> PunHelper.GetPuns(category, theme, synSet, engine, lookupResult.Value)).ToList();

            puns.Should().HaveCountGreaterThan(2);

            TestOutputHelper.WriteLine(puns.Count + " puns");

            foreach (var (newPhrase, oldPhrase) in puns.Select(x=>(x.NewPhrase, x.OldPhrase)).Distinct())
            {
                TestOutputHelper.WriteLine($"{newPhrase} ({oldPhrase})");
            }
        }

    }
}

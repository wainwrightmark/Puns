using System.Linq;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Puns.Test
{
    public class PunTests
    {
        public PunTests(ITestOutputHelper testOutputHelper) => TestOutputHelper = testOutputHelper;

        public ITestOutputHelper TestOutputHelper { get; }

        [Fact]
        public void TestSynSets()
        {
            var synSets = WordData.GetSynsets("Fish").ToList();

            synSets.Should().HaveCountGreaterThan(2);

            foreach (var synSet in synSets)
            {
                TestOutputHelper.WriteLine(synSet.Gloss);
            }

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

            var synSets = WordData.GetSynsets(theme);

            var puns = synSets.SelectMany(synSet=> PunHelper.GetPuns(category, theme, synSet)).ToList();

            puns.Should().HaveCountGreaterThan(2);

            foreach (var pun in puns.Select(x=>(x.NewPhrase, x.OldPhrase)).Distinct())
            {
                TestOutputHelper.WriteLine($"{pun.NewPhrase} ({pun.OldPhrase})");
            }
        }

    }
}

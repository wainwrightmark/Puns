using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Puns;

namespace Benchmark
{

[SimpleJob(RuntimeMoniker.NetCoreApp50)]
[RPlotExporter]
public class PunHelperBenchmark
{

    [Params("Vegetable", "Fish", "Chocolate", "Food", "House", "Furniture", "Person")]
    public string Theme;

    [Params(PunCategory.Songs, PunCategory.Bands, PunCategory.Movies, PunCategory.Idiom)]
    public PunCategory Category;

    [Benchmark]
    public List<Pun> TestPunHelper()
    {
        var fixture = new WordFixture();

        var synSets = fixture.WordNetEngine.GetSynSets(Theme).ToList();

        var puns = Puns.PunHelper.GetPuns(
                Category,
                Theme,
                synSets,
                fixture.WordNetEngine,
                fixture.PronunciationEngine,
                fixture.SpellingEngine,
                PunStrategyFactory.AllFactories
            )
            .ToList();

        return puns;
    }
}

}
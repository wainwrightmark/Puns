using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Puns;

namespace Benchmark
{

[SimpleJob(RuntimeMoniker.NetCoreApp50)]
[RPlotExporter]
public class PunStrategyBenchmark
{
    [GlobalSetup]
    public void Setup()
    {
        _fixture = new WordFixture();
    }

    [Params(
        nameof(PunStrategyFactory.Homophone),
        nameof(PunStrategyFactory.PerfectRhyme),
        nameof(PunStrategyFactory.Prefix),
        nameof(PunStrategyFactory.PrefixRhyme),
        nameof(PunStrategyFactory.SameConsonants),
        nameof(PunStrategyFactory.InfixRhyme),
        nameof(PunStrategyFactory.SharedPrefix)
    )]
    public string Strategy;

    public IReadOnlyList<string> Themes = new List<string>()
    {
        "Vegetable",
        "Fish",
        "Chocolate",
        "Food",
        "House",
        "Furniture",
        "Person"
    };

    private WordFixture _fixture;

    [Benchmark]
    public int TestPunHelper()
    {
        var strategies = new List<PunStrategyFactory>() { PunStrategyFactory.GetByName(Strategy) };

        int punCount = 0;

        foreach (var theme in Themes)
        {
            var synSets = _fixture.WordNetEngine.GetSynSets(theme).ToList();

            foreach (var category in Enum.GetValues<PunCategory>())
            {
                var puns = PunHelper.GetPuns(
                        category,
                        theme,
                        synSets,
                        _fixture.WordNetEngine,
                        _fixture.PronunciationEngine,
                        _fixture.SpellingEngine,
                        strategies
                    )
                    .ToList();

                punCount += puns.Count;
            }
        }

        return punCount;
    }
}

}

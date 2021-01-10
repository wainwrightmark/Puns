using System;
using System.Collections.Generic;
using System.Linq;
using Pronunciation;
using Puns.Strategies;

namespace Puns
{

public class PunStrategyFactory
{
    public static PunStrategyFactory Homophone = new((s, t) => new HomophonePunStrategy(s, t), nameof(Homophone));
    public static PunStrategyFactory PerfectRhyme = new((s, t) => new PerfectRhymePunStrategy(s, t), nameof(PerfectRhyme));
    public static PunStrategyFactory Prefix = new((s, t) => new PrefixPunStrategy(s, t), nameof(Prefix));
    public static PunStrategyFactory PrefixRhyme = new((s, t) => new PrefixRhymePunStrategy(s, t), nameof(PrefixRhyme));
    public static PunStrategyFactory SameConsonants = new((s, t) => new SameConsonantsPunStrategy(s, t), nameof(SameConsonants));
    public static PunStrategyFactory InfixRhyme = new((s, t) => new InfixRhymePunStrategy(s, t), nameof(InfixRhyme));
    public static PunStrategyFactory SharedPrefix = new((s, t) => new SharedPrefixPunStrategy(s, t), nameof(SharedPrefix));

    public static readonly IReadOnlyList<PunStrategyFactory> AllFactories =
        new[]
        {
            Homophone, PerfectRhyme, Prefix, PrefixRhyme, SameConsonants, InfixRhyme,
            SharedPrefix
        };

    public static readonly IReadOnlyDictionary<string, PunStrategyFactory> FactoriesDictionary =
        AllFactories.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        public string Name {get;}

    private readonly Func<SpellingEngine, IReadOnlyList<PhoneticsWord>, PunStrategy> _func;

    private PunStrategyFactory(Func<SpellingEngine, IReadOnlyList<PhoneticsWord>, PunStrategy> func, string name)
    {
        _func     = func;
        Name = name;
    }

    public PunStrategy GetStrategy(SpellingEngine spellingEngine, IReadOnlyList<PhoneticsWord> theme) => _func(spellingEngine, theme);

    public static PunStrategyFactory GetByName(string name) => FactoriesDictionary[name];
}

}

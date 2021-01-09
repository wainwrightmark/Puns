using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Pronunciation;
using WordNet;

namespace Puns.Blazor.Pages
{

public class Choice<T>
{
    public Choice(T entity, bool chosen)
    {
        Entity = entity;
        Chosen = chosen;
    }

    public T Entity { get; }

    public bool Chosen { get; set; }

    /// <inheritdoc />
    public override string ToString() => (Entity, Chosen).ToString();
}

public sealed class PunState : IDisposable
{
    public PunState(string initialTheme)
    {
        WordNetEngine       = new WordNetEngine();
        PronunciationEngine = new PronunciationEngine();
        SpellingEngine      = new SpellingEngine();
        Theme               = initialTheme;
    }

    public PunCategory PunCategory { get; set; } = PunCategory.Idiom;

    private string _theme = "";

    public string Theme
    {
        get => _theme;
        set
        {
            var changed = !_theme.Trim().Equals(value.Trim(), StringComparison.OrdinalIgnoreCase);

            _theme = value.Trim();
            if (changed)
            {
                AllSynSets =
                    GetSynSets(Theme, WordNetEngine)
                        .Select((x,i)=> new Choice<SynSet>(x, i==0))
                        .ToList();
            }
        }
    }

    public IReadOnlyCollection<Choice<SynSet>> AllSynSets { get; private set; } =  new List<Choice<SynSet>>();


    public IReadOnlyCollection<IGrouping<string, Pun>> PunList { get; set; } = Array.Empty<IGrouping<string, Pun>>();


    public bool IsGenerating { get; set; }

    public WordNetEngine WordNetEngine { get; }

    public PronunciationEngine PronunciationEngine { get; }

    public SpellingEngine SpellingEngine { get; }

    private static IEnumerable<SynSet> GetSynSets(string? theme, WordNetEngine wordNetEngine)
    {
        if (string.IsNullOrWhiteSpace(theme))
            return Enumerable.Empty<SynSet>();

        var sets = wordNetEngine.GetSynSets(theme);
        return sets;
    }

    public async Task Find()
    {
        if (string.IsNullOrWhiteSpace(Theme))
        {
            IsGenerating = false;
            return;
        }

        IsGenerating = true;
        PunList      = Array.Empty<IGrouping<string, Pun>>();

        var task = new Task<IReadOnlyCollection<IGrouping<string, Pun>>>(
            () => GetPuns(
                PunCategory,
                Theme,
                AllSynSets.Where(x=>x.Chosen).Select(x=>x.Entity).ToList(),
                WordNetEngine,
                PronunciationEngine,
                SpellingEngine
            ).ToList()
        );

        task.Start();

        PunList = await task;

        IsGenerating = false;
    }

        public IEnumerable<Pun> StreamPuns() => PunHelper.GetPuns(
            PunCategory,
            Theme,
            AllSynSets.Where(x => x.Chosen).Select(x => x.Entity).ToList(),
            WordNetEngine,
            PronunciationEngine,
            SpellingEngine
        );



        private static IEnumerable<IGrouping<string, Pun>> GetPuns(
        PunCategory punCategory,
        string theme,
        IReadOnlyCollection<SynSet> synSets,

        WordNetEngine wordNetEngine,
        PronunciationEngine pronunciationEngine,
        SpellingEngine spellingEngine)
    {
        //TODO use virtualize https://docs.microsoft.com/en-us/aspnet/core/blazor/webassembly-performance-best-practices?view=aspnetcore-5.0



        var puns = PunHelper.GetPuns(
            punCategory,
            theme,
            synSets,
            wordNetEngine,
            pronunciationEngine,
            spellingEngine
        ).ToList();



        var groupedPuns = puns
            .Distinct()
            .SelectMany(pun => pun.PunWords.Select(punWord => (pun, punWord)))
            .GroupBy(x => x.punWord, x => x.pun, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(x => x.Count());

        HashSet<Pun> usedPuns = new();

        var pairs = from @group in groupedPuns
                    from pun in @group
                    where usedPuns.Add(pun)
                    select (@group.Key, pun);

        var finalPuns = pairs.GroupBy(x => x.Key, x => x.pun)
            .OrderByDescending(x => x.Count());
            //.ToList();

        return finalPuns;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        WordNetEngine.Dispose();
        PronunciationEngine.Dispose();
    }
}

}

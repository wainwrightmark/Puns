using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pronunciation;
using WordNet;

namespace Puns.Blazor.Pages
{

public class Choice<T>
{
    public Choice(T entity, bool chosen, Action<bool> onChanged)
    {
        Entity    = entity;
        _chosen   = chosen;
        OnChanged = onChanged;
    }

    public T Entity { get; }

    private bool _chosen;

    public bool Chosen
    {
        get => _chosen;
        set
        {
            if (_chosen != value)
            {
                _chosen = value;
                OnChanged(_chosen);
            }
        }
    }

    public Action<bool> OnChanged { get; }

    /// <inheritdoc />
    public override string ToString() => (Entity, Chosen).ToString();
}

public sealed class PunState : IDisposable
{
    public PunState(string initialTheme, PunCategory? initialCategory, Action stateHasChanged)
    {
        WordNetEngine       = new WordNetEngine();
        PronunciationEngine = new PronunciationEngine();
        SpellingEngine      = new SpellingEngine();
        Theme               = initialTheme;
        StateHasChanged     = stateHasChanged;
        PunCategory         = initialCategory;
    }

    private PunCategory? _punCategory;

    public PunCategory? PunCategory
    {
        get => _punCategory;
        set
        {
            _punCategory = value;
            ClearPuns();
        }
    }

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
                var sets          = GetSynSets(Theme, WordNetEngine);
                var setGlossPairs = PunHelper.GetRelativeGloss(sets, 3, WordNetEngine);

                AllSynSets =
                    setGlossPairs
                        .Select(
                            (x, i) => new Choice<(SynSet synSet, string gloss)>(
                                x,
                                i == 0,
                                _ => ClearPuns()
                            )
                        )
                        .ToList();

                ClearPuns();
            }
        }
    }

    public Action StateHasChanged { get; }

    public IReadOnlyCollection<Choice<(SynSet synSet, string gloss)>> AllSynSets
    {
        get;
        private set;
    } = new List<Choice<(SynSet synSet, string gloss)>>();

    public IReadOnlyCollection<IGrouping<string, Pun>>? PunList { get; set; } = null;

    public void ClearPuns()
    {
        PunList = null;
    }

    public IReadOnlyList<PunCategory?> PossibleCategories = Enum.GetValues<PunCategory>()
        .Cast<PunCategory?>()
        .Prepend(null)
        .ToList();

    public bool IsGenerating => _generatingTask != null;
    public bool CanGenerate => !string.IsNullOrWhiteSpace(Theme) && PunCategory.HasValue;

    private Task<IReadOnlyCollection<IGrouping<string, Pun>>>? _generatingTask = null;

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
        _generatingTask = null;
        ClearPuns();

        if (!CanGenerate)
            return;

        var task = new Task<IReadOnlyCollection<IGrouping<string, Pun>>>(
            () => GetPuns(
                PunCategory!.Value,
                Theme,
                AllSynSets.Where(x => x.Chosen).Select(x => x.Entity.synSet).ToList(),
                WordNetEngine,
                PronunciationEngine,
                SpellingEngine
            )
        );

        _generatingTask = task;

        StateHasChanged();

        task.Start();
        PunList = await task;

        if (_generatingTask == task)
        {
            _generatingTask = null;
        }

        StateHasChanged();
    }

    private static IReadOnlyCollection<IGrouping<string, Pun>> GetPuns(
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
            spellingEngine,
            PunStrategyFactory.AllFactories
        );

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
            .OrderByDescending(x => x.Count())
            .ToList();

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

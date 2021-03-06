﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Pronunciation;
using WordNet;

namespace Puns.Blazor.Pages
{

public record FavoritePun(string NewText, decimal Score);

public sealed class PunState : IDisposable
{
    public PunState(string initialTheme, PunCategory initialCategory, Action stateHasChanged, ISyncLocalStorageService storage)
    {
        WordNetEngine       = new WordNetEngine();
        PronunciationEngine = new PronunciationEngine();
        SpellingEngine      = new SpellingEngine();
        Theme               = initialTheme;
        StateHasChanged     = stateHasChanged;
        Storage             = storage;
        PunCategory         = initialCategory;
        FavoritePuns        = GetfavoritePuns(storage);
    }

    private PunCategory _punCategory;

    public PunCategory PunCategory
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
                var sets = GetSynSets(Theme, WordNetEngine).ToList();
                PossibleSynsets = PunHelper.GetRelativeGloss(sets, 3, WordNetEngine).ToList();
                ChosenSynsets   = PossibleSynsets.Take(1).Select(x=>x.Index).ToList();
                ClearPuns();
            }
        }
    }

    private static Dictionary<string, FavoritePun> GetfavoritePuns(ISyncLocalStorageService storage)
    {
        var length = storage.Length();
        var dict   = new Dictionary<string, FavoritePun>();

        for (var i = 0; i < length; i++)
        {
            var key   = storage.Key(i);
            var value = storage.GetItem<FavoritePun>(key);
            dict[key] = value;
        }

        return dict;

    }

    public Action StateHasChanged { get; }
    public ISyncLocalStorageService Storage { get; }
    public Dictionary<string, FavoritePun> FavoritePuns { get; }

    public void SetRating(Pun pun, decimal rating)
    {
        if(rating > 0)
        {
            var fp = new FavoritePun(pun.NewPhrase, rating);
            bool changed;

            if (FavoritePuns.TryGetValue(fp.NewText, out var oldFp) && oldFp.Score != rating)
            {
                changed = false;
            }
            else
                changed = true;


            if (changed)
            {
                FavoritePuns[fp.NewText] = fp;
                Storage.SetItem(fp.NewText, fp);
            }
        }
        else
        {
            if(FavoritePuns.Remove(pun.NewPhrase))

            {
                Storage.RemoveItem(pun.NewPhrase);
            }
        }


    }

    public decimal GetRating(Pun pun)
    {
        if (FavoritePuns.TryGetValue(pun.NewPhrase, out var fp))
        {
            return fp.Score;
        }

        return 0;
    }

    public IReadOnlyCollection<SynsetWithGloss> PossibleSynsets { get; private set; } =
        new List<SynsetWithGloss>();

    public IEnumerable<int> ChosenSynsets { get; set; } =
        new List<int>();


    public IReadOnlyCollection<IGrouping<string, Pun>>? PunList { get; set; }

    public void ClearPuns()
    {
        PunList = null;
    }

    public bool IsGenerating => _generatingTask != null;
    public bool CanGenerate => !string.IsNullOrWhiteSpace(Theme);

    private Task<IReadOnlyCollection<IGrouping<string, Pun>>>? _generatingTask;

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
                PunCategory,
                Theme,
                PossibleSynsets.Where(x=> ChosenSynsets.Contains(x.Index))
                    .Select(x=>x.SynSet).ToList(),
                //AllSynSets

                //    .Where(x => x.Chosen)
                //    .Select(x => x.Entity.synSet).ToList(),
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
